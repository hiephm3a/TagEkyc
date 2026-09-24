# TIP-88C1-B2-DURABLE-KEY — Durable Wrapped-Key Custody Foundation — BUILD DISPATCH (v0.6)

**Status: SUPERSEDED — DECOMPOSED (Homeowner-approved, after the v0.7 STOP/RRI on non-convergence). NOT BUILD AUTHORITY.**
The v0.6 review (6 HIGH / 3 MEDIUM, 7th patch, zero code) was a documented non-converging review-loop on a growing surface;
two findings were not docs-closable (CSPRNG needs deployment authority; the fixture journal had become a co-equal contract).
Decomposed into three streams — DO NOT IMPLEMENT FROM THIS DOCUMENT:
- `tip_88c1_b2_durable_key_prod_build_dispatch.md` (DK-PROD) — lead production-key custody contract (findings A/B/D/I).
- `tip_88c1_b2_durable_key_fixture_proof_build_dispatch.md` (DK-FIXTURE-PROOF) — durable provider test double (findings E/G/H).
- `tip_88c1_b2_durable_key_csprng_prereq.md` (DK-CSPRNG-PREREQ) — deployment-policy RRI for the CSPRNG extension schema (C).
Retained below verbatim as history only (SHA `CE39B509864AD6D5A9800342271E085671BB095077FBF0CCC25151F82C8B7013` at supersession).

---
*Historical v0.6 content preserved below is NOT the build contract. Use the three decomposed documents above.*

**(Historical status line:)** DOCS-ONLY — READY FOR FINAL CLOSURE REVIEW — NOT READY FOR BUILD. Proof-build authorization is SEPARATE and NOT
requested in this turn. DO NOT IMPLEMENT/MIGRATE/EDIT-CODE/COMMIT/PUSH/MERGE/PR/DEPLOY. Decomposed from the superseded
combined draft `tip_88c1_b2_durable_custody_build_dispatch.md` (v7). Sibling
`tip_88c1_b2_durable_object_custody_build_dispatch.md` is BLOCKED on this contract and is untouched by this patch. R2 remains
SUSPENDED. Do not reopen R1/CORE/AUTH/KEY1/KEY2/B1/BETA/DEKKEK semantics.
**Repo:** `D:\Task\Remote Signing\TagEkyc` · **Baseline:** `23e0154805e468919e513bf04b0a24d374d1ef13` · **Tier-0.**

**Owns:** PostgreSQL durable attempt-key reservation head + append-only preparation/observation history; an additive
alternate-key + composite FK binding to the landed R1 attempt; the internally-selected wrapping profile; a durable
provider-operation adapter mapping (H5); provider operation-token generation + binding; `WrapDek`/`UnwrapDek`; provisioning
resolution + cleanup; preparation lease/fence/revision; direct + recovered activation; durable provider receipts/evidence;
bounded retry + process-restart recovery; Active→Revoked; frame-agnostic AEAD capability seams confined to Infrastructure;
key-custody roles/ACL; exact SQL/Up/Down for this scope; a test-only fixture KEK provider; proof tests.
**Does NOT own:** S3/MinIO, object keys, multipart, write sessions/parts, object verify/delete/quarantine, R2 codec,
plaintext streaming, ContentCommitment post-decrypt verification, R3 staging, real Raw BIO, production activation.

**Preserved-closed (do not reopen):** DURABLE-KEY owns only wrapped-key reservation, provider recovery/cleanup, key
evidence, AEAD capability seams and its DB/role/readiness surfaces; `WrappingSuiteId`/`WrappingSuiteVersion` are NOT added to
the R1 row; head and append-only history remain separate; `ProviderOutcomeUnknown` blocks every fresh DEK; `NoProviderResult`
is distinct from `ProviderUnavailable`; direct and recovered activation are distinct operations; no `PreparationOwnerActorId`;
`RowFence` stays removed; `RowRevision` and `CurrentPreparationFence` stay separate with completed semantics (§H11); the
landed process-local DEKKEK provider stays test-only `ProcessLocalFixture` and fails production readiness; the
business/application actor is never conflated with an internal worker lease owner; DURABLE-OBJECT and R2 stay blocked.

## K1. Landed codec (exact — the single canonical helper)
The landed schema already contains `tagekyc.raw_export_c1_hash_canonical(p_domain text, VARIADIC p_fields text[]) RETURNS
bytea`. This dispatch introduces NO second hash/canonical helper. `C1HashCanonical(domain, s0..s(n-1))` = SHA-256 over the
concatenation of `LP(domain)` followed by `LP(s_i)` for each i in 0..n-1, where `LP(s) = u32-BE(octet_length(NFC-UTF8(s)))
‖ NFC-UTF8(s)`. Scalar rendering (exact SQL):
**Guid → lowercase 32-char `"N"` text** = `replace(uuid_value::text,'-','')`; **byte[] → lowercase hexadecimal** =
`pg_catalog.encode(value,'hex')`; **integer/bigint → invariant canonical decimal text**; **text → passed to the helper,
which normalizes NFC**. Optional fields use unambiguous tagged scalars built from ordinary landed scalars: **absent → one
scalar `"0"`; present → two scalars `"1"` then the canonical value**. A tag and value are never concatenated into one
scalar. Every present provider token/reference/receipt/suite value is trimmed, non-empty, control-character-free and
bounded ≤512 UTF-8 bytes (§M7). No UUID network-byte-order, no fixed-width binary, no raw-bytea rendering under this codec.

## 1. Authoritative state / operation table (derive everything from this)
`PreparationDisposition ∈ { PreparingLive, PreparingExpiredAwaitingResolution, ProviderOutcomeUnknown,
ProviderCorruptOrUnverifiable, ProviderCleanupRequired, ReadyForFreshPreparation, Active, Revoked, AbandonRequested,
ReservationAbandoned }`.
Operations (capability): prepare (`tagekyc_raw_export_custody_encryptor`) · direct-activate (encryptor) · mark-expired
(`tagekyc_raw_export_reconciler`) · resolve-provider-outcome (reconciler) · recovered-activate (reconciler) ·
acknowledge-cleanup (reconciler) · revoke (reconciler, `tagekyc_raw_export_lifecycle`) · inspect / read-recovery-context
(encryptor, reconciler). Every transition is a single guarded compare-and-set under a `FOR UPDATE` lock of the head row.

**Transitions (guard → target, event appended):**
- Absent → `PreparingLive` (prepare; `Opened`).
- `PreparingLive` → `PreparingExpiredAwaitingResolution` when `pg_catalog.statement_timestamp()` ≥ `CurrentPreparationLeaseExpiresAtUtc` (mark-expired; `Expired`).
- `PreparingLive` → `Active` **direct** (§H9; `DirectActivated`).
- {`PreparingLive`,`PreparingExpiredAwaitingResolution`,`ProviderOutcomeUnknown`} → `Active` **recovered** (§H9; `RecoveredActivated`).
- {`PreparingLive`,`PreparingExpiredAwaitingResolution`} + `Unknown` → `ProviderOutcomeUnknown` (`ResolvedOutcomeUnknown`).
- {`PreparingLive`,`PreparingExpiredAwaitingResolution`} + `Corrupt` → `ProviderCorruptOrUnverifiable` (`ResolvedCorrupt`).
- {`PreparingLive`,`PreparingExpiredAwaitingResolution`} + `CleanupRequired` → `ProviderCleanupRequired` (`ResolvedCleanupRequired`).
- `PreparingExpiredAwaitingResolution` + `NoProviderResult` (provider-positive proof) → `ReadyForFreshPreparation` (`ResolvedNoResult`).
- `ProviderCleanupRequired` + {`Cleaned`,`AlreadyAbsent`} → `ReadyForFreshPreparation` (`CleanupAcknowledged`).
- `ProviderCleanupRequired` + {`CleanupUnavailable`,`CleanupOutcomeUnknown`,`CleanupFailed`} → `ProviderCleanupRequired`
  (unchanged; append repeatable `CleanupAttemptObserved`; increment `CleanupAttemptCount`; set `NextCleanupAttemptNotBeforeUtc`;
  no fresh DEK). When `statement_timestamp() > CleanupDeadlineUtc` while still blocking: remain `ProviderCleanupRequired`, set
  `CleanupOperatorInterventionRequired = true`, readiness hard-fails, still no fresh DEK (not reinterpreted as Corrupt) (§H5).
- `ReadyForFreshPreparation` → `PreparingLive` (prepare; new `PreparationId`, `PreparationFence`+1, new token+lease; `Opened`).
- `Active` → `Revoked` (revoke; `Revoked` event). Revoke is reachable ONLY from `Active`.
- **Two-phase abandonment (item E — operator authorization is NOT evidence of provider-resource absence):**
  1. {`PreparingLive`,`PreparingExpiredAwaitingResolution`,`ProviderOutcomeUnknown`,`ProviderCleanupRequired`} →
     `AbandonRequested` (operator request-abandon). `AbandonRequested` is NON-terminal and blocking: no fresh DEK, no
     activation; it drives the provider operation toward `CleanupRequired` (then cleanup) or absence resolution. Readiness
     `PROD_RAW_EXPORT_KEY_ABANDON_PENDING` fails WHILE pending.
  2. `AbandonRequested` → `ReservationAbandoned` (finalize-abandon) is permitted ONLY when the provider-operation mapping has
     reached `CleanedUp` (provider resource cleaned) or `AbsenceProven` (positively absent) — never on operator authorization
     alone. `ReservationAbandoned` is terminal: no activation, no fresh preparation, no revoke. **A completed abandonment does
     NOT fail global readiness** (the resource is provably gone); it is a clean terminal like `Revoked`. Every authorized
     operation leaves both `AbandonRequested` (blocked) and `ReservationAbandoned` (terminal) unchanged. Mapping-only
     abandonment is impossible — there is no mapping `Abandoned` state; cleanup can never be lost.

**Provider-success / process-crash-before-`ResultObserved` window (v0.4 gap 1) — closed:** a DB operation token alone is NOT
durable provider recovery. The provider itself MUST be durable and idempotent keyed by `ProviderOperationToken`: `WrapDek` is
CreateOrGet (a second call with the same token returns the identical wrapped result), and `LookupByOperationToken` (§3)
returns `Found`/`PositivelyAbsent`/`Unknown` for that token. If the process crashes AFTER provider success but BEFORE the
adapter records `ResultObserved`, the mapping is still `Issued`; a fresh process resolves by calling
`LookupByOperationToken(token)` and then, for `Found`, invokes the reconciler-only recorder
`raw_export_record_recovered_key_provider_result` (§4a), which records `ResultObserved` AND performs recovered activation in
one guarded SD transaction; `PositivelyAbsent → AbsenceProven`, `Unknown → ProviderOutcomeUnknown`. **Trust boundary
(item B):** `LookupByOperationToken.Found` is guaranteed BY THE TRUSTED DURABLE PROVIDER ADAPTER to be the immutable original
result for the (token, context) — the fixture journal (§2e) is CreateOrGet-immutable, and production KMS/HSM is idempotent by
token. SQL does NOT independently know what the provider originally returned; it locks the frozen context/profile, validates
the wrapped shape (32/12/16-byte lengths) and suite equality against the persisted profile, recomputes `WrappedDekMetadataDigest`
and (for the resolution family) the resolution-evidence digest, and performs guarded CAS. Any divergent-result attack is
detected at the fixture-provider adapter/journal layer (test #57c), not by an impossible SQL byte-comparison. A provider that
cannot offer durable lookup fails readiness `PROD_RAW_EXPORT_KEY_PROVIDER_DURABILITY_UNSUPPORTED`. Fresh-process test #57
exercises the exact crash window.

**Recovery machine (H6) — closed:**
- `ProviderOutcomeUnknown` blocks every fresh DEK; it permits re-resolution of the SAME `AttemptKeyReservationId`,
  `PreparationId`, `PreparationFence`, `ProviderOperationToken`, `AttemptKeyContextFingerprint`. Permitted re-resolution edges:
  `+WrappedResultRecovered → Active` (recovered activation); `+Unknown → ProviderOutcomeUnknown` + append repeated
  `ResolvedOutcomeUnknown`; `+ProviderUnavailable → (unchanged) ProviderOutcomeUnknown` + append `ProviderUnavailableObserved`;
  `+ProviderResourceCleanupRequired → ProviderCleanupRequired`.
- `ProviderUnavailable` never maps to `NoProviderResult`, creates no new preparation, appends a repeatable
  `ProviderUnavailableObserved`, leaves the blocking disposition unchanged, and is bounded by the retry model below.
- `ProviderCorruptOrUnverifiable` is a **terminal disposition inside DURABLE-KEY v0.3 (H4)**: it blocks every fresh DEK, has
  NO successor in this slice — no activation, no fresh preparation, **no revoke** — no wrapped-key lease, and readiness stays
  failed with `PROD_RAW_EXPORT_KEY_PROVIDER_CORRUPT_UNRESOLVED`. Every currently-authorized operation leaves it
  unchanged/blocked. Future operator resolution of this state requires a separately-reviewed amendment (out of scope here).
- `NoProviderResult` (H3 — positive absence proof) is accepted **only** from `PreparingExpiredAwaitingResolution`, only after
  `pg_catalog.statement_timestamp()` ≥ lease expiry, only with exact current preparation/fence/token, and only when the
  adapter-mapping state is `AbsenceProven` carrying a non-empty bounded `ProviderAbsenceProofReceipt` with all wrapped-result/
  resource/cleanup fields NULL and the absence-evidence digest recomputing exactly. Timeout, transport failure, missing
  response, empty lookup, process-local dictionary miss and generic `NotFound` do NOT prove absence.
- **Resolution retry model (enforceable, persisted, M1 concrete):** the head carries `ResolutionAttemptCount bigint`,
  `NextResolutionAttemptNotBeforeUtc timestamptz`, `ResolutionDeadlineUtc timestamptz`. Each provider-resolution observation
  increments `ResolutionAttemptCount` and sets `NextResolutionAttemptNotBeforeUtc = statement_timestamp() +
  min(ResolutionInitialRetryDelay × ResolutionRetryMultiplier^(ResolutionAttemptCount−1), ResolutionMaxBackoff)` (values §M1).
  **Gap 7:** `ResolutionAttemptCount` counts ONLY actual provider-resolution observations (a `resolve` that performed a real
  provider `LookupByOperationToken`/resolution, or a recovered-result recording); `mark-expired` sets/initializes
  `ResolutionDeadlineUtc` but does NOT increment the count — expiry is a clock event, not a provider observation. With
  `ResolutionMaxAttemptCount = 1`, at least one real lookup is therefore always admitted after expiry before exhaustion
  (test #63);
  a resolve attempted before `NextResolutionAttemptNotBeforeUtc` returns `RetryTooSoon` without mutation. **Deadline
  initialization (gap 5):** `ResolutionDeadlineUtc` is set exactly once, at first entry into
  `PreparingExpiredAwaitingResolution` for the current generation, to `statement_timestamp() + ResolutionDeadline`, and is
  immutable for that generation (a new preparation generation sets a fresh one). **Exhaustion (gap 5):** while still blocking,
  the reservation transitions to `ProviderCorruptOrUnverifiable` (`ResolvedCorrupt`, terminal operator-intervention) when
  `statement_timestamp() > ResolutionDeadlineUtc` **OR** when `ResolutionMaxAttemptCount > 0` and `ResolutionAttemptCount ≥
  ResolutionMaxAttemptCount` (whichever first); when `ResolutionMaxAttemptCount = 0` only the deadline governs. Proven by
  fields + executable transitions, not asserted. Cleanup has its own independent clock/counter (§H5), never sharing the
  resolution fields; `CleanupDeadlineUtc` is set once at first entry into `ProviderCleanupRequired` as `statement_timestamp()
  + CleanupDeadline`, immutable for that generation.

## 2. Relational schema + constraints
### 2a. Landed R1 binding (H1 — closed, no deferral)
Known landed `tagekyc.raw_export_source_encryption_attempts`: PK (`AttemptId`); UNIQUE (`SourceArtifactId`,
`EncryptionAttemptRevision`); UNIQUE (`SourceArtifactId`, `AttemptId`, `Fence`); it does NOT have UNIQUE(`AttemptId`,
`AttemptKeyReservationId`). This migration adds one additive alternate key on the landed attempt table:
```
ALTER TABLE tagekyc.raw_export_source_encryption_attempts
  ADD CONSTRAINT uq_raw_export_enc_attempt_attemptid_keyresvid
  UNIQUE ("AttemptId", "AttemptKeyReservationId");   -- 45 bytes ≤ 63
```
`AttemptKeyReservationId` is a landed attempt column; `AttemptId` is the landed PK; the pair is therefore functionally
unique and the additive constraint is non-destructive. The reservation head declares the composite FK:
```
CONSTRAINT fk_raw_export_attempt_key_resv_attempt_composite      -- 48 bytes ≤ 63
  FOREIGN KEY ("AttemptId", "AttemptKeyReservationId")
  REFERENCES tagekyc.raw_export_source_encryption_attempts ("AttemptId", "AttemptKeyReservationId")
  ON DELETE RESTRICT
```
Source column order `("AttemptId","AttemptKeyReservationId")` = target column order (matches the new alternate key exactly).
EF: `modelBuilder.Entity<RawExportAttemptKeyReservationRow>().HasOne<RawExportSourceEncryptionAttemptRow>().WithMany()
.HasForeignKey(r => new { r.AttemptId, r.AttemptKeyReservationId }).HasPrincipalKey(a => new { a.AttemptId,
a.AttemptKeyReservationId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_raw_export_attempt_key_resv_attempt_composite")`.
Up adds the alternate key then the FK; Down drops the FK then the alternate key (restoring the landed key set exactly).
Catalog round-trip test reads `pg_constraint`/`information_schema.table_constraints` for both names. Mutation: dropping
either the alternate key or the FK makes `KeyReservation_ForeignKey_BindsExactLandedAttempt` go RED (insert with a
non-matching `(AttemptId,AttemptKeyReservationId)` must raise SQLSTATE `23503`). A SQL procedural existence check does not
substitute for this FK.

### 2b. Head `tagekyc.raw_export_attempt_key_reservations` (owner `tagekyc_raw_export_deployer`; direct DML denied; H11 CAS)
PK `AttemptKeyReservationId uuid`. Columns: `AttemptId uuid NOT NULL`; frozen key-context `EncryptionAttemptFingerprint
bytea NOT NULL CHECK octet_length=32`, `KeyProviderId text NOT NULL`, `KekId text NOT NULL`, `KekVersion integer NOT NULL`,
`KekFingerprint text NOT NULL`; `AttemptKeyContextFingerprint bytea NOT NULL CHECK octet_length=32`; internal profile
`WrappingSuiteId text NOT NULL`, `WrappingSuiteVersion integer NOT NULL` (immutable after first write); `PreparationDisposition
text NOT NULL`; `CurrentPreparationId uuid NULL`; `CurrentPreparationFence bigint NOT NULL DEFAULT 1`;
`CurrentPreparationLeaseExpiresAtUtc timestamptz NULL`; `CurrentProviderOperationToken text NULL`; resolution retry
`ResolutionAttemptCount bigint NOT NULL DEFAULT 0`, `NextResolutionAttemptNotBeforeUtc timestamptz NULL`,
`ResolutionDeadlineUtc timestamptz NULL`; cleanup retry (H5, independent clock) `CleanupAttemptCount bigint NOT NULL DEFAULT
0`, `NextCleanupAttemptNotBeforeUtc timestamptz NULL`, `CleanupDeadlineUtc timestamptz NULL`,
`CleanupOperatorInterventionRequired boolean NOT NULL DEFAULT false`; active wrapped state `WrappedDekCiphertext bytea NULL
CHECK octet_length=32`, `WrappedDekNonce bytea NULL CHECK octet_length=12`, `WrappedDekTag bytea NULL CHECK octet_length=16`,
`WrappedDekMetadataDigest bytea NULL CHECK octet_length=32`; `RowRevision bigint NOT NULL DEFAULT 1` (monotonic head-mutation
revision, +1 per non-idempotent head UPDATE; idempotent replay/read increments neither fence nor revision); `PreparedAtUtc
timestamptz NULL`; `RevokedAtUtc timestamptz NULL`; `RevocationReasonCode text NULL`; `CreatedAtUtc timestamptz NOT NULL`,
`UpdatedAtUtc timestamptz NOT NULL`. No `RowFence`, no `PreparationOwnerActorId`. **M2:** first `CurrentPreparationFence = 1`
at head insert; each fresh preparation (`ReadyForFreshPreparation`→`PreparingLive`) increments it by exactly 1, no reuse;
`RowRevision = 1` at insert, +1 per non-idempotent head UPDATE; `bigint` overflow of either counter fails closed with
`RAW_EXPORT_KEY_ARGUMENT_INVALID`. §H11/§M1 pin the full delta matrices.

### 2c. History `tagekyc.raw_export_attempt_key_preparation_events` (H7 — append-only authority)
Owner deployer; PK `PreparationEventId uuid`; FK `AttemptKeyReservationId`→head ON DELETE RESTRICT. Columns:
`PreparationId uuid NOT NULL`; `PreparationFence bigint NOT NULL`; `EventSequence bigint NOT NULL`; `EventKind text NOT NULL
∈ {Opened, Expired, DirectActivated, RecoveredActivated, ResolvedNoResult, ResolvedOutcomeUnknown, ResolvedCorrupt,
ResolvedCleanupRequired, ProviderUnavailableObserved, CleanupAttemptObserved, CleanupAcknowledged, AbandonRequested,
ProviderOperationAbandoned, Revoked}`; `ResolutionKind text NULL`;
`CleanupResultKind text NULL`; `ProviderOperationToken text NULL`; `ProviderOperationReceipt text NULL`;
`ProviderCleanupReference text NULL`; `ProviderCleanupReceipt text NULL`; `ProviderResolutionEvidenceDigest bytea NULL CHECK
octet_length=32`; `ProviderCleanupEvidenceDigest bytea NULL CHECK octet_length=32`; `CleanupObservationEvidenceDigest bytea
NULL CHECK octet_length=32`; `WrappedDekMetadataDigest bytea NULL CHECK octet_length=32`; `WrappingSuiteId text NULL`;
`WrappingSuiteVersion integer NULL`; `OperatorReasonCode text NULL`; `OperatorActorEvidence text NULL`; `RevocationReasonCode
text NULL`; `RevocationEvidenceDigest bytea NULL CHECK octet_length=32`; `HeadRowRevision bigint NULL`; `EventAtUtc timestamptz
NOT NULL`.
`EventSequence` is allocated atomically under the locked head as `COALESCE(max(EventSequence),0)+1` per reservation, begins
at 1, increments once per appended event:
```
CONSTRAINT uq_raw_export_attempt_key_event_sequence
  UNIQUE ("AttemptKeyReservationId", "EventSequence")           -- 40 bytes ≤ 63
```
Singleton event kinds use a partial UNIQUE INDEX (not a table `UNIQUE` constraint carrying a `WHERE` clause):
```
CREATE UNIQUE INDEX uq_raw_export_attempt_key_event_singleton   -- 41 bytes ≤ 63
  ON tagekyc.raw_export_attempt_key_preparation_events
  ("AttemptKeyReservationId","PreparationId","PreparationFence","EventKind")
  WHERE "EventKind" IN ('Opened','DirectActivated','RecoveredActivated','CleanupAcknowledged','AbandonRequested','ProviderOperationAbandoned','Revoked');
```
`ResolvedNoResult`,`ResolvedOutcomeUnknown`,`ResolvedCorrupt`,`ResolvedCleanupRequired`,`ProviderUnavailableObserved`,
`CleanupAttemptObserved` are repeatable (ordered by `EventSequence`, not blocked by the singleton index);
`ProviderOperationAbandoned` is a singleton (added to the partial UNIQUE INDEX below). Per-`EventKind`
CHECK shapes (§M1.3) enforce which evidence columns are NON-NULL vs NULL per kind. Append-only: direct INSERT/UPDATE/DELETE
denied by the guard trigger (§M2); only the SECURITY DEFINER transition functions append. Longest identifier here = the
table name at 41 bytes.

### 2d. Durable provider-operation adapter mapping `tagekyc.raw_export_key_provider_operations` (H1/H2 — durable + CAS)
The fixture provider is not assumed to offer a native durable idempotency API, so DURABLE-KEY owns a durable adapter mapping
that itself durably represents the authoritative **opaque** wrapped result (never the raw DEK). It is the ONLY provider-token
topology; the "native binding" alternative is not carried. Owner deployer; direct DML denied; mutable head with narrowly
guarded CAS UPDATE (H2). PK `ProviderOperationId uuid`. Immutable-identity columns: `KeyProviderId text NOT NULL`;
`ProviderOperationToken text NOT NULL`; `AttemptKeyReservationId uuid NOT NULL` (FK head RESTRICT); `PreparationId uuid NOT
NULL`; `PreparationFence bigint NOT NULL`; `AttemptKeyContextFingerprint bytea NOT NULL CHECK octet_length=32`. Mutable state:
`ProviderOperationState text NOT NULL ∈ {Issued, ResultObserved, CleanupRequired, CleanedUp, AbsenceProven}` (no `Abandoned`
mapping state — abandonment is a head-level two-phase lifecycle, §1/§4a, and can only finalize once the mapping is `CleanedUp`
or `AbsenceProven`, so provider cleanup is never lost).
Durable wrapped-result columns (H1, all NULL until `ResultObserved`): `WrappedDekCiphertext bytea NULL CHECK octet_length=32`,
`WrappedDekNonce bytea NULL CHECK octet_length=12`, `WrappedDekTag bytea NULL CHECK octet_length=16`, `WrappedDekMetadataDigest
bytea NULL CHECK octet_length=32`, `WrappingSuiteId text NULL`, `WrappingSuiteVersion integer NULL`, `ResultObservedAtUtc
timestamptz NULL`. Resource/receipt/absence columns: `ProviderResourceReference text NULL`, `ProviderOperationReceipt text
NULL`, `ProviderCleanupReference text NULL`, `ProviderCleanupReceipt text NULL`, `ProviderAbsenceProofReceipt text NULL`,
`ProviderAbsenceObservedAtUtc timestamptz NULL`; `IssuedAtUtc timestamptz NOT NULL`, `UpdatedAtUtc timestamptz NOT NULL`.
```
CONSTRAINT uq_raw_export_key_provider_op_provider_token
  UNIQUE ("KeyProviderId","ProviderOperationToken");            -- 44 bytes ≤ 63
```
**Mutable state graph (H2 — exact transitions, no generic setter):** `Issued → ResultObserved → CleanupRequired → CleanedUp`;
terminal branch `Issued → AbsenceProven`. Every mapping UPDATE checks `ProviderOperationId`, `AttemptKeyReservationId`,
`PreparationId`, `PreparationFence`, `KeyProviderId`, `ProviderOperationToken`, the expected `ProviderOperationState`, and the
current reservation/preparation generation; immutable-identity columns can never change. **The `Issued` row is created only by
`prepare`** (§4, item I) using an internally-derived `ProviderOperationId`, `ProviderOperationToken` and
`AttemptKeyContextFingerprint`, atomically with the head and `Opened` event — there is NO separately-granted issue function.
**Abandonment is head-level two-phase (item E):** operator request-abandon sets the head `AbandonRequested`; the mapping still
advances only along the graph above to `CleanedUp` or `AbsenceProven`; finalize-abandon then sets the head
`ReservationAbandoned`. There is no mapping `Abandoned` state, so provider cleanup can never be bypassed by operator
authorization.

**Two independent durability layers (gap 5), neither substituting for the other:**
- **Provider-owned durable CreateOrGet/Lookup** closes the *provider-success-before-DB-persistence* window: if the process
  dies after the provider wrapped but before the DB mapping records `ResultObserved`, the authoritative wrapped result still
  lives in the provider (keyed by token) and is recovered via `LookupByOperationToken` (§3) then the recovered-result recorder
  (§4a).
- **The DB mapping `ResultObserved` row** closes *restart/readback after* `ResultObserved`: once recorded, any later restart
  reads the durable wrapped result from the mapping/head without touching the provider.
The adapter never persists the raw DEK, never creates a second DEK while the same token is unresolved, and never reinterprets
an empty/missing row as positive absence (H3 requires the explicit `AbsenceProven` state). Writes only via the §4/§4a SD
functions; read surface = the recovery-context function (§H8) plus the inspect-current-operation function (§4a). Guard permits
the prepare-driven INSERT and the exact per-function UPDATE delta only; direct UPDATE/DELETE and mutation under a stale
preparation/fence/token are denied (§M2).

### 2e. Fixture KEK provider durable journal (item A — test-only; makes the fresh-process proof buildable)
`tagekyc.raw_export_fixture_kek_wrap_journal` (owner `tagekyc_raw_export_deployer`; TEST-ONLY; direct DML denied; created by
the migration but never carries a production-qualified KEK, so production readiness still fails via
`PROD_RAW_EXPORT_KEK_NOT_QUALIFIED`). It is the provider's OWN durable memory (distinct from the §2d adapter mapping) that
closes the provider-success-before-DB-persistence window. **Insert-once immutable (CreateOrGet); no UPDATE ever.**
Columns: PK `FixtureWrapId uuid`; `KeyProviderId text NOT NULL`; `ProviderOperationToken text NOT NULL`;
`AttemptKeyContextFingerprint bytea NOT NULL CHECK octet_length=32` (immutable); `WrapState text NOT NULL ∈ {Wrapped,
PositivelyAbsent}`; wrapped family (NON-NULL iff `Wrapped`) `WrappedDekCiphertext bytea CHECK octet_length=32`,
`WrappedDekNonce bytea CHECK octet_length=12`, `WrappedDekTag bytea CHECK octet_length=16`, `WrappingSuiteId text`,
`WrappingSuiteVersion integer`, `ProviderResourceReference text`, `ProviderOperationReceipt text`; absence family (NON-NULL
iff `PositivelyAbsent`) `ProviderAbsenceProofReceipt text`; `CreatedAtUtc timestamptz NOT NULL`. A CHECK enforces exactly one
family populated per `WrapState`.
```
CONSTRAINT uq_raw_export_fixture_kek_journal_provider_token
  UNIQUE ("KeyProviderId","ProviderOperationToken");            -- 48 bytes ≤ 63
```
**SD functions** (owner deployer, `SET search_path = pg_catalog`, one overload, `REVOKE ALL FROM PUBLIC, tagekyc_runtime`):
- `tagekyc.raw_export_fixture_kek_wrap(p_key_provider_id text, p_provider_operation_token text,
  p_attempt_key_context_fingerprint bytea, p_wrapped_dek_ciphertext bytea, p_wrapped_dek_nonce bytea, p_wrapped_dek_tag bytea,
  p_wrapping_suite_id text, p_wrapping_suite_version integer, p_provider_resource_reference text, p_provider_operation_receipt
  text) RETURNS TABLE(wrap_state text, wrapped_dek_ciphertext bytea, wrapped_dek_nonce bytea, wrapped_dek_tag bytea,
  wrapping_suite_id text, wrapping_suite_version integer, provider_resource_reference text, provider_operation_receipt text)` —
  grantee `tagekyc_raw_export_custody_encryptor`. **CreateOrGet with concurrency CAS:** an `INSERT ON CONFLICT
  ("KeyProviderId","ProviderOperationToken") DO NOTHING` followed by a `SELECT` of the row; two concurrent wraps for the same token → one
  inserts, both read the identical immutable row. If a row already exists whose `AttemptKeyContextFingerprint` differs from
  `p_attempt_key_context_fingerprint`, it raises `P0001 RAW_EXPORT_FIXTURE_KEK_CONTEXT_MISMATCH` (never returns a different
  context's bytes). The returned wrapped bytes are always the immutable original.
- `tagekyc.raw_export_fixture_kek_lookup(p_key_provider_id text, p_provider_operation_token text,
  p_attempt_key_context_fingerprint bytea) RETURNS TABLE(outcome text, wrap_state text, wrapped_dek_ciphertext bytea,
  wrapped_dek_nonce bytea, wrapped_dek_tag bytea, wrapping_suite_id text, wrapping_suite_version integer,
  provider_resource_reference text, provider_operation_receipt text, provider_absence_proof_receipt text)` — grantee
  `tagekyc_raw_export_reconciler`. Row present + context matches → `outcome = 'Found'` (state `Wrapped`) or `'PositivelyAbsent'`;
  context mismatch → `'ContextMismatch'`; no row → `'Unknown'` (absence is NEVER inferred from a missing row — only an explicit
  `PositivelyAbsent` journal row proves absence). **Restart:** the journal is a durable table, so a fresh process reads the
  same immutable row and returns `Found`, which is exactly what test #57 depends on.
**Guard/ACL:** the journal is routed INSERT-only by the existing `trg_raw_export_attempt_key_guard` (§M2, extended to this
table); no PUBLIC/runtime/direct DML; the two functions are the only write/read surfaces. **Readiness:** the fixture provider
declares durability SUPPORTED (journal present), so `PROD_RAW_EXPORT_KEY_PROVIDER_DURABILITY_UNSUPPORTED` passes; it remains
unqualified for production via `PROD_RAW_EXPORT_KEK_NOT_QUALIFIED`. **Up/Down/reapply:** the table + two functions are created
and dropped by the migration (no orphan overload/ACL; catalog round-trip). **Named mutation tests:** #57 (fresh-process
Found), #57c (adapter returns bytes divergent from the journal → caught at the adapter layer), #57d (journal is insert-once
immutable — a direct UPDATE/DELETE raises `P0001`; a second `wrap` with a mismatched context raises
`RAW_EXPORT_FIXTURE_KEK_CONTEXT_MISMATCH`), #57e (`lookup` on a missing row returns `Unknown`, never absence), #57f (journal
functions reconciler/encryptor-only, non-owner grant/PUBLIC → `role_routine_grants` `Assert.Empty` fails and a wrong-role call
raises `42501`).

## 3. Provider + application interfaces (H4 — boundary corrected)
The public `TagEkyc.Contracts` layer exposes NO Infrastructure lease type and NO raw DEK. Contracts hold only opaque
operation-scoped capability interfaces + typed DTOs; the `AttemptDekLease` and KEK operations are Infrastructure-internal.

**Infrastructure-internal (namespace `TagEkyc.Infrastructure.RawExport`, not referenced by Contracts):**
- `internal interface IKekOperationProvider` (**durable + idempotent by `providerOperationToken`, gap 1**) —
  `WrapDekAsync(KekReference kek, string providerOperationToken, ReadOnlyMemory<byte> attemptKeyContextFingerprint,
  IAttemptDekCandidate dek, CancellationToken) → KekWrapResult` (`{WrappedDekCiphertext, WrappedDekNonce, WrappedDekTag,
  WrappingSuiteId, WrappingSuiteVersion, ProviderResourceReference, ProviderOperationReceipt}` | typed failure) — CreateOrGet:
  a second call with the same token durably returns the identical wrapped result and never re-wraps a new DEK;
  `LookupByOperationTokenAsync(string providerOperationToken, ReadOnlyMemory<byte> attemptKeyContextFingerprint,
  CancellationToken) → KekOperationLookup` where `KekOperationLookup ∈ {Found{ciphertext,nonce,tag,suiteId,suiteVersion,
  providerResourceReference,receipt}, PositivelyAbsent{absenceProofReceipt}, Unknown}` (read-only crash-window recovery); the
  `Found` payload — including `providerResourceReference` (item J) — is GUARANTEED BY THE TRUSTED PROVIDER ADAPTER to be the
  immutable original result for the (token, context) (CreateOrGet-immutable journal for the fixture, token-idempotent KMS/HSM
  in production); the recorder (§4a) validates shape/suite and recomputes digests rather than byte-comparing against an
  unknown original, and the divergent-result attack is caught at the adapter/journal layer (item B, test #57c); `UnwrapDekAsync(KekReference
  kek, ReadOnlyMemory<byte> ciphertext, nonce, tag, CancellationToken) → IAttemptDekLease | typed failure`. The provider
  declares a durability capability; a non-durable provider fails readiness `PROD_RAW_EXPORT_KEY_PROVIDER_DURABILITY_UNSUPPORTED`.
  `FixtureKekOperationProvider` satisfies durability via a provider-owned fixture journal table
  `tagekyc.raw_export_fixture_kek_wrap_journal` (test-only; fails production KEK readiness) so the crash-window test survives a
  fresh process; production providers satisfy it via native KMS/HSM idempotency behind the same interface.
- `internal interface IKekProvisioningRecoveryOperation` (reconciler-invoked) —
  `ResolveProvisioningOperationAsync(string providerOperationToken, ReadOnlyMemory<byte> attemptKeyContextFingerprint,
  CancellationToken) → KekProvisioningResolution`; `CleanupProvisioningOperationAsync(string providerCleanupReference,
  ReadOnlyMemory<byte> attemptKeyContextFingerprint, CancellationToken) → KekProvisioningCleanupResult`.
- `internal interface IAttemptDekCandidate : IDisposable` / `internal interface IAttemptDekLease : IDisposable` — confined,
  non-assignable, zeroize-on-dispose; never returned to Contracts/application.

`KekProvisioningResolution ∈ {WrappedResultRecovered{ciphertext,nonce,tag,suiteId,suiteVersion,providerResourceReference,
receipt}, NoProviderResult{absenceProofReceipt}, ProviderOutcomeUnknown, ProviderResourceCleanupRequired{cleanupReference},
ProviderUnavailable, CorruptOrUnverifiable}`. `WrappedResultRecovered` carries `providerResourceReference` (item J).
`NoProviderResult` MUST carry a non-empty bounded positive-absence receipt (H3); a missing/empty receipt is treated as
`ProviderOutcomeUnknown`, never absence.
`KekProvisioningCleanupResult ∈ {Cleaned{receipt}, AlreadyAbsent{receipt}, CleanupUnavailable, CleanupOutcomeUnknown,
CleanupFailed}`.

**Application-facing (`TagEkyc.Contracts.RawExport`, operation-scoped, no lease/DEK crosses):**
- `IAttemptKeyReservationProvisioningOperation.ProvisionAsync(AttemptKeyProvisioningRequest, CancellationToken) →
  AttemptKeyProvisioningOutcome` — internally: generate a random 32-byte DEK candidate → `WrapDekAsync` → persist+activate
  the durable reservation via SD functions → zeroize the candidate on success and failure → returns only a typed outcome
  (disposition + reservation id), never the DEK.
- `IAttemptAeadEncryptionOperation.EncryptBoundedChunkAsync(AttemptAeadEncryptRequest, CancellationToken) →
  AttemptAeadCiphertext` — locks the exact `Active` reservation, unwraps inside the internal boundary, performs one bounded
  encrypt, zeroizes the lease before returning ciphertext+tag.
- `IAttemptAeadVerificationOperation.DecryptAndVerifyBoundedChunkAsync(AttemptAeadVerifyRequest, CancellationToken) →
  AttemptAeadVerification` — same lock/unwrap/one-bounded-op/zeroize; returns plaintext or a typed verification failure.
The application orchestrator holds none of these secrets and stores no lease. AEAD seams are frame-agnostic (no R2 framing/
AAD/chunk codec is decided here).

**K11 coexistence:** the landed DEKKEK `IAttemptKeyProvider` (single `CreateOrGetAttemptKeyAsync`, process-local) is
reclassified `ProcessLocalFixture`; the durable lifecycle lives behind the interfaces above; a forbidden-edge ArchTest
proves the process-local provider is not resolvable in the durable composition.

## 4. SQL / ACL manifest (M3, M4)
All functions: SECURITY DEFINER; owner `tagekyc_raw_export_deployer`; `SET search_path = pg_catalog`; exactly one overload;
every function REVOKEs ALL from `PUBLIC` and from `tagekyc_runtime`; created by Up, dropped by Down. **M4:** every listed
*business* condition is a typed RETURN value; SQLSTATE `P0001` is raised ONLY for structural/guard violations (null/malformed
argument, unauthorized `current_user`, missing/nested write-context GUC, direct DML), with the exact `MessageText`
`RAW_EXPORT_KEY_WRITE_CONTEXT_INVALID` (guard) or `RAW_EXPORT_KEY_ARGUMENT_INVALID` (argument). No condition is listed as
both a return value and an unspecified failure.

Roles are written with their full literal (no alias). Every `GRANT EXECUTE` in the table below names the function's full
signature and the full capability-role literal (for example `GRANT EXECUTE ON FUNCTION
tagekyc.raw_export_revoke_attempt_key_reservation(uuid,text) TO tagekyc_raw_export_lifecycle`); no role alias is used.

| Function (schema-qualified, ordered arg types) | RETURNS | EXECUTE granted to | Business return values (typed) |
|---|---|---|---|
| `tagekyc.raw_export_prepare_attempt_key_reservation(uuid,uuid,uuid)` | `TABLE(outcome text, preparation_id uuid, preparation_fence bigint, provider_operation_token text, preparation_lease_expires_at_utc timestamptz)` | `tagekyc_raw_export_custody_encryptor` | `PreparingLive, ExistingMatch, InProgress, Conflict, HeadNotReserved, Terminated, StaleFence` |
| `tagekyc.raw_export_activate_attempt_key_reservation(uuid,uuid,bigint,bytea,bytea,bytea,bytea,text)` | `text` | `tagekyc_raw_export_custody_encryptor` | `Activated, StalePreparation, DigestMismatch, LeaseExpired, HeadNotCurrent, HeadNotReserved, Terminated` |
| `tagekyc.raw_export_mark_attempt_key_preparation_expired(uuid,uuid,bigint)` | `text` | `tagekyc_raw_export_reconciler` | `Expired, NotDue, StalePreparation, RetryTooSoon` |
| `tagekyc.raw_export_resolve_attempt_key_provider_outcome(uuid,uuid,bigint,text,text,text)` | `text` | `tagekyc_raw_export_reconciler` | `ResolvedNoResult, ResolvedOutcomeUnknown, ResolvedCorrupt, ResolvedCleanupRequired, ProviderUnavailableObserved, DeadlineExceeded, StalePreparation, IllegalResolution, RetryTooSoon` |
| `tagekyc.raw_export_activate_recovered_attempt_key_reservation(uuid,uuid,bigint,bytea,bytea,bytea,bytea,text,bytea)` | `text` | `tagekyc_raw_export_reconciler` | `ActivatedRecovered, StalePreparation, DigestMismatch, HeadNotCurrent, HeadNotReserved, Terminated` |
| `tagekyc.raw_export_revoke_attempt_key_reservation(uuid,text)` | `text` | `tagekyc_raw_export_reconciler`, `tagekyc_raw_export_lifecycle` | `Revoked, AlreadyRevoked, NotActive` |
| `tagekyc.raw_export_inspect_attempt_key_reservation(uuid)` | `TABLE(preparation_disposition text, attempt_id uuid, attempt_key_context_fingerprint bytea, current_preparation_id uuid, current_preparation_fence bigint, current_preparation_lease_expires_at_utc timestamptz, current_provider_operation_token text, wrapping_suite_id text, wrapping_suite_version integer, wrapped_dek_ciphertext bytea, wrapped_dek_nonce bytea, wrapped_dek_tag bytea, wrapped_dek_metadata_digest bytea, prepared_at_utc timestamptz, revoked_at_utc timestamptz, revocation_reason_code text)` | `tagekyc_raw_export_custody_encryptor`, `tagekyc_raw_export_reconciler` | rows / none |
| `tagekyc.raw_export_read_current_attempt_key_recovery_context(uuid)` | see §H8 signature | `tagekyc_raw_export_reconciler` | rows / none |

The last two rows are read-only projections (no mutation). Provider-operation adapter functions are pinned separately with
full named signatures in §4a. `resolve_attempt_key_provider_outcome` and `mark_attempt_key_preparation_expired` use
`pg_catalog.statement_timestamp()` for every lease/retry/deadline comparison (§H7).

`prepare` (H2): a `FOR UPDATE` lock is taken on the exact landed attempt row and current source head; proves `AttemptId` and
`AttemptKeyReservationId` reference the same attempt, `SourceArtifactId` matches, the current head points to that exact
attempt, `CustodyState = Reserved`, the attempt is not R2-terminated, and ownership/reservation/retention deadlines are
valid; reads frozen selectors ONLY from the locked landed row; selects the internal wrapping profile; renders every scalar
per K1; calls `tagekyc.raw_export_c1_hash_canonical` (schema-qualified) to compute `AttemptKeyContextFingerprint`; persists;
returns only the preparation handle. No caller-supplied selector/suite/context-fingerprint/token is accepted (extra caller
arguments do not exist in the signature). The helper's landed owner/ACL is unchanged and no new PUBLIC/runtime EXECUTE edge
to it is introduced (readiness `PROD_RAW_EXPORT_HASH_HELPER_ACL_INVALID` asserts the helper is not runtime-executable).
**Atomic creation (gap 2), internally-derived (gap 1):** a single `prepare` SD transaction writes ALL THREE — the head row (or
the `ReadyForFreshPreparation`→`PreparingLive` head UPDATE), the `Opened` history event, and the provider-operation `Issued`
mapping row — atomically, using an internally-derived `ProviderOperationId = pg_catalog.gen_random_uuid()`, a
`ProviderOperationToken` from a **256-bit CSPRNG rendered base64url without padding** (item I:
`rtrim(translate(encode(gen_random_bytes(32),'base64'),'+/','-_'),'=')`, where `gen_random_bytes` is provided by the
`pgcrypto` extension), and `AttemptKeyContextFingerprint` (computed here). No caller token is accepted — the token is generated
inside the SD body, so an arbitrary caller can never supply one. Readiness `PROD_RAW_EXPORT_KEY_CSPRNG_UNAVAILABLE` fails if
`pgcrypto` (or `gen_random_bytes`) is not installed. There is NO encryptor-callable
`raw_export_issue_key_provider_operation` surface: the `Issued` INSERT is inlined inside the `prepare` SD body (if factored
into an internal SQL helper, that helper has zero non-owner EXECUTE and is invoked only within `prepare`). A failure of any one
write rolls back the whole transaction, so a `PreparingLive` head can never exist without its `Opened` event and `Issued`
mapping. As a defensive backstop, `prepare` is idempotent under CAS: re-invoked for a `PreparingLive` head whose mapping is
somehow absent, it returns `InProgress` and re-inserts the missing `Issued` row for the exact current
`PreparationId`/`PreparationFence`/token rather than stranding or minting a second token. Test #58 proves the gap cannot strand
a preparation.

### 4a. Provider-operation adapter SD functions (H6 — full named signatures + CAS)
All SECURITY DEFINER, owner `tagekyc_raw_export_deployer`, `SET search_path = pg_catalog`, one overload, each REVOKEs ALL
from `PUBLIC` and from `tagekyc_runtime`, internal timestamps via `pg_catalog.statement_timestamp()`. Named parameters are typed
per field (`uuid`/`bigint`/`text`/`bytea`). Each pins predecessor→successor, exact allowed column delta, typed returns and
guard exceptions. Where a mapping transition is coupled to a head disposition transition (wrapped-result→recovered
activation; absence→`ReadyForFreshPreparation`; cleanup-required/observation/acknowledge→the head cleanup disposition), the
§4a function performs BOTH the mapping UPDATE and the coupled head UPDATE (and its history append) atomically in one SD
transaction under the same write-context GUC, so head and mapping never diverge. `resolve_attempt_key_provider_outcome`
(§4) remains the disposition entry point and delegates to these recorders; it never writes the mapping outside them.
- Provider-operation `Issued` creation has NO standalone granted function (gap 1): it is performed only inside `prepare`
  (§4). If factored into an internal SQL helper `tagekyc.raw_export_issue_key_provider_operation_internal`, that helper has
  zero non-owner EXECUTE (`REVOKE ALL FROM PUBLIC, tagekyc_runtime` and every capability role) and is invoked solely within
  the `prepare` SD body — no encryptor/reconciler/lifecycle EXECUTE edge exists (readiness + negative-ACL test #58b).
- `tagekyc.raw_export_record_recovered_key_provider_result(p_attempt_key_reservation_id uuid, p_preparation_id uuid,
  p_preparation_fence bigint, p_provider_operation_token text, p_wrapped_dek_ciphertext bytea, p_wrapped_dek_nonce bytea,
  p_wrapped_dek_tag bytea, p_wrapping_suite_id text, p_wrapping_suite_version integer, p_provider_resource_reference text,
  p_provider_operation_receipt text) RETURNS text` — grantee `tagekyc_raw_export_reconciler` (item 2). Executable fresh-process
  `Found` recovery: in one guarded SD transaction it locks the frozen context/profile, validates the wrapped shape
  (`octet_length` 32/12/16) and that `p_wrapping_suite_id`/`p_wrapping_suite_version` equal the persisted profile, moves the
  mapping `Issued → ResultObserved`, RECOMPUTES `WrappedDekMetadataDigest` from the persisted profile (no caller digest,
  item C), and performs recovered activation of the head
  (`{PreparingLive,PreparingExpiredAwaitingResolution,ProviderOutcomeUnknown} → Active`, `RecoveredActivated` event). SQL does
  not claim to independently know the provider's original bytes (item B — the trusted adapter guarantees immutability); returns
  `RecoveredActivated, ShapeInvalid, SuiteMismatch, StaleOperation, StateConflict`.
- `tagekyc.raw_export_record_key_provider_wrapped_result(p_provider_operation_id uuid, p_attempt_key_reservation_id uuid,
  p_preparation_id uuid, p_preparation_fence bigint, p_provider_operation_token text, p_wrapped_dek_ciphertext bytea,
  p_wrapped_dek_nonce bytea, p_wrapped_dek_tag bytea, p_wrapping_suite_id text, p_wrapping_suite_version integer,
  p_provider_operation_receipt text, p_provider_resource_reference text) RETURNS text` — grantee
  `tagekyc_raw_export_custody_encryptor`; `Issued → ResultObserved`; validates wrapped shape + suite equality, RECOMPUTES
  `WrappedDekMetadataDigest` from the persisted profile (item C — no caller digest); delta = wrapped-result + suite + receipt +
  recomputed digest + `ResultObservedAtUtc`; returns `ResultObserved, ExistingMatch, ShapeInvalid, SuiteMismatch, StaleOperation,
  StateConflict`.
- `tagekyc.raw_export_record_key_provider_absence(p_provider_operation_id uuid, p_attempt_key_reservation_id uuid,
  p_preparation_id uuid, p_preparation_fence bigint, p_provider_operation_token text, p_provider_absence_proof_receipt text)
  RETURNS text` — grantee `tagekyc_raw_export_reconciler`; `Issued → AbsenceProven`; delta = `ProviderAbsenceProofReceipt` +
  `ProviderAbsenceObservedAtUtc`; all wrapped/resource/cleanup fields remain NULL; returns `AbsenceProven, ExistingMatch,
  StaleOperation, StateConflict`.
- `tagekyc.raw_export_mark_key_provider_cleanup_required(p_provider_operation_id uuid, p_attempt_key_reservation_id uuid,
  p_preparation_id uuid, p_preparation_fence bigint, p_provider_operation_token text, p_provider_cleanup_reference text)
  RETURNS text` — grantee `tagekyc_raw_export_reconciler`; `{Issued,ResultObserved} → CleanupRequired`; returns
  `CleanupRequired, ExistingMatch, StaleOperation, StateConflict`.
- `tagekyc.raw_export_record_key_provider_cleanup_observation(p_provider_operation_id uuid, p_attempt_key_reservation_id uuid,
  p_preparation_id uuid, p_preparation_fence bigint, p_provider_operation_token text, p_cleanup_result_kind text,
  p_provider_cleanup_reference text, p_provider_cleanup_receipt text) RETURNS text` — grantee `tagekyc_raw_export_reconciler`;
  `CleanupRequired → CleanupRequired` (unchanged). **Gap 4:** SQL RECOMPUTES the mandatory `CleanupObservationEvidenceDigest =
  C1HashCanonical("tip-88c1-key-cleanup-observation-evidence-v1", AttemptKeyContextFingerprint[hex], PreparationId[N-guid],
  PreparationFence[dec], CleanupResultKind, opt(ProviderCleanupReceipt))` from the LOCKED head's frozen identity (absent
  receipt → tagged scalar `"0"`; present → `"1"` then the receipt) and appends `CleanupAttemptObserved` carrying it; NO
  observation is persisted without this digest; increments head `CleanupAttemptCount`, sets `NextCleanupAttemptNotBeforeUtc`;
  `p_cleanup_result_kind ∈ {CleanupUnavailable, CleanupOutcomeUnknown, CleanupFailed}`; returns `CleanupObserved, RetryTooSoon,
  DeadlineIntervention, StaleOperation, IllegalCleanupKind`.
- `tagekyc.raw_export_acknowledge_key_provider_cleanup(p_provider_operation_id uuid, p_attempt_key_reservation_id uuid,
  p_preparation_id uuid, p_preparation_fence bigint, p_provider_operation_token text, p_cleanup_result_kind text,
  p_provider_cleanup_reference text, p_provider_cleanup_receipt text, p_provider_cleanup_evidence_digest bytea) RETURNS text` —
  grantee `tagekyc_raw_export_reconciler`; `CleanupRequired → CleanedUp` (`p_cleanup_result_kind ∈ {Cleaned,AlreadyAbsent}`);
  drives the head `ProviderCleanupRequired → ReadyForFreshPreparation`; returns `CleanedUp, EvidenceMissing, IllegalCleanupKind,
  StaleOperation, StateConflict`.
- `tagekyc.raw_export_request_abandon_attempt_key_reservation(p_attempt_key_reservation_id uuid, p_preparation_id uuid,
  p_preparation_fence bigint, p_provider_operation_token text, p_operator_reason_code text) RETURNS text` — grantee
  `tagekyc_raw_export_lifecycle`. **Item E phase 1:** head
  `{PreparingLive,PreparingExpiredAwaitingResolution,ProviderOutcomeUnknown,ProviderCleanupRequired} → AbandonRequested`
  (non-terminal, blocking); appends an immutable `AbandonRequested` event carrying `ProviderOperationToken, OperatorReasonCode,
  OperatorActorEvidence, HeadRowRevision, EventAtUtc`. **Item F:** `OperatorActorEvidence` is NOT caller-authored — SQL derives
  it from the trusted authenticated-actor GUC via `tagekyc.raw_export_current_actor()` (fail-closed: an empty/missing context
  raises `P0001 RAW_EXPORT_KEY_ACTOR_CONTEXT_MISSING`); only `OperatorReasonCode` is a bounded caller input. Readiness
  `PROD_RAW_EXPORT_KEY_ABANDON_PENDING` fails while pending. Returns `AbandonRequested, AlreadyRequested, StaleOperation,
  StateConflict`.
- `tagekyc.raw_export_finalize_abandon_attempt_key_reservation(p_attempt_key_reservation_id uuid, p_preparation_id uuid,
  p_preparation_fence bigint, p_provider_operation_token text) RETURNS text` — grantee `tagekyc_raw_export_lifecycle`.
  **Item E phase 2:** head `AbandonRequested → ReservationAbandoned` PERMITTED ONLY when the provider-operation mapping is
  `CleanedUp` (provider resource cleaned) or `AbsenceProven` (positively absent), never on operator authorization alone; else
  returns `CleanupNotComplete`. Appends an immutable `ProviderOperationAbandoned` event carrying `ProviderOperationToken,
  OperatorReasonCode, OperatorActorEvidence (derived), HeadRowRevision, EventAtUtc` — a dedicated payload, `RevocationReasonCode`
  MUST be NULL there. A completed `ReservationAbandoned` does NOT fail global readiness (the resource is provably gone); every
  authorized op leaves it terminal/unchanged. Returns `ReservationAbandoned, CleanupNotComplete, NotRequested, StaleOperation,
  StateConflict`.
- `tagekyc.raw_export_inspect_key_provider_operation(p_attempt_key_reservation_id uuid) RETURNS TABLE(provider_operation_id
  uuid, provider_operation_state text, key_provider_id text, provider_operation_token text, preparation_id uuid,
  preparation_fence bigint, wrapped_dek_ciphertext bytea, wrapped_dek_nonce bytea, wrapped_dek_tag bytea,
  wrapped_dek_metadata_digest bytea, wrapping_suite_id text, wrapping_suite_version integer, provider_resource_reference text,
  provider_operation_receipt text, provider_cleanup_reference text, provider_cleanup_receipt text,
  provider_absence_proof_receipt text, result_observed_at_utc timestamptz, provider_absence_observed_at_utc timestamptz)` —
  grantee `tagekyc_raw_export_reconciler`; read-only current-generation projection. Business conditions above are typed
  returns; only guard/argument violations raise `P0001` (§M4); a missing EXECUTE raises `PostgresException` SQLSTATE `42501`.

**Revoke evidence (gap 6):** `raw_export_revoke_attempt_key_reservation(p_attempt_key_reservation_id uuid,
p_revocation_reason_code text)` binds the reason into the immutable `Revoked` event: it stores `RevocationReasonCode` on the
event AND SQL RECOMPUTES `RevocationEvidenceDigest = C1HashCanonical("tip-88c1-key-revocation-evidence-v1",
AttemptKeyContextFingerprint[hex], PreparationId[N-guid], PreparationFence[dec], RevocationReasonCode)` from the LOCKED head's
frozen identity; the head `RevocationReasonCode`/`RevokedAtUtc` are set in the same transaction. The reason is not free-floating
head state — it is anchored in the append-only event and its recomputed digest.

## 5. Restart / recovery traces (H8)
Add a reconciler-only exact read surface:
```
tagekyc.raw_export_read_current_attempt_key_recovery_context(p_attempt_key_reservation_id uuid)
RETURNS TABLE(
  attempt_key_reservation_id uuid, attempt_id uuid, preparation_disposition text,
  current_preparation_id uuid, current_preparation_fence bigint,
  current_preparation_lease_expires_at_utc timestamptz, provider_operation_token text,
  attempt_key_context_fingerprint bytea, wrapping_suite_id text, wrapping_suite_version integer,
  latest_event_sequence bigint, latest_event_kind text,
  latest_resolution_event_sequence bigint, latest_resolution_kind text, latest_provider_resolution_evidence_digest bytea,
  latest_cleanup_required_event_sequence bigint, latest_cleanup_required_reference text,
  latest_cleanup_attempt_event_sequence bigint, latest_cleanup_result_kind text, latest_provider_cleanup_evidence_digest bytea,
  latest_activation_event_sequence bigint, latest_activation_kind text,
  mapping_provider_operation_state text, mapping_provider_operation_receipt text, mapping_provider_cleanup_reference text,
  mapping_provider_cleanup_receipt text, mapping_provider_absence_proof_receipt text, mapping_wrapped_dek_metadata_digest bytea,
  next_resolution_attempt_not_before_utc timestamptz, resolution_deadline_utc timestamptz, resolution_attempt_count bigint,
  next_cleanup_attempt_not_before_utc timestamptz, cleanup_deadline_utc timestamptz, cleanup_attempt_count bigint,
  cleanup_operator_intervention_required boolean,
  latest_abandonment_event_sequence bigint, abandonment_provider_operation_token text, abandonment_operator_reason_code text,
  abandonment_operator_actor_evidence text, abandonment_head_row_revision bigint, abandonment_event_at_utc timestamptz)
```
**M3 — event-family safe:** the projection is scoped to the current preparation generation (`WHERE PreparationId =
head.CurrentPreparationId AND PreparationFence = head.CurrentPreparationFence`) and returns, for that generation, the
deterministic latest-per-family (`ORDER BY EventSequence DESC LIMIT 1` computed independently within each family): latest
overall event, latest provider-resolution event, latest cleanup-required event, latest cleanup-attempt observation, latest
activation event, and the independent latest-abandonment family (item G: abandonment event sequence, `ProviderOperationToken`,
`OperatorReasonCode`, trusted `OperatorActorEvidence`, `HeadRowRevision`, `EventAtUtc`) — plus the current authoritative
provider-operation mapping state and its durable evidence. A crash-window `Issued` mapping awaiting `Found` recovery, an
`AbandonRequested` head pending cleanup/absence, and a terminal `ReservationAbandoned` head (finalized from mapping `CleanedUp`
or `AbsenceProven`, with the latest `AbandonRequested`+`ProviderOperationAbandoned` events) are all fully reconstructable from
this projection. It never relies on one latest row to carry every evidence family, never lists generic history, is `EXECUTE`-granted only to
`tagekyc_raw_export_reconciler` (PUBLIC/`tagekyc_runtime`/encryptor/lifecycle revoked), SECURITY DEFINER, `SET search_path =
pg_catalog`, one overload; owner/overload/ACL are catalog-tested. Every provider outcome traces `provider-result → persisted
§2c event (+ §2d mapping row) → this read surface → permitted §1 next transition → terminal | retry (§H6)`. The restart test
constructs fresh `DbContext`, repository, service and provider instances and resumes using only persisted state.

## 6. Readiness (M5 — full literals, landed reconciliation, precedence)
**Topology dispatch (item D).** Registration is per topology, so validators never cross-fire:
- `ProcessLocalFixture` topology: the landed `AttemptKeyReadinessValidator` (codes `PROD_RAW_EXPORT_ATTEMPT_KEY_PROFILE_MISSING`,
  `PROD_RAW_EXPORT_ATTEMPT_KEY_PROFILE_INVALID`, `PROD_RAW_EXPORT_ATTEMPT_KEY_FIXTURE_ACTIVE`) applies; the durable validators
  are NotApplicable and NOT registered.
- `DurableKey` topology: the durable validators below apply; the landed fixture `AttemptKeyReadinessValidator` is NotApplicable
  and NOT registered — so a qualified durable PRODUCTION provider is never rejected by `PROFILE_MISSING`/`PROFILE_INVALID`/
  `FIXTURE_ACTIVE` (test #37b). DI registers exactly one topology's validator set; the aggregate endpoint iterates only the
  registered set in the order below.
The landed three codes are retained (not renamed/removed) for the fixture topology. Durable-topology codes (each fully
spelled), grouped by validator, evaluated first-match within a validator:
- `CustodyRoleReadinessValidator` (queries `pg_roles` + `pg_auth_members`), first-match order: `PROD_RAW_EXPORT_CUSTODY_ROLE_MISSING`
  → `PROD_RAW_EXPORT_CUSTODY_ROLE_ATTRIBUTE_INVALID` → `PROD_RAW_EXPORT_CUSTODY_LOGIN_ATTRIBUTE_INVALID` →
  `PROD_RAW_EXPORT_CUSTODY_ROLE_GRANT_INVALID` → `PROD_RAW_EXPORT_CUSTODY_ROLE_CROSS_MEMBERSHIP` →
  `PROD_RAW_EXPORT_CUSTODY_ROLE_SET_ROLE_ENABLED`.
- `DurableKeyProviderReadinessValidator`, first-match order: `PROD_RAW_EXPORT_PROVIDER_TOPOLOGY_INVALID` →
  `PROD_RAW_EXPORT_KEY_PROVIDER_DURABILITY_UNSUPPORTED` → `PROD_RAW_EXPORT_KEY_CSPRNG_UNAVAILABLE` →
  `PROD_RAW_EXPORT_KEK_NOT_QUALIFIED` → `PROD_RAW_EXPORT_HASH_HELPER_ACL_INVALID` → `PROD_RAW_EXPORT_KEY_RETRY_CONFIG_INVALID`.
- `KeyReservationReadinessValidator`, first-match order: `PROD_RAW_EXPORT_KEY_RESERVATION_STATE_INVALID` →
  `PROD_RAW_EXPORT_KEY_PREPARATION_HISTORY_INVALID` → `PROD_RAW_EXPORT_KEY_PREPARATION_LEASE_INVALID` →
  `PROD_RAW_EXPORT_KEY_OPERATION_RECOVERY_UNSUPPORTED` → `PROD_RAW_EXPORT_KEY_PROVIDER_CLEANUP_UNSUPPORTED` →
  `PROD_RAW_EXPORT_KEY_PROVIDER_CLEANUP_OUTCOME_UNKNOWN` → `PROD_RAW_EXPORT_KEY_PROVIDER_CLEANUP_OPERATOR_INTERVENTION` →
  `PROD_RAW_EXPORT_KEY_PROVIDER_CORRUPT_UNRESOLVED` → `PROD_RAW_EXPORT_KEY_ABANDON_PENDING`. A **pending** `AbandonRequested`
  fails readiness via `PROD_RAW_EXPORT_KEY_ABANDON_PENDING`; a **completed** terminal `ReservationAbandoned` does NOT fail
  global readiness (item E — a legitimate completed abandonment must not fail readiness forever), exactly like `Revoked`.
Aggregate endpoint ordering, `DurableKey` topology: `CustodyRoleReadinessValidator` → `DurableKeyProviderReadinessValidator`
→ `KeyReservationReadinessValidator` (the landed fixture validator is NOT registered here). `ProcessLocalFixture` topology:
only the landed `AttemptKeyReadinessValidator` is registered. The endpoint returns the ordered union of the REGISTERED set
(each validator internally first-match) — the two sets never both run. Every code names its readiness type/file and its exact
positive + negative fixture; each named test asserts the exact code string it expects.

## 7. Durable evidence + golden vectors (H3)
Wrapping profile #1: `WrappingSuiteId = "AES-256-GCM"`, `WrappingSuiteVersion = 1`, DEK 32 bytes, wrapped ciphertext 32
bytes, nonce 12 bytes, tag 16 bytes. Preimages use the single landed codec (K1):
- `AttemptKeyContextFingerprint = C1HashCanonical("tip-88c1-attempt-key-context-v1", AttemptKeyReservationId[N-guid],
  AttemptId[N-guid], EncryptionAttemptFingerprint[hex], KeyProviderId, KekId, KekVersion[dec], KekFingerprint, WrappingSuiteId,
  WrappingSuiteVersion[dec])`.
- `WrappedDekMetadataDigest = C1HashCanonical("tip-88c1-wrapped-dek-metadata-v1", AttemptKeyContextFingerprint[hex],
  WrappingSuiteId, WrappingSuiteVersion[dec], WrappedDekNonce[hex], WrappedDekCiphertext[hex], WrappedDekTag[hex])`.
- `ProviderResolutionEvidenceDigest = C1HashCanonical("tip-88c1-key-provider-resolution-evidence-v1",
  AttemptKeyContextFingerprint[hex], PreparationId[N-guid], PreparationFence[dec], ResolutionKind, ProviderOperationToken,
  opt(ProviderOperationReceipt), opt(ProviderCleanupReference), opt(WrappedDekMetadataDigest[hex]))`.
- `ProviderCleanupEvidenceDigest = C1HashCanonical("tip-88c1-key-provider-cleanup-evidence-v1",
  AttemptKeyContextFingerprint[hex], PreparationId[N-guid], PreparationFence[dec], CleanupResultKind, ProviderCleanupReference,
  ProviderCleanupReceipt)`. `opt(x)` = tagged scalars per K1 (`"0"` absent; `"1"`,value present).
- `ProviderAbsenceEvidenceDigest = C1HashCanonical("tip-88c1-key-provider-absence-evidence-v1",
  AttemptKeyContextFingerprint[hex], PreparationId[N-guid], PreparationFence[dec], "AbsenceProven", ProviderOperationToken,
  ProviderAbsenceProofReceipt)` — a distinct domain (H3) so positive absence is not conflated with a resolution digest.
- `CleanupObservationEvidenceDigest = C1HashCanonical("tip-88c1-key-cleanup-observation-evidence-v1",
  AttemptKeyContextFingerprint[hex], PreparationId[N-guid], PreparationFence[dec], ProviderCleanupReference, CleanupResultKind,
  opt(ProviderCleanupReceipt))` (gap 4; **item J adds `ProviderCleanupReference` before `CleanupResultKind`**) — mandatory on
  every `CleanupAttemptObserved`; absent receipt → tagged `"0"`.
- `RevocationEvidenceDigest = C1HashCanonical("tip-88c1-key-revocation-evidence-v1", AttemptKeyContextFingerprint[hex],
  PreparationId[N-guid], PreparationFence[dec], RevocationReasonCode)` (gap 6) — bound into the immutable `Revoked` event.
- **Item J storage pin:** for `ResolvedNoResult`, the stored `ProviderResolutionEvidenceDigest` IS the
  `ProviderAbsenceEvidenceDigest` (identical value) — positive absence is not given a second, divergent digest.

**Golden vectors (independently recomputed).** Fixture inputs (synthetic, non-patient): `AttemptKeyReservationId =
11111111-1111-4111-8111-111111111111`; `AttemptId = 22222222-2222-4222-8222-222222222222`; `EncryptionAttemptFingerprint =
0xAB×32`; `KeyProviderId = "fixture-kek-local"`; `KekId = "fixture-kek-1"`; `KekVersion = 1`; `KekFingerprint =
"fixture-kek-fpr-v1"`; `WrappingSuiteId = "AES-256-GCM"`; `WrappingSuiteVersion = 1`; `WrappedDekNonce =
0102030405060708090a0b0c`; `WrappedDekCiphertext` = the 32 bytes 0x20..0x3f in ascending order; `WrappedDekTag` = the 16
bytes 0x40..0x4f in ascending order; `PreparationId = 33333333-3333-4333-8333-333333333333`; **`PreparationFence = 1`** (M2 —
first fence is 1, changed from the v0.2 value 0); `ProviderOperationToken =
"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"` (base64url of 32 zero bytes); resolution optionals = receipt
`"receipt-fixture-0001"` present, cleanup-reference absent, metadata-digest present; cleanup `CleanupResultKind = "Cleaned"`,
`ProviderCleanupReference = "cleanup-ref-fixture-0001"`, `ProviderCleanupReceipt = "cleanup-rcpt-fixture-0001"`; absence
`ProviderAbsenceProofReceipt = "absence-rcpt-fixture-0001"`.

`AttemptKeyContextFingerprint` and `WrappedDekMetadataDigest` do not depend on `PreparationFence` and are unchanged from v0.2;
`ProviderResolutionEvidenceDigest`, `ProviderCleanupEvidenceDigest` change with fence 0→1; `ProviderAbsenceEvidenceDigest` is
new. Old-vs-new (fence-dependent):

| Digest | v0.2 value (fence 0) | v0.3 value (fence 1) |
|---|---|---|
| `ProviderResolutionEvidenceDigest` | `50541858348bdd66a6ed55ee794450ccd681a6b5197919a167d99637d10bacce` | `b30d9e7b6ce91af6f42bf826bd05daacc0c17bcb1409051b3517de5cc3f2c2dd` |
| `ProviderCleanupEvidenceDigest` | `0cebfc8eacf9ba1b217ecbdafae0a851870c12e5a372cdfde8533d0016706d0a` | `d3f47850ae3cfa8cf233e85633b609201313bfa8d301b5689311f34e16dc54bb` |

| Digest (v0.3) | Value (lowercase hex, 32 bytes) |
|---|---|
| `AttemptKeyContextFingerprint` | `37108746b02b4a73070aafdb116ceeb15ba5b18399286b92fe38c40fc86c7793` |
| `WrappedDekMetadataDigest` | `607a19029096528f90db3fef24c5ba2229bf3cf86a5972932b00622503889d30` |
| `ProviderResolutionEvidenceDigest` | `b30d9e7b6ce91af6f42bf826bd05daacc0c17bcb1409051b3517de5cc3f2c2dd` |
| `ProviderCleanupEvidenceDigest` | `d3f47850ae3cfa8cf233e85633b609201313bfa8d301b5689311f34e16dc54bb` |
| `ProviderAbsenceEvidenceDigest` | `cab1a516ae8a2bd75fd260c529f2b35dd1a4d430a2d4f4ed9a733ea6bfd714d9` |
| `CleanupObservationEvidenceDigest` (ref `cleanup-ref-fixture-0001`, kind `CleanupFailed`, receipt absent) | `72c5086615ad0b49b5c8952dd6ea3d3dbfbe5c9a670aaf30f2550d07479969ed` |
| `RevocationEvidenceDigest` (reason `operator-revoke-fixture-0001`) | `af78d56210a6e7ba6cea7b0c05df1d1544742ebad279e94162d03a47cc223204` |

Independent recomputation command (must reproduce the seven values above byte-for-byte — five carried from v0.3 plus the two
v0.4 additions):
```
python - <<'PY'
import hashlib,unicodedata,struct
def lp(s):
    b=unicodedata.normalize('NFC',s).encode('utf-8'); return struct.pack('>I',len(b))+b
def c1(d,*f):
    buf=lp(d)
    for x in f: buf+=lp(x)
    return hashlib.sha256(buf).hexdigest()
ctx=c1("tip-88c1-attempt-key-context-v1","11111111111141118111111111111111","22222222222242228222222222222222",
       "ab"*32,"fixture-kek-local","fixture-kek-1","1","fixture-kek-fpr-v1","AES-256-GCM","1")
nonce="0102030405060708090a0b0c"; ct="".join(f"{b:02x}" for b in range(0x20,0x40)); tag="".join(f"{b:02x}" for b in range(0x40,0x50))
meta=c1("tip-88c1-wrapped-dek-metadata-v1",ctx,"AES-256-GCM","1",nonce,ct,tag)
res=c1("tip-88c1-key-provider-resolution-evidence-v1",ctx,"33333333333343338333333333333333","1",
       "WrappedResultRecovered","A"*43,"1","receipt-fixture-0001","0","1",meta)
cln=c1("tip-88c1-key-provider-cleanup-evidence-v1",ctx,"33333333333343338333333333333333","1",
       "Cleaned","cleanup-ref-fixture-0001","cleanup-rcpt-fixture-0001")
absd=c1("tip-88c1-key-provider-absence-evidence-v1",ctx,"33333333333343338333333333333333","1",
        "AbsenceProven","A"*43,"absence-rcpt-fixture-0001")
obs=c1("tip-88c1-key-cleanup-observation-evidence-v1",ctx,"33333333333343338333333333333333","1","cleanup-ref-fixture-0001","CleanupFailed","0")
rev=c1("tip-88c1-key-revocation-evidence-v1",ctx,"33333333333343338333333333333333","1","operator-revoke-fixture-0001")
print(ctx); print(meta); print(res); print(cln); print(absd); print(obs); print(rev)
PY
```
SQL RECOMPUTES resolution/cleanup/absence/cleanup-observation/revocation digests from persisted trusted context + supplied
typed provider evidence; a caller-supplied digest is never authoritative (recovered activation recomputes with the PERSISTED
suite/profile, §H9).

## 8. Role topology (K10 — full, standalone)
Capability roles minted here (`NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS`, `ADMIN OPTION=false`):
`tagekyc_raw_export_custody_encryptor`, `tagekyc_raw_export_reconciler`, `tagekyc_raw_export_lifecycle`. Deployment LOGIN
roles (credentials at deploy; names pinned): `tagekyc_raw_export_encryptor_login`, `tagekyc_raw_export_reconciler_login`,
`tagekyc_raw_export_lifecycle_login` (`LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS`). Membership: one
LOGIN → exactly one capability role, `INHERIT=true`, `SET=false`, `ADMIN OPTION=false`; no nesting/cross-membership. Down:
revoke every exact privilege/default-privilege created here, preserve deployment-owned LOGIN roles, DROP only the three
migration-owned capability roles, no broad `DROP OWNED BY`; prove no orphan ACL/default-ACL/membership + clean reapply.

## M1. Lifecycle nullability / immutability matrices
**M1.1 Head nullable-field matrix (per disposition; ✓=NON-NULL required, ∅=NULL required, –=either).**

| Column | PreparingLive | PrepExpired | ProvOutcomeUnknown | ProvCorrupt | ProvCleanupReq | ReadyForFresh | Active | Revoked | AbandonReq | ResvAbandoned |
|---|---|---|---|---|---|---|---|---|---|---|
| `CurrentPreparationId` | ✓ | ✓ | ✓ | ✓ | ✓ | ∅ | ✓ | ✓ | ✓ | ✓ |
| `CurrentPreparationLeaseExpiresAtUtc` | ✓ | ✓ | ✓ | ✓ | ✓ | ∅ | ✓ | ✓ | ✓ | – |
| `CurrentProviderOperationToken` | ✓ | ✓ | ✓ | ✓ | ✓ | ∅ | ✓ | ✓ | ✓ | ✓ |
| `ResolutionDeadlineUtc` | ✓ | ✓ | ✓ | ✓ | – | ∅ | – | – | – | ∅ |
| `NextResolutionAttemptNotBeforeUtc` | ∅ | ✓ | ✓ | ✓ | ∅ | ∅ | ∅ | ∅ | ∅ | ∅ |
| `CleanupDeadlineUtc` | ∅ | ∅ | ∅ | ∅ | ✓ | ∅ | ∅ | ∅ | – | ∅ |
| `NextCleanupAttemptNotBeforeUtc` | ∅ | ∅ | ∅ | ∅ | – | ∅ | ∅ | ∅ | – | ∅ |
| `WrappedDekCiphertext`/`Nonce`/`Tag`/`MetadataDigest` | ∅ | ∅ | ∅ | ∅ | ∅ | ∅ | ✓ | ✓ | ∅ | ∅ |
| `PreparedAtUtc` | ∅ | ∅ | ∅ | ∅ | ∅ | ∅ | ✓ | ✓ | ∅ | ∅ |
| `RevokedAtUtc`/`RevocationReasonCode` | ∅ | ∅ | ∅ | ∅ | ∅ | ∅ | ∅ | ✓ | ∅ | ∅ |

`AbandonRequested` (non-terminal, blocking) and `ReservationAbandoned` (terminal) are explicit columns above (item H); the
head carries NO abandonment evidence columns — token/reason/trusted-actor/`HeadRowRevision` live only in the immutable
`AbandonRequested` and `ProviderOperationAbandoned` events (item G/H). A head CHECK enforces both sparse shapes (test #59b).

**M1.2 Head immutable / allowed-delta manifest.** Immutable after row insert: `AttemptKeyReservationId`, `AttemptId`,
`EncryptionAttemptFingerprint`, `KeyProviderId`, `KekId`, `KekVersion`, `KekFingerprint`, `AttemptKeyContextFingerprint`,
`WrappingSuiteId`, `WrappingSuiteVersion`, `CreatedAtUtc`. Generated internally, never caller-set: `CurrentPreparationFence`,
`RowRevision`, all `*AtUtc`, `AttemptKeyContextFingerprint`. Wrapped-state columns move `∅→NON-NULL` only on activation and
are immutable thereafter (revoke does not erase them, §H10). Allowed OLD→NEW deltas are pinned per function in the CHECK/
trigger (§M2); any other column delta is rejected.

**M1.3 History per-EventKind NON-NULL matrix (evidence columns; others NULL).**

| EventKind | required NON-NULL evidence |
|---|---|
| `Opened` | `PreparationId,PreparationFence,ProviderOperationToken,WrappingSuiteId,WrappingSuiteVersion` |
| `Expired` | `PreparationId,PreparationFence` |
| `DirectActivated` | `PreparationId,PreparationFence,ProviderOperationReceipt,WrappedDekMetadataDigest,WrappingSuiteId,WrappingSuiteVersion` |
| `RecoveredActivated` | `PreparationId,PreparationFence,ProviderOperationReceipt,WrappedDekMetadataDigest,WrappingSuiteId,WrappingSuiteVersion,ResolutionKind,ProviderResolutionEvidenceDigest` |
| `ResolvedNoResult`/`ResolvedOutcomeUnknown`/`ResolvedCorrupt` | `PreparationId,PreparationFence,ResolutionKind,ProviderResolutionEvidenceDigest` |
| `ResolvedCleanupRequired` | `PreparationId,PreparationFence,ResolutionKind,ProviderResolutionEvidenceDigest,ProviderCleanupReference` |
| `ProviderUnavailableObserved` | `PreparationId,PreparationFence,ResolutionKind` |
| `CleanupAttemptObserved` | `PreparationId,PreparationFence,ProviderCleanupReference,CleanupResultKind,CleanupObservationEvidenceDigest` (mandatory digest, §gap 4; `ProviderCleanupReceipt` tagged-optional) |
| `CleanupAcknowledged` | `PreparationId,PreparationFence,CleanupResultKind,ProviderCleanupReference,ProviderCleanupReceipt,ProviderCleanupEvidenceDigest` |
| `AbandonRequested` | `PreparationId,PreparationFence,ProviderOperationToken,OperatorReasonCode,OperatorActorEvidence,HeadRowRevision` (item E phase 1; `OperatorActorEvidence` trusted-derived, item F; `RevocationReasonCode` MUST be NULL) |
| `ProviderOperationAbandoned` | `PreparationId,PreparationFence,ProviderOperationToken,OperatorReasonCode,OperatorActorEvidence,HeadRowRevision` (item E phase 2; `OperatorActorEvidence` trusted-derived; `RevocationReasonCode` MUST be NULL) |
| `Revoked` | `PreparationId,PreparationFence,RevocationReasonCode,RevocationEvidenceDigest` (§gap 6) |

Enforced by a per-row CHECK constraint keyed on `EventKind`, not by test names alone; every history row is immutable
(append-only trigger). `CleanupOperatorInterventionRequired` is a NOT NULL boolean (default false) settable true only in the
`ProviderCleanupRequired` disposition once `CleanupDeadlineUtc` is exceeded.

**M1.4 Provider-operation mapping state matrix (H1/H2; predecessor → required NON-NULL delta; identity columns
`ProviderOperationId,KeyProviderId,ProviderOperationToken,AttemptKeyReservationId,PreparationId,PreparationFence,
AttemptKeyContextFingerprint` immutable in every state):**

| State | Permitted predecessor | Required NON-NULL (beyond identity) | Must stay NULL |
|---|---|---|---|
| `Issued` | (insert) | `IssuedAtUtc` | all wrapped/resource/receipt/cleanup/absence |
| `ResultObserved` | `Issued` | `WrappedDekCiphertext,WrappedDekNonce,WrappedDekTag,WrappedDekMetadataDigest,WrappingSuiteId,WrappingSuiteVersion,ProviderOperationReceipt,ResultObservedAtUtc` | absence fields |
| `AbsenceProven` | `Issued` | `ProviderAbsenceProofReceipt,ProviderAbsenceObservedAtUtc` | all wrapped/resource/cleanup fields |
| `CleanupRequired` | `Issued`,`ResultObserved` | `ProviderCleanupReference` | absence fields |
| `CleanedUp` | `CleanupRequired` | `ProviderCleanupReceipt` | absence fields |

There is NO mapping `Abandoned` state (item E). Abandonment is a head-level two-phase lifecycle whose evidence
(token/reason/trusted-actor/`HeadRowRevision`) lives ONLY in the immutable `AbandonRequested` and `ProviderOperationAbandoned`
events (item H), never on the mapping or head; finalize-abandon is gated on the mapping already being `CleanedUp` or
`AbsenceProven`. Enforced by CHECK; the only legal mapping edges are those in §2d's state graph.

**M1.5 Lease / retry configuration (concrete values + ranges; DI options `DurableKeyCustodyOptions`).** No `backoff(count)`
or "pinned max" is left symbolic; the exponential formula is in §1. Out-of-range, malformed or overflowing configuration
fails closed at readiness with `PROD_RAW_EXPORT_KEY_RETRY_CONFIG_INVALID` (no fresh DEK).

| Config key | Default | Valid range |
|---|---|---|
| `PreparationLeaseDuration` | 15 min | [1 min, 24 h] |
| `ResolutionInitialRetryDelay` | 30 s | [5 s, 1 h] |
| `ResolutionRetryMultiplier` | 2.0 | [1.0, 10.0] |
| `ResolutionMaxBackoff` | 30 min | [`ResolutionInitialRetryDelay`, 24 h] |
| `ResolutionDeadline` | 24 h | [`PreparationLeaseDuration`, 30 d] |
| `ResolutionMaxAttemptCount` (0 = deadline governs) | 0 | [0, 1 000 000] |
| `CleanupInitialRetryDelay` | 1 min | [5 s, 1 h] |
| `CleanupRetryMultiplier` | 2.0 | [1.0, 10.0] |
| `CleanupMaxBackoff` | 1 h | [`CleanupInitialRetryDelay`, 24 h] |
| `CleanupDeadline` | 7 d | [1 h, 90 d] |
| `BoundedAeadOperationDuration` | 30 s | [1 s, 5 min] |

`NextCleanupAttemptNotBeforeUtc = statement_timestamp() + min(CleanupInitialRetryDelay × CleanupRetryMultiplier^(CleanupAttemptCount−1),
CleanupMaxBackoff)`. `bigint` counter overflow fails closed with `RAW_EXPORT_KEY_ARGUMENT_INVALID`.

**Gap 5 — `ResolutionMaxAttemptCount` semantics + deadline init.** `ResolutionMaxAttemptCount = 0` means "attempt count does
not cap; only `ResolutionDeadlineUtc` governs". When `> 0`, the reservation transitions to `ProviderCorruptOrUnverifiable`
(terminal operator-intervention, `ResolvedCorrupt`) as soon as `ResolutionAttemptCount ≥ ResolutionMaxAttemptCount`, even if
the deadline has not passed. `ResolutionDeadlineUtc` is initialized once, at first entry into
`PreparingExpiredAwaitingResolution` for a generation, to `statement_timestamp() + ResolutionDeadline`; `CleanupDeadlineUtc`
once, at first entry into `ProviderCleanupRequired`, to `statement_timestamp() + CleanupDeadline`; both are immutable for that
generation and reset only when a fresh preparation generation opens.

## M2. Exact write guards
Two append/mutate guards, one per writable table pair, matching the landed termination-guard precision.

`tagekyc.raw_export_attempt_key_reservations` + `tagekyc.raw_export_attempt_key_preparation_events` + `tagekyc.raw_export_key_provider_operations`:
- Trigger `trg_raw_export_attempt_key_guard` → function `tagekyc.raw_export_attempt_key_guard()`.
- `TG_TABLE_SCHEMA = 'tagekyc'`; `TG_TABLE_NAME ∈ {'raw_export_attempt_key_reservations','raw_export_attempt_key_preparation_events','raw_export_key_provider_operations','raw_export_fixture_kek_wrap_journal'}` (routes by name; the fixture journal is INSERT-only, item A).
- Requires `current_user = 'tagekyc_raw_export_deployer'` (the SD function owner) AND transaction-local GUC
  `tagekyc.raw_export_attempt_key_write_context = 'active'`; a missing, empty, or already-active (nested) context raises
  `P0001 RAW_EXPORT_KEY_WRITE_CONTEXT_INVALID`.
- Prior GUC value captured at function entry via `current_setting('tagekyc.raw_export_attempt_key_write_context', true)`;
  restored on BOTH success and exception (an `EXCEPTION WHEN OTHERS` handler re-applies the captured prior value with
  `set_config` at transaction-local scope, then re-raises).
- Allowed operations: head table INSERT (prepare) + UPDATE with the per-function allowed column delta; history table INSERT
  only; **mapping table INSERT (issue) + the exact per-§4a-function UPDATE delta (H2)** — mapping UPDATE is permitted only
  through the §4a SECURITY DEFINER functions, which advance `ProviderOperationState` along the §2d graph and re-check
  `ProviderOperationId,AttemptKeyReservationId,PreparationId,PreparationFence,KeyProviderId,ProviderOperationToken`, the
  expected prior state and the current reservation/preparation generation. UPDATE/DELETE on history, DELETE on mapping, any
  mapping UPDATE that changes an immutable-identity column or runs under a stale preparation/fence/token, any UPDATE/DELETE on
  the fixture journal (insert-once immutable, item A), and any direct DML without the GUC are rejected.
- Positive control: an authorized SD call sets the GUC and succeeds. Mutation tests: (a) direct `UPDATE`/`INSERT`/`DELETE`
  without the GUC → `Assert.Throws<PostgresException>` SQLSTATE `P0001`; (b) a spoofed GUC set outside a function
  (`set_config('tagekyc.raw_export_attempt_key_write_context','active',true)` in a raw session) still fails because
  `current_user` is not the deployer owner.

## M3. SQL / ACL manifest completeness
For every function in §4 and §5: full schema-qualified name; full ordered argument types; exact `RETURNS` shape;
`SECURITY DEFINER`; owner `tagekyc_raw_export_deployer`; `SET search_path = pg_catalog`; the exact role literal on every
`GRANT EXECUTE` (never an alias); `USAGE` on schema `tagekyc` limited to the three capability roles; ALL privileges revoked
from `PUBLIC` and from `tagekyc_runtime` on every function; no direct table privilege to any capability role (all access via
SD functions); no unexpected overload (one signature per name); default-ACL posture asserted via `ALTER DEFAULT PRIVILEGES`
revocation; Up creates, Down drops. A `pg_proc`/`pg_authid`/`information_schema.role_routine_grants` catalog test asserts the
manifest exactly.

## M4. Return-outcome vs exception (already applied in §4)
Every business condition → a typed RETURN value (columns above). Only structural/guard violations raise `P0001` with exact
`MessageText` (`RAW_EXPORT_KEY_WRITE_CONTEXT_INVALID` or `RAW_EXPORT_KEY_ARGUMENT_INVALID`). No value appears as both a
return and an unspecified failure. Tests assert the exact contract (return string equality, or SQLSTATE+MessageText).

## M5. Readiness manifest (see §6 for the full literal set + precedence)
Landed `PROD_RAW_EXPORT_ATTEMPT_KEY_PROFILE_MISSING`/`_PROFILE_INVALID`/`_FIXTURE_ACTIVE` are retained and coexist; the new
validators add the codes in §6. Every code: exact readiness type/file (`CustodyRoleReadinessValidator.cs`,
`DurableKeyProviderReadinessValidator.cs`, `KeyReservationReadinessValidator.cs`, landed `AttemptKeyReadinessValidator.cs`) +
one positive + one negative fixture. First-match precedence is per-validator; aggregate ordering is the validator sequence in
§6.

## M6. Test → mutation → assertion map
Project `TagEkyc.IntegrationTests.Tip88C1B2DurableKeyTests` (integration) / `TagEkyc.ArchTests.Tip88C1B2DurableKeyArchTests`
(architecture). Each row: exact C# FQN | exact scratch mutation | expected exception type + SQLSTATE + `MessageText`, or exact
return value | positive control | restoration proof (byte-identical revert of the scratch mutation, asserted in the same
test run). Grouped/vague names removed; each condition has its own mapping.

| # | FQN (method) | Scratch mutation | Expected RED signal | Positive control |
|---|---|---|---|---|
| 1 | `KeyReservation_ForeignKey_BindsExactLandedAttempt` | drop `fk_raw_export_attempt_key_resv_attempt_composite` | insert non-matching pair no longer raises `PostgresException` SQLSTATE `23503` | matching pair inserts |
| 2 | `LandedAttempt_AlternateKey_Exists` | (M5 ordered) drop the dependent FK `fk_raw_export_attempt_key_resv_attempt_composite` first, THEN drop `uq_raw_export_enc_attempt_attemptid_keyresvid`, apply the scratch defect, run the catalog assertion, then restore both byte-identically (DDL setup failure is not counted as RED) | `information_schema` query `Assert.Single` → empty | both objects present |
| 3a | `Prepare_DerivesContextFingerprint_MissingNfc` | render a scalar without NFC normalization | computed fingerprint ≠ golden `37108746b02b4a73070aafdb116ceeb15ba5b18399286b92fe38c40fc86c7793` (`Assert.Equal` fails) | NFC render matches golden |
| 3b | `Prepare_DerivesContextFingerprint_FromLockedLandedRow` | skip the `FOR UPDATE` lock of the landed row | a concurrent selector change is read, fingerprint diverges from golden (`Assert.Equal` fails) | locked read matches golden |
| 4 | `Prepare_RejectsExtraCallerSelector_NoSuchArgument` | add a caller-selector arg path | overload no longer single → catalog `Assert.Single` fails | 3-arg prepare succeeds |
| 5 | `Evidence_AttemptKeyContextFingerprint_GoldenVector` | flip one preimage byte / use binary UUID | `Assert.Equal("37108746b02b4a73070aafdb116ceeb15ba5b18399286b92fe38c40fc86c7793")` fails | fixture reproduces golden |
| 6 | `Evidence_WrappedDekMetadataDigest_GoldenVector` | reorder nonce/ct/tag | `Assert.Equal("607a19029096528f90db3fef24c5ba2229bf3cf86a5972932b00622503889d30")` fails | reproduces golden |
| 7 | `Evidence_ProviderResolutionEvidenceDigest_GoldenVector` | drop the optional tag scalar | `Assert.Equal("b30d9e7b6ce91af6f42bf826bd05daacc0c17bcb1409051b3517de5cc3f2c2dd")` (fence=1) fails | reproduces golden |
| 8 | `Evidence_ProviderCleanupEvidenceDigest_GoldenVector` | swap reference/receipt order | `Assert.Equal("d3f47850ae3cfa8cf233e85633b609201313bfa8d301b5689311f34e16dc54bb")` (fence=1) fails | reproduces golden |
| 8b | `Evidence_ProviderAbsenceEvidenceDigest_GoldenVector` | reuse the resolution domain for absence | `Assert.Equal("cab1a516ae8a2bd75fd260c529f2b35dd1a4d430a2d4f4ed9a733ea6bfd714d9")` fails | distinct-domain reproduces golden |
| 9 | `ProviderToken_ProviderScopedUnique_Bound` | drop `uq_raw_export_key_provider_op_provider_token` | duplicate `(KeyProviderId,token)` no longer raises `23505` | distinct tokens insert |
| 10 | `Recovery_Unknown_BlocksFreshDek_PermitsReResolveSameToken` | forbid re-resolve of the same token | re-resolve returns `IllegalResolution` instead of `ResolvedOutcomeUnknown` (`Assert.Equal` fails) | fresh DEK blocked |
| 11 | `Recovery_Unavailable_AppendsObservation_NeverNoResult` | map Unavailable→NoProviderResult | disposition becomes `ReadyForFreshPreparation` (`Assert.Equal("ProviderUnavailableObserved")` fails) | remains blocking |
| 12 | `Recovery_NoResult_OnlyFromExpired_AfterLease_ProviderProven` | accept NoResult from `PreparingLive` | returns `ResolvedNoResult` instead of `IllegalResolution` | expired+proven path succeeds |
| 13 | `Recovery_RetryTooSoon_BeforeBackoffWindow` | ignore `NextResolutionAttemptNotBeforeUtc` | early resolve mutates instead of returning `RetryTooSoon` | after window resolves |
| 14 | `Recovery_DeadlineExceeded_ToCorruptOperatorIntervention` | ignore `ResolutionDeadlineUtc` | stays blocking without `ProviderCorruptOrUnverifiable` (`Assert.Equal("DeadlineExceeded")` fails) | past deadline transitions |
| 15 | `Recovery_Corrupt_IsTerminal_NoSuccessor_NoRevoke` | allow any successor from Corrupt | prepare/activate/recovered-activate/revoke from Corrupt returns success (should be `IllegalResolution`/`NotActive`); readiness omits `PROD_RAW_EXPORT_KEY_PROVIDER_CORRUPT_UNRESOLVED` | every authorized op leaves Corrupt unchanged/blocked |
| 16 | `History_EventSequence_ProviderScopedUnique_StartsAtOne` | drop `uq_raw_export_attempt_key_event_sequence` | duplicate sequence no longer raises `23505` | monotonic 1..n |
| 17 | `History_SingletonKinds_PartialUniqueIndex` | drop `uq_raw_export_attempt_key_event_singleton` | second `DirectActivated` no longer raises `23505` | repeatable kinds still append |
| 18 | `History_AppendOnly_UpdateDeleteDenied` | remove append-only branch in guard | `UPDATE`/`DELETE` succeeds (should raise `P0001`) | INSERT via SD succeeds |
| 19 | `Activation_Direct_RelocksAttemptHeadLeaseTerminatedDeadlines` | drop the re-lock/lease/terminated/deadline check | activation on terminated/expired/non-current head returns `Activated` (should be `Terminated`/`LeaseExpired`/`HeadNotCurrent`) | valid head activates |
| 20a | `Activation_Recovered_ReconcilerOnly_AclDenied` | grant/allow encryptor to call recovered-activate | encryptor call no longer raises `PostgresException` SQLSTATE `42501` (permission denied) | reconciler holds EXECUTE |
| 20b | `Activation_Recovered_RecomputesWithPersistedSuite` | trust the caller digest instead of recomputing with the persisted suite | swapped receipt with stale digest returns `ActivatedRecovered` (should be `DigestMismatch`) | recompute path activates |
| 21 | `Evidence_RecoveredActivation_DigestRecomputed_NotCallerAuthoritative` | accept caller digest verbatim | mismatched persisted vs caller no longer returns `DigestMismatch` | recomputed match activates |
| 22 | `Restart_ResumesFromPersistedStateOnly` (**restart integration; fails-not-skips**) | resume from process memory | fresh-instance `read_current_attempt_key_recovery_context` returns empty (`Assert.Single` fails) | resumes from DB rows |
| 23 | `RecoveryContext_ReadSurface_ExactSignatureAndReconcilerOnlyAcl` | grant it to encryptor/PUBLIC | `role_routine_grants` `Assert.Equal(manifest)` fails; an encryptor call raises `PostgresException` SQLSTATE `42501` | reconciler-only holds |
| 24 | `Revoked_CannotEncrypt` | allow lease when Revoked | encrypt returns ciphertext (should raise revoked-state failure) | Active encrypts |
| 25 | `Revoked_CannotDecryptVerify_NoNewLease` | allow lease after revoke | verify returns plaintext (should raise revoked-state failure) | Active verifies |
| 26 | `Revoke_EvidenceImmutable` | update `RevokedAtUtc`/`RevocationReasonCode` | append-only/immutable guard no longer raises `P0001` | first revoke persists |
| 27 | `Lease_NoPublicMemberReturnsIAttemptDekLease` | make a public/interface member return `IAttemptDekLease` | ArchRule "no publicly-accessible member returns `IAttemptDekLease`" → `rule.Check()` `Assert.True(result.IsSuccessful)` fails | internal-only lease passes |
| 27b | `Contracts_NoDependencyOnInfrastructureLeaseTypes` | reference an Infrastructure lease type from `TagEkyc.Contracts` | ArchRule "`TagEkyc.Contracts` must not depend on `TagEkyc.Infrastructure.RawExport` lease types" → `IsSuccessful` false | no such dependency passes |
| 28 | `WriteGuard_DirectDmlDenied_PositiveControlAllowsAuthorized` | remove the GUC check | direct `UPDATE` succeeds (should raise `P0001`) | SD write succeeds |
| 29 | `WriteGuard_ContextRestored_OnExceptionAndSuccess` | skip restore in exception handler | leaked GUC (`Assert.Equal('', current_setting)` fails) | GUC empty after both paths |
| 30 | `WriteGuard_SpoofedGuc_StillFailsOnCurrentUser` | rely on GUC only | spoofed-GUC raw-session write succeeds (should raise `P0001` on `current_user`) | deployer-owner SD write succeeds |
| 31 | `Roles_CapabilityAttributes_Exact` | flip a `NOLOGIN`/`NOSUPERUSER` bit | `pg_roles` `Assert.Equal(false)` fails | attributes exact |
| 32 | `Roles_LoginAttributes_Exact` | flip a LOGIN attribute | `pg_roles` `Assert.Equal` fails | login attributes exact |
| 33 | `Roles_CrossMembership_FailsReadiness` | add cross-membership | `Assert.Contains("PROD_RAW_EXPORT_CUSTODY_ROLE_CROSS_MEMBERSHIP")` fails | single membership clean |
| 34 | `Roles_SetRoleEnabled_FailsReadiness` | set `SET=true` | `Assert.Contains("PROD_RAW_EXPORT_CUSTODY_ROLE_SET_ROLE_ENABLED")` fails | `SET=false` clean |
| 35 | `Roles_GrantToRuntimeOrPublic_Rejected` | grant a function to `tagekyc_runtime`/PUBLIC | `role_routine_grants` `Assert.Empty` fails | only capability roles hold EXECUTE |
| 36 | `HashHelper_NotRuntimeExecutable` | grant helper EXECUTE to runtime | `Assert.Contains("PROD_RAW_EXPORT_HASH_HELPER_ACL_INVALID")` fails | helper deployer-only |
| 37 | `Kek_DurableStoreWithFixtureKek_FailsReadiness` | mark fixture KEK qualified | `Assert.Contains("PROD_RAW_EXPORT_KEK_NOT_QUALIFIED")` fails | fixture flagged unqualified |
| 37b | `Topology_QualifiedDurableProvider_NotRejectedByFixtureCodes` (item D) | register the landed fixture `AttemptKeyReadinessValidator` under the `DurableKey` topology | a qualified durable production provider is rejected by `PROFILE_MISSING`/`PROFILE_INVALID`/`FIXTURE_ACTIVE` (`Assert.DoesNotContain` fails) | durable topology registers only durable validators; fixture validator NotApplicable |
| 38 | `Provider_TopologyInvalid_WhenAdapterMappingMissing` | drop the mapping table | `Assert.Contains("PROD_RAW_EXPORT_PROVIDER_TOPOLOGY_INVALID")` fails | mapping present |
| 39 | `Aead_EncryptAndVerifyTypesNonAssignable` | make one type implement both `IAttemptAeadEncryptionOperation` and `IAttemptAeadVerificationOperation` | ArchRule "no type implements both AEAD capability interfaces" → `IsSuccessful` false | distinct implementations pass |
| 40 | `Coexistence_DurableComposition_DoesNotResolveProcessLocalProvider` | register/resolve the landed `IAttemptKeyProvider` in the durable root | resolving it no longer throws `InvalidOperationException` (`Assert.Throws<InvalidOperationException>` fails) | durable provider resolves |
| 41 | `SparseState_Head_AllDispositionsExact` | violate an M1.1 shape | head CHECK `Assert.Throws<PostgresException>` `23514` fails | valid shapes insert |
| 42 | `Migration_DownReapply_UnderProductionLikeMemberships` | leave an orphan grant | `Assert.Empty(orphans)` fails | clean down+reapply |
| 43 | `Identifiers_AllUnder63Bytes_CatalogRoundTrip` | rename an object to >63 bytes | explicit intended-name array asserted for UTF-8 byte length (`Assert.True(≤63)`) + catalog `Assert.Equal` round-trip fails | all names ≤63 |
| 44 | *(REMOVED, v0.5 item 4)* — the old "delete persist-before-return → second DEK" mutation is vacuous under the durable `LookupByOperationToken` contract (the lost result is recoverable by lookup, so no second DEK is required). Test #57 is the canonical pre-persistence crash-window proof; this row is intentionally retired, not renumbered. |
| 45 | `Durable_MissingMappingRow_NotTreatedAsAbsence` | reinterpret an empty/missing mapping row as positive absence | `NoProviderResult` accepted without `AbsenceProven` (head reaches `ReadyForFreshPreparation`; should stay blocking) | only `AbsenceProven` + receipt permits it |
| 46 | `Absence_ProvenState_RequiredForNoProviderResult` | accept `NoProviderResult` while mapping = `Issued` | head transitions to `ReadyForFreshPreparation` (should return `IllegalResolution`) | `AbsenceProven` + non-empty receipt + recomputed absence digest transitions |
| 47 | `Mapping_CAS_RejectsStalePreparationFenceToken` | drop the identity/expected-state re-check in a §4a UPDATE | a stale-fence mapping UPDATE succeeds (should return `StaleOperation`) | current-generation UPDATE succeeds |
| 48 | `Mapping_ImmutableIdentityColumns` | UPDATE `KeyProviderId`/`ProviderOperationToken` on the mapping | guard no longer raises `P0001` | state-only UPDATE via §4a succeeds |
| 49 | `Cleanup_Unavailable_KeepsBlocking_AppendsObservation` | transition out of `ProviderCleanupRequired` on `CleanupUnavailable` | disposition leaves `ProviderCleanupRequired` (should stay + append `CleanupAttemptObserved`) | stays blocking, observation appended |
| 50 | `Cleanup_OutcomeUnknown_KeepsBlocking_NoFreshDek` | treat `CleanupOutcomeUnknown` as `Cleaned` | head reaches `ReadyForFreshPreparation` (should stay `ProviderCleanupRequired`) | stays blocking |
| 51 | `Cleanup_Failed_KeepsBlocking_IncrementsCounter` | skip `CleanupAttemptCount` increment on `CleanupFailed` | `CleanupAttemptCount` unchanged (`Assert.Equal(prev+1)` fails) | counter increments |
| 52 | `Cleanup_RetryTooSoon_BeforeCleanupWindow` | ignore `NextCleanupAttemptNotBeforeUtc` | early cleanup observation mutates instead of returning `RetryTooSoon` | after window observes |
| 53 | `Cleanup_DeadlineExceeded_SetsOperatorInterventionReadinessFails` | ignore `CleanupDeadlineUtc` | `CleanupOperatorInterventionRequired` stays false and readiness omits `PROD_RAW_EXPORT_KEY_PROVIDER_CLEANUP_OPERATOR_INTERVENTION` (`Assert.Contains` fails); state must NOT become Corrupt | past deadline sets intervention, stays blocking |
| 54 | `Cleanup_Restart_ReadbackLatestReferenceAndObservation` (**integration, fails-not-skips**) | resume from process memory | fresh-instance recovery projection returns null `latest_cleanup_required_reference`/`latest_cleanup_attempt_event_sequence` (`Assert.NotNull` fails) | resumes latest cleanup family from DB |
| 55 | `StatementTime_LeaseExpiry_UsesStatementTimestampNotTransaction` (**timing harness**) | replace `statement_timestamp()` with `transaction_timestamp()` in the lease/deadline checks | a transaction opened before expiry, then crossing expiry mid-transaction, still activates/resolves as if fresh (should return `LeaseExpired`/`DeadlineExceeded`) | statement-time rejects the stale lease |
| 56 | `RetryConfig_OutOfRange_FailsReadiness` | set a config value outside its §M1.5 range | readiness omits `PROD_RAW_EXPORT_KEY_RETRY_CONFIG_INVALID` (`Assert.Contains` fails) | in-range config passes |
| 56b | `TokenGeneration_CsprngBase64UrlNoPad_ReadinessWhenPgcryptoMissing` (item I) | drop the `pgcrypto` dependency / accept a caller-supplied token | prepare mints a non-256-bit or padded token, or a caller token is accepted; readiness omits `PROD_RAW_EXPORT_KEY_CSPRNG_UNAVAILABLE` when `gen_random_bytes` is absent (`Assert.Contains` fails) | 43-char base64url-no-pad 256-bit token generated server-side; no caller token |
| 57 | `ProviderCrashWindow_FreshProcess_RecoversViaRecordRecoveredResult` (**fresh-process integration; fails-not-skips**) | after provider `WrapDek` succeeds durably, crash BEFORE `record_key_provider_wrapped_result`; then bypass `LookupByOperationToken`+`record_recovered_key_provider_result` so the fresh process re-wraps | a second DEK is generated / wrapped bytes differ from the provider journal (`Assert.Equal(1, dekGenerationCount)` and byte-equality fail) | fresh process `LookupByOperationToken` → `Found` → `record_recovered_key_provider_result` records `ResultObserved` + recovered-activates the exact original bytes, one DEK |
| 57b | `RecordRecoveredResult_ReconcilerOnly_AclDenied` | grant/allow encryptor or lifecycle to call `raw_export_record_recovered_key_provider_result` | the non-reconciler call no longer raises `PostgresException` SQLSTATE `42501`; `role_routine_grants` `Assert.Equal(manifest)` fails | reconciler-only holds EXECUTE |
| 57c | `AdapterLayer_DivergentProviderResult_Detected` (item B — adapter, not SQL) | make the provider adapter return wrapped bytes/`ProviderResourceReference` diverging from the immutable fixture-journal row | the adapter's compare-to-journal no longer flags the divergence (`Assert.Throws`/typed mismatch fails) — SQL is not expected to detect it | adapter returns the immutable journal result |
| 57d | `FixtureJournal_InsertOnceImmutable_ContextMismatch` | attempt a direct UPDATE/DELETE on the journal, or a second `raw_export_fixture_kek_wrap` with a mismatched context | UPDATE/DELETE no longer raises `P0001`; the mismatched-context wrap no longer raises `RAW_EXPORT_FIXTURE_KEK_CONTEXT_MISMATCH` | insert-once holds; CreateOrGet returns the immutable original |
| 57e | `FixtureJournal_LookupMissingRow_ReturnsUnknownNotAbsence` | make `raw_export_fixture_kek_lookup` return absence for a missing row | a missing row yields `PositivelyAbsent`/`Found` instead of `Unknown` (`Assert.Equal("Unknown")` fails) | only an explicit `PositivelyAbsent` journal row proves absence |
| 57f | `FixtureJournal_Functions_RoleScopedAcl` | grant the journal wrap/lookup functions to PUBLIC/wrong role | `role_routine_grants` `Assert.Empty` fails; a wrong-role call no longer raises `42501` | wrap=encryptor, lookup=reconciler only |
| 58 | `Prepare_HeadEventIssued_Atomic_NoStrandedPreparation` | drop the provider-operation `Issued` INSERT (or the `Opened` event) from the `prepare` transaction | a `PreparingLive` head exists without its `Issued` mapping / `Opened` event (`Assert.Single` on each fails); the idempotent re-`prepare` backstop does not repair it | atomic prepare writes all three or none; re-prepare repairs a missing `Issued` for the same token |
| 58b | `Issue_NoEncryptorCallableSurface_OwnerOnlyHelper` | expose `raw_export_issue_key_provider_operation` (or the internal helper) with a non-owner EXECUTE grant | `role_routine_grants` shows an encryptor/reconciler/lifecycle/PUBLIC EXECUTE edge (`Assert.Empty` fails); a direct encryptor call no longer raises `42501` | no granted issue surface; issue happens only inside `prepare` |
| 59 | `Abandon_WritesImmutableAbandonEvents` | overload `RevocationReasonCode` for abandonment / make an abandon event mutable | `RevocationReasonCode` is non-NULL on `AbandonRequested`/`ProviderOperationAbandoned` (CHECK `Assert.Throws` fails) or an UPDATE/DELETE of either event no longer raises `P0001` | request/finalize append immutable events with token/reason/trusted-actor/`HeadRowRevision` |
| 59b | `Abandon_TwoPhase_FinalizeGatedOnCleanupOrAbsence` | allow finalize-abandon (`ReservationAbandoned`) while the mapping is still `Issued`/`ResultObserved`/`CleanupRequired` | finalize returns `ReservationAbandoned` instead of `CleanupNotComplete` (`Assert.Equal` fails); a head CHECK for both `AbandonRequested`/`ReservationAbandoned` sparse shapes no longer raises `23514` | finalize succeeds only from mapping `CleanedUp`/`AbsenceProven` |
| 59c | `Abandon_OperatorActor_TrustedNotCallerText_SpoofAndMissing` (item F) | pass a caller-authored actor string / clear the actor GUC | a caller string is persisted as `OperatorActorEvidence` (should be derived); or a missing actor context no longer raises `P0001 RAW_EXPORT_KEY_ACTOR_CONTEXT_MISSING` | actor derived from `raw_export_current_actor()`; missing context fails closed |
| 59d | `Abandon_Completed_DoesNotFailGlobalReadinessForever` (item E) | keep `PROD_RAW_EXPORT_KEY_ABANDON_PENDING` emitted after finalize | readiness still fails for a terminal `ReservationAbandoned` (`Assert.DoesNotContain` fails); only pending `AbandonRequested` should fail | completed abandonment is readiness-clean like `Revoked` |
| 60 | `CleanupObservation_MandatoryRecomputedDigest_RefThenKind_TaggedAbsentReceipt` | persist a `CleanupAttemptObserved` without the recomputed digest, or omit `ProviderCleanupReference` from the preimage | observation persists with NULL `CleanupObservationEvidenceDigest` (CHECK `Assert.Throws` fails); with-reference case ≠ golden `72c5086615ad0b49b5c8952dd6ea3d3dbfbe5c9a670aaf30f2550d07479969ed` | SQL-recomputed digest (ref before kind, absent receipt tagged `"0"`) reproduces golden |
| 61 | `Resolution_MaxAttemptCount_ExhaustionToCorrupt` | ignore `ResolutionMaxAttemptCount` when > 0 | at `ResolutionAttemptCount ≥ ResolutionMaxAttemptCount` the disposition stays blocking instead of `ProviderCorruptOrUnverifiable` (`Assert.Equal` fails) | exhaustion transitions to terminal operator-intervention |
| 62 | `Revoke_BindsReasonIntoImmutableEventAndDigest` | store the reason only as free head state / skip the recomputed digest | `Revoked` event lacks `RevocationReasonCode`/`RevocationEvidenceDigest` (CHECK `Assert.Throws` fails); digest ≠ golden `af78d56210a6e7ba6cea7b0c05df1d1544742ebad279e94162d03a47cc223204` | revoke binds reason + recomputed digest into the immutable event |
| 63 | `Resolution_MaxCountOne_AdmitsOneRealLookupAfterExpiry` | count `mark-expired` as a resolution attempt (increment `ResolutionAttemptCount` on expiry) | with `ResolutionMaxAttemptCount = 1`, expiry alone reaches the cap and the first real `resolve`/lookup is rejected as exhausted (`Assert.Equal("ResolvedOutcomeUnknown"/"RecoveredActivated")` fails) | expiry does not increment; at least one real provider lookup is admitted after expiry before exhaustion |

## M7. Text bounds + handle redaction
For every `KeyProviderId`, `KekId`, `KekFingerprint`, `WrappingSuiteId`, `ProviderOperationToken`, `ProviderOperationReceipt`,
`ProviderCleanupReference`, `ProviderCleanupReceipt`, `RevocationReasonCode`, and (item K) `ProviderResourceReference`,
`ProviderAbsenceProofReceipt`, `OperatorReasonCode`, `OperatorActorEvidence`, plus every fixture-journal handle/receipt
(`FixtureWrapId` handle, journal `ProviderResourceReference`/`ProviderOperationReceipt`/`ProviderAbsenceProofReceipt`):
NFC-normalized + trimmed; non-empty; ≤512 UTF-8
bytes (`ProviderOperationToken` additionally = 43-char base64url, 256-bit CSPRNG, generated inside the trusted boundary, no
padding, non-empty); control characters (`U+0000..U+001F`, `U+007F`) rejected by a DB CHECK (`value !~ '[\x00-\x1f\x7f]'`)
and a C# validator. `ProviderOperationToken` and provider handles are internal opaque types with a redacted `ToString()`
(prints a fixed tag, never the value); never plaintext-logged, never a metric label, never in a readiness payload, never in
a public API. Token lifetime = the current preparation generation; terminal cleanup marks the mapping `CleanedUp` or
`AbsenceProven` (there is no mapping `Abandoned` state — item E).

## M8. Exact buildable file allowlist (M8 — chosen architecture, full paths)
**Landed, adapted to `ProcessLocalFixture` + adapter/forbidden-edge:**
`src/TagEkyc.Contracts/RawExport/AttemptKeyContracts.cs`;
`src/TagEkyc.Infrastructure/RawExport/AttemptDekLease.cs`;
`src/TagEkyc.Infrastructure/RawExport/FixtureAttemptKeyProvider.cs`;
`src/TagEkyc.Infrastructure/RawExport/AttemptKeyServiceCollectionExtensions.cs`;
`src/TagEkyc.Infrastructure/RawExport/AttemptKeyReadinessValidator.cs`;
`tests/TagEkyc.ArchTests/Tip88C1B2AttemptKeyProviderTests.cs`.
**New — Contracts (application-facing, no lease/DEK):**
`src/TagEkyc.Contracts/RawExport/AttemptKeyReservationContracts.cs`;
`src/TagEkyc.Contracts/RawExport/AttemptAeadOperationContracts.cs`.
**New — Infrastructure (internal KEK ops, durable providers, adapter mapping, readiness, DI, entities):**
`src/TagEkyc.Infrastructure/RawExport/KekOperationContracts.cs`;
`src/TagEkyc.Infrastructure/RawExport/FixtureKekOperationProvider.cs`;
`src/TagEkyc.Infrastructure/RawExport/KekProvisioningRecoveryOperation.cs`;
`src/TagEkyc.Infrastructure/RawExport/PostgresAttemptKeyReservationProvider.cs`;
`src/TagEkyc.Infrastructure/RawExport/PostgresKeyProviderOperationMap.cs`;
`src/TagEkyc.Infrastructure/RawExport/AttemptKeyRecoveryContextReader.cs`;
`src/TagEkyc.Infrastructure/RawExport/AttemptAeadOperationService.cs`;
`src/TagEkyc.Infrastructure/RawExport/CustodyRoleReadinessValidator.cs`;
`src/TagEkyc.Infrastructure/RawExport/DurableKeyProviderReadinessValidator.cs`;
`src/TagEkyc.Infrastructure/RawExport/KeyReservationReadinessValidator.cs`;
`src/TagEkyc.Infrastructure/RawExport/DurableKeyCustodyServiceCollectionExtensions.cs`;
`src/TagEkyc.Infrastructure/RawExport/DurableKeyCustodyOptions.cs` (lease/retry config §M1.5 + range validation);
`src/TagEkyc.Infrastructure/Persistence/Entities/RawExportAttemptKeyReservationRow.cs`;
`src/TagEkyc.Infrastructure/Persistence/Entities/RawExportAttemptKeyPreparationEventRow.cs`;
`src/TagEkyc.Infrastructure/Persistence/Entities/RawExportKeyProviderOperationRow.cs`;
`src/TagEkyc.Infrastructure/Persistence/Entities/RawExportFixtureKekWrapJournalRow.cs` (test-only provider durability, fails prod KEK readiness);
`src/TagEkyc.Infrastructure/Persistence/Configurations/RawExportAttemptKeyReservationConfig.cs`;
`src/TagEkyc.Infrastructure/Persistence/Configurations/RawExportAttemptKeyPreparationEventConfig.cs`;
`src/TagEkyc.Infrastructure/Persistence/Configurations/RawExportKeyProviderOperationConfig.cs`;
`src/TagEkyc.Infrastructure/Persistence/Configurations/RawExportFixtureKekWrapJournalConfig.cs`;
`src/TagEkyc.Infrastructure/Persistence/Migrations/{UTCyyyyMMddHHmmss}_Tip88C1B2DurableKeyFoundation.cs` (+ paired
`.Designer.cs`; LOCKED timestamp rule — allocated at build, not guessed here);
`src/TagEkyc.Infrastructure/Persistence/Migrations/TagEkycDbContextModelSnapshot.cs`;
`src/TagEkyc.Infrastructure/Persistence/TagEkycDbContext.cs`;
`src/TagEkyc.Infrastructure/TagEkyc.Infrastructure.csproj`.
**New — API wiring + readiness surface:**
`src/TagEkyc.Api/Program.cs`; `src/TagEkyc.Api/ReadinessEndpoint.cs`; `src/TagEkyc.Api/appsettings.json`.
**New — tests + as-built:**
`tests/TagEkyc.IntegrationTests/Tip88C1B2DurableKeyTests.cs`;
`tests/TagEkyc.ArchTests/Tip88C1B2DurableKeyArchTests.cs`;
`docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_b2_durable_key_as_built.md`;
`docs/phase1_scope_and_debt_registry_v0_1.md`.
**Single landed E3 constant (one-constant-only, additive-snapshot-proven first):**
`tests/TagEkyc.IntegrationTests/Tip88B1E3ResolverReadBoundaryTests.cs`.
Any file outside this list is STOP/RRI. No directory wildcard and no deferred file remain.

## M9. Identifier / migration / Down proof
Every DURABLE-KEY identifier is pinned with its correct UTF-8 byte length (ASCII, 1 byte/char): tables
`raw_export_attempt_key_reservations` (35), `raw_export_attempt_key_preparation_events` (41),
`raw_export_key_provider_operations` (34), `raw_export_fixture_kek_wrap_journal` (35, test-only);
constraints/indexes `uq_raw_export_enc_attempt_attemptid_keyresvid` (45),
`fk_raw_export_attempt_key_resv_attempt_composite` (48), `uq_raw_export_attempt_key_event_sequence` (40),
`uq_raw_export_attempt_key_event_singleton` (41), `uq_raw_export_key_provider_op_provider_token` (44); trigger
`trg_raw_export_attempt_key_guard` (32); functions/roles as named in §4/§4a/§8 (each ≤63, e.g.
`raw_export_record_recovered_key_provider_result` = 47). There is NO granted `raw_export_issue_key_provider_operation`
function; if an internal owner-only issue helper is used it carries zero non-owner EXECUTE (census + negative-ACL test #58b). These hand-counts are NOT the gate:
test #43 constructs an explicit intended-name array, asserts each name's UTF-8 byte length ≤63, and asserts an exact catalog
round-trip. **Down (v0.4 additions):** drop the fixture journal table, the new history columns (`OperatorReasonCode`,
`OperatorActorEvidence`, `CleanupObservationEvidenceDigest`, `RevocationReasonCode`, `RevocationEvidenceDigest`,
`HeadRowRevision`), the new head cleanup-retry columns, and the added event-kind values with the same drop-then-reapply proof;
the seven evidence digest DOMAINS (context, wrapped-metadata, resolution, cleanup, absence, cleanup-observation, revocation)
are string constants, not schema identifiers. **Down (v0.5 additions):** drop
`raw_export_record_recovered_key_provider_result` and any owner-only issue helper (no orphan overload/ACL); the
`ReservationAbandoned` disposition and `ProviderOperationAbandoned` event kind are `text` values removed with the same
drop-then-reapply proof; no new table is added in v0.5. **Down:** drop the FK then the additive alternate key on the landed
attempt table (restoring its landed key set exactly); drop DURABLE-KEY functions/triggers/indexes/tables; revoke exact
grants + default privileges; DROP only the three migration-owned capability roles; preserve deployment-owned LOGIN roles;
leave no orphan overload/ACL/default-ACL/membership/trigger/index; reapply cleanly (tests #42, #2).

## Closure matrix (H1–H11, M1–M9)
| Row | Section(s) | Authoritative decision | Artifact affected | Verification | Status |
|---|---|---|---|---|---|
| H1 | §2a | Additive `uq_raw_export_enc_attempt_attemptid_keyresvid` + composite FK `fk_raw_export_attempt_key_resv_attempt_composite` ON DELETE RESTRICT | landed attempt table + head, EF mapping | tests #1,#2; catalog round-trip | CLOSED |
| H2 | §4 prepare | prepare locks landed row, derives fingerprint via the landed `raw_export_c1_hash_canonical`, no caller selector | prepare SD function | tests #3,#4,#5,#36 | CLOSED |
| H3 | §7 | Profile AES-256-GCM/1; seven evidence constructions; seven real golden vectors + independent recompute command | evidence functions, tests | tests #5–#8, #8b, #60, #62 (byte-exact) | CLOSED |
| H4 | §3 | Raw DEK/KEK ops Infrastructure-internal; Contracts expose only operation-scoped capabilities; no lease crosses | interfaces, allowlist | tests #27,#39; ArchTests | CLOSED |
| H5 | §2d,§3,§4 | ONE topology: durable adapter mapping `raw_export_key_provider_operations`; token 256-bit base64url provider-unique | mapping table + SD ops | tests #9,#38; §M7 | CLOSED |
| H6 | §1 recovery | Unknown/Unavailable/Corrupt/NoResult edges pinned; bounded retry fields + deadline→Corrupt transition | state table, head retry cols | tests #10–#15 | CLOSED |
| H7 | §2c,§M1.3 | Full event history columns; `uq_raw_export_attempt_key_event_sequence` from 1; singleton partial index; per-kind CHECK; append-only | history table | tests #16,#17,#18 | CLOSED |
| H8 | §5 | `read_current_attempt_key_recovery_context` reconciler-only, current-generation, deterministic latest-per-event-family + mapping state | recovery read function | tests #22,#23,#54 | CLOSED |
| H9 | §1,§4,§M6 | Direct vs recovered activation distinct barriers; recovered recomputes with persisted suite, reconciler-only | activate + recovered-activate functions | tests #19,#20,#21 | CLOSED |
| H10 | §3,§M1.2 | Active-only encrypt+verify; no new lease after Revoke; wrapped state immutable; revoke evidence immutable; bounded lease | AEAD services, head | tests #24,#25,#26,#27 | CLOSED |
| H11 | §2b,§M1.2 | `CurrentPreparationFence` (generation) + `RowRevision` (head write) separate; every head change one guarded CAS | head + functions | tests #19,#28; §M2 | CLOSED |
| M1 | §M1 | Full head/history/mapping nullability + immutability matrices, DB-enforced | CHECK/trigger | tests #41; #26 | CLOSED |
| M2 | §M2 | Exact guard trigger/function, GUC, current_user, restore, spoof-proof | guard trigger | tests #28,#29,#30 | CLOSED |
| M3 | §4,§M3 | Full SQL/ACL manifest, role literals, no direct table grant, one overload | all functions | tests #23,#35; catalog | CLOSED |
| M4 | §4,§M4 | Every business condition returns a typed value; only guard/argument raises P0001 | all functions | every §M6 return-value assert | CLOSED |
| M5 | §6,§M5 | Landed DEKKEK codes retained; new codes fully spelled; per-validator first-match + aggregate order | readiness validators | tests #33,#34,#36,#37,#38 | CLOSED |
| M6 | §M6 | 77 active named tests (through #63; #44 retired; incl. splits 3a/3b, 8b, 20a/20b, 27/27b, 37b, 56b, 57b–57f, 58b, 59b–59d), each FQN\|mutation\|expected\|positive\|restore | test projects | the table itself | CLOSED |
| M7 | §M7 | Text bounds + control-char CHECK + token spec + redacted handles | validators, CHECK | tests #9; ArchTests | CLOSED |
| M8 | §M8 | Full explicit allowlist for the chosen adapter architecture; one E3 constant | allowlist | STOP/RRI on any extra file | CLOSED |
| M9 | §M9 | Every identifier pinned with correct byte length; Down restores landed keys + reapplies clean | migration Down | tests #2,#42,#43 | CLOSED |

## v0.3 correction closure (H1–H7 / M1–M9)
Marked CLOSED only where an executable contract now exists (named function/constraint/test); otherwise STOP/RRI.
| v0.3 item | Section(s) | Authoritative decision | Verification | Status |
|---|---|---|---|---|
| H1 durable adapter | §2d, §4a | Mapping durably stores the opaque wrapped result (ct/nonce/tag/metadigest/suite/receipt) + absence fields; recover after response loss via provider lookup (§2e); never a second DEK; never raw DEK | tests #45, #57 | CLOSED |
| H2 satisfiable mapping | §2d, §4a, §M2 | Mutable mapping head, guarded CAS UPDATE via §4a functions; exact state graph incl. `AbsenceProven`; identity immutable; §M2 no longer says INSERT-only | tests #47,#48 | CLOSED |
| H3 positive absence | §1, §3, §4a, §7 | `AbsenceProven` state + `ProviderAbsenceProofReceipt` + distinct `ProviderAbsenceEvidenceDigest` domain; timeout/miss/NotFound never prove absence | tests #46,#8b | CLOSED |
| H4 corrupt terminal | §1, §M1.1, §M6 | `ProviderCorruptOrUnverifiable` terminal in v0.3: no successor, no activation, no fresh prep, NO revoke; readiness stays failed | test #15 | CLOSED |
| H5 cleanup observations | §1, §2b, §2c, §4a, §6 | `CleanupAttemptObserved` repeatable event; independent cleanup counter/clock/deadline; deadline → operator-intervention (not Corrupt) | tests #49–#54 | CLOSED |
| H6 provider-op SQL/CAS | §4a | Named-parameter SD functions with predecessor/successor/delta/returns/grantee; no type-only ambiguous signature | tests #45–#52; catalog | CLOSED |
| H7 statement-time | §1, §4, §4a, §M1.5 | `pg_catalog.statement_timestamp()` for every lease/retry/deadline/observation comparison | test #55 | CLOSED |
| M1 concrete values | §1, §M1.5 | Config keys/defaults/ranges + exact backoff formula; out-of-range fails readiness | test #56 | CLOSED |
| M2 fence/revision | §2b | First fence = 1; +1 per fresh prep; RowRevision = 1 at insert, +1 per non-idempotent update; overflow fails closed; golden vectors recomputed | tests #7,#8,#8b (fence=1) | CLOSED |
| M3 event-family readback | §5 | Latest-per-family projection + mapping state; never one-latest-row | tests #22,#54 | CLOSED |
| M4 matrices expanded | §M1.1, §M1.3, §M1.4, §M1.5 | Cleanup fields, `AbsenceProven`, `CleanupAttemptObserved`, mapping CAS states, fence=1 all in matrices | tests #41,#47,#48 | CLOSED |
| M5 mutation proofs | §M6 | FK-before-alt-key ordering (#2); split 3a/3b, 20a/20b; per-cleanup-kind cases | tests #2,#3a,#3b,#20a,#20b,#49–#51 | CLOSED |
| M6 table shape | §4 | Inspect row reduced to 4 cells; read-only note moved to prose | rendered table | CLOSED |
| M7 exact RED | §M6 | `42501` for missing EXECUTE (#20a,#23); arch rules pin exact forbidden symbols (#27,#27b,#39,#40) | those tests | CLOSED |
| M8 identifier evidence | §M9 | Byte lengths corrected (35/41/34/45/48/40/41/44/32); array-based length + catalog test | test #43 | CLOSED |
| M9 allowlist/closure honesty | §M8, this table | Added `DurableKeyCustodyOptions.cs`; mapping/absence within listed entities; E3 one-constant unchanged; rows CLOSED only with executable contract | STOP/RRI on extra file | CLOSED |

## v0.4 gap closure (1–6)
| Gap | Section(s) | Authoritative decision | Verification | Status |
|---|---|---|---|---|
| 1 provider crash window | §1, §3, §6 | Provider durable + idempotent by token (`WrapDek` CreateOrGet + `LookupByOperationToken`); fixture journal `raw_export_fixture_kek_wrap_journal`; readiness `PROD_RAW_EXPORT_KEY_PROVIDER_DURABILITY_UNSUPPORTED`; fresh-process recovery `Found→ResultObserved→recovered-activate` | test #57 (fresh-process, fails-not-skips) | CLOSED — crash window demonstrably satisfiable |
| 2 atomic prepare | §4 prepare | head + `Opened` event + `Issued` mapping in ONE SD transaction; idempotent CAS re-prepare backstop for a `PreparingLive` head missing its mapping | test #58 | CLOSED |
| 3 abandon evidence | §2c, §4a, §M1.3 | Immutable `ProviderOperationAbandoned` event (token/reason/actor/fence/`HeadRowRevision`/time); `RevocationReasonCode` MUST be NULL there — never overloaded | test #59 | CLOSED |
| 4 cleanup-obs evidence | §4a, §7, §M1.3 | SQL-recomputed mandatory `CleanupObservationEvidenceDigest` from frozen identity + tagged-optional receipt (absent → `"0"`); no observation persists without it; golden vector | test #60 | CLOSED |
| 5 max-attempt/deadline init | §1, §M1.5 | `ResolutionMaxAttemptCount`=0 → deadline governs; >0 → exhaustion → `ProviderCorruptOrUnverifiable`; `ResolutionDeadlineUtc`/`CleanupDeadlineUtc` set once at first entry, immutable per generation | test #61 | CLOSED |
| 6 revoke evidence | §4a note, §7, §M1.3 | `RevocationReasonCode` bound into the immutable `Revoked` event + SQL-recomputed `RevocationEvidenceDigest`; golden vector | test #62 | CLOSED |

## v0.5 executability closure (1–7)
| Item | Section(s) | Authoritative decision | Verification | Status |
|---|---|---|---|---|
| 1 no issue surface | §2d, §4, §4a, §M9 | `Issued` created only inside `prepare` from internally-derived id/token/fingerprint; no granted issue function; any internal helper is owner-only, zero non-owner EXECUTE | tests #58, #58b | CLOSED |
| 2 executable Found recovery | §1, §4a, §5, §6 | Reconciler-only `raw_export_record_recovered_key_provider_result` records `ResultObserved` + recovered activation atomically; ACL/readiness/Down/no-orphan synchronized | tests #57, #57b | CLOSED |
| 3 abandonment | §1, §2d, §4a, §5, §6, §M1.1 | **Superseded by v0.6 item E** (two-phase): request→`AbandonRequested`; finalize→`ReservationAbandoned` only from mapping `CleanedUp`/`AbsenceProven`; no mapping `Abandoned` state; pending fails readiness, completed does not | tests #59, #59b, #59c, #59d | CLOSED |
| 4 remove vacuous #44 | §M6 | #44 retired (recoverable by lookup under the durable contract, not RED); #57 is canonical | §M6 row 44 | CLOSED |
| 5 rewrite §2d durability | §2d | Two independent layers: provider CreateOrGet/Lookup closes provider-success-before-DB-persistence; DB mapping closes restart/readback after `ResultObserved`; neither substitutes | §2d prose | CLOSED |
| 6 exact-value equality | §1, §3, §4a | **Superseded by v0.6 item B:** `LookupByOperationToken.Found` includes `ProviderResourceReference`; immutability is guaranteed by the trusted adapter/journal (CreateOrGet), divergence caught at the adapter layer (#57c), not by an impossible SQL byte-compare; recorder validates shape/suite + recomputes digests | test #57c | CLOSED |
| 7 resolution-count semantics | §1, §M1.5 | `ResolutionAttemptCount` increments only on real provider observations; `mark-expired` does not; max-count=1 admits ≥1 lookup after expiry | tests #61, #63 | CLOSED |

## v0.6 consolidated closure (A–L)
| Item | Section(s) | Authoritative decision | Verification | Status |
|---|---|---|---|---|
| A fixture journal | §2e, §M2, §M8 | Exact `raw_export_fixture_kek_wrap_journal` schema (PK/unique, immutable context, wrapped/absence families, sparse CHECK) + `raw_export_fixture_kek_wrap` (CreateOrGet CAS) / `raw_export_fixture_kek_lookup`; owner/ACL/guard/readiness/Up/Down; positive-absence journal row | tests #57, #57d, #57e, #57f | CLOSED |
| B no impossible SQL compare | §1, §3, §4a | `ExactValueMismatch` removed from SQL; adapter/journal guarantees immutability; SQL validates shape/suite + recomputes digests + CAS | test #57c (adapter layer) | CLOSED |
| C no caller digests | §4a, §M6 | `p_provider_resolution_evidence_digest` removed from recorder; `p_wrapped_dek_metadata_digest` removed from wrapped-result recorder; SQL recomputes | tests #21, #60 | CLOSED |
| D readiness topology | §6, §M5 | Per-topology registration; qualified durable production provider never hit by fixture codes | test #37b | CLOSED |
| E two-phase abandon | §1, §2d, §4a, §6, §M1.1, §M1.4 | `AbandonRequested → (cleanup/absence) → ReservationAbandoned`; no mapping `Abandoned`; pending fails readiness, completed does not | tests #59b, #59d | CLOSED |
| F trusted operator identity | §4a, §M1.3 | `OperatorActorEvidence` derived from `raw_export_current_actor()` (fail-closed); only `OperatorReasonCode` is caller input | test #59c | CLOSED |
| G recovery projection | §5 | Independent latest-abandonment family (sequence/token/reason/trusted-actor/`HeadRowRevision`/time) | tests #22, #54 | CLOSED |
| H sparse matrices | §M1.1, §M1.4 | Explicit `AbandonRequested`/`ReservationAbandoned` columns; contradictory prose removed; evidence event-only | tests #41, #59b | CLOSED |
| I token generation | §4, §6 | `gen_random_bytes(32)` (pgcrypto) → base64url-no-pad; server-side only; readiness `PROD_RAW_EXPORT_KEY_CSPRNG_UNAVAILABLE` | test #56b | CLOSED |
| J evidence contracts | §3, §7 | `ProviderCleanupReference` added to cleanup-observation digest (recomputed golden vector in §7); `ResolvedNoResult` digest = absence digest; `WrappedResultRecovered` carries `ProviderResourceReference` | tests #60, #8b | CLOSED |
| K text/handle rules | §M7 | Bounds/NFC/trim/control-char/redaction extended to resource-ref, absence receipt, operator reason/actor, all fixture-journal handles | tests #9; ArchTests | CLOSED |
| L mechanical sweep | this table + closure | Seven vectors (not four); #44 removed from ranges; seven domains; census recomputed to 77; graph/SQL/ACL/readiness/projection/matrices/tests/allowlist/ids/Down synchronized | full-document grep below | CLOSED |

## Sweep (v0.6)
Full-document grep for every v0.2–v0.6 banned token. v0.6-specific checks: fixture journal schema/functions/ACL fully pinned
(§2e); no `ExactValueMismatch` SQL outcome (adapter-layer instead); no caller-authoritative digest in any recorder; readiness
validators registered per topology (no cross-fire); abandonment is two-phase with no mapping `Abandoned` state; `OperatorActorEvidence`
is trusted-derived, never caller text; recovery projection carries the abandonment family; sparse matrix has explicit
`AbandonRequested`/`ReservationAbandoned` columns and no contradictory prose; token via `gen_random_bytes` base64url-no-pad;
cleanup-observation digest includes `ProviderCleanupReference`; closure claims corrected (seven vectors/domains, #44 removed,
census 77). v0.5-specific checks: no encryptor-callable issue surface (issue is
inline in `prepare`; any helper is owner-only); fresh-process `Found` recovery is executable via a reconciler-only recorder
(not prose only); `Abandoned` is an atomic coupled head+mapping transition (never mapping-only); the vacuous #44 mutation is
retired (not presented as RED); §2d states two non-substituting durability layers; `LookupByOperationToken.Found` carries
`ProviderResourceReference` with byte/exact-value equality; `ResolutionAttemptCount` is not incremented by `mark-expired`.
v0.4-specific checks: the provider crash-window is closed by a
durable idempotent provider contract (`WrapDek` CreateOrGet + `LookupByOperationToken`), not a DB token alone; prepare is
atomic across head/event/`Issued`; abandonment has its own immutable event and never overloads `RevocationReasonCode`; every
cleanup observation carries an SQL-recomputed mandatory digest; `ResolutionMaxAttemptCount` and both deadline initializations
are pinned; revocation reason is bound into the immutable event + recomputed digest. Outcome, all absent outside the report
enumeration: no `TBD`, no
`OPEN QUESTION`, no `verify at proof-build`, no standalone `...`/`…`, no `wrapped…`, no `same args`, no `<literal>`, no
`<exact>`, no `published in as-built`, no bare/generic `Assert.Throws` (every assertion pairs a SQLSTATE or exact return
value), no grouped test name without an independent mapping, no role alias in an ACL row, no abbreviated readiness literal,
no architecture joined by `OR`. v0.3-specific checks: no contradictory INSERT-only mapping language (§2d/§M2 now permit
guarded CAS UPDATE); no "empty ResultObserved" absence proof (replaced by explicit `AbsenceProven` + receipt, §H3); no
`Corrupt → revoke` (H4 makes Corrupt terminal with no revoke); every cleanup outcome has a durable `CleanupAttemptObserved`
event + retry fields; no security-sensitive `now()`/`transaction_timestamp()` (all lease/deadline checks use
`statement_timestamp()`); no `backoff(...)` without an exact formula (§1 + §M1.5); no single-latest-event recovery claim
(§5 is latest-per-family); no ambiguous type-only provider-op SQL signature (§4a full named parameters); no invalid mutation
setup (test #2 drops the FK before the alternate key); identifier byte lengths corrected (§M9); no row marked CLOSED without
an executable contract. `Q1` is removed. The two remaining, deliberately-retained hits, both justified:
- `{UTCyyyyMMddHHmmss}` (§M8) — the EF migration timestamp is a LOCKED build-time allocation (stamped when the migration is
  scaffolded, never guessed in a doc), not a deferred design decision. Every other identifier is concrete.
- The words listed inside this Sweep paragraph and in the two banned-token report lines are the report enumerating the
  banned strings themselves; they are not usages elsewhere in the contract.

## Open questions / STOP-RRI
None. H1 removed the only open question (Q1); the landed attempt key set is known and the additive constraint + composite FK
are ratified. Any file outside §M8 encountered at proof-build, or any landed-schema surprise contradicting §2a, is STOP/RRI.

## Proof-build boundary (NOT authorized here)
A separately-authorized proof build would implement ONLY: reservation head; preparation history; provider-operation adapter
mapping; prepare/expire/resolve/re-resolve/cleanup; direct/recovered activation; restart readback; key roles/ACL/write
guards; readiness validators; Up/Down/reapply; the fixture KEK provider; the targeted integration + architecture tests
above. No S3/MinIO, no object lifecycle, no R2 codec, no real Raw BIO, no production activation. Proof-build authorization is
not granted in this turn, and this document is not marked READY FOR BUILD.
