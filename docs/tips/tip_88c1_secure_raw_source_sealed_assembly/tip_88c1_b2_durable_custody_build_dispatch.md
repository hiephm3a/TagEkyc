# TIP-88C1-B2-DURABLE-CUSTODY — Durable Key/Object Custody Foundation — BUILD DISPATCH (v7)

**Status: SUPERSEDED — decomposed (PI-TAG-001) into two independently-reviewable build dispatches:**
- `tip_88c1_b2_durable_key_custody_build_dispatch.md` (DURABLE-KEY — leads; narrow proof build after its own review)
- `tip_88c1_b2_durable_object_custody_build_dispatch.md` (DURABLE-OBJECT — blocked on the DURABLE-KEY contract)

**NOT BUILD AUTHORITY. DO NOT IMPLEMENT FROM THIS DOCUMENT.** Retained for history only (the two documents above carry the
authoritative, corrected scope). This combined draft exceeded single-review convergence (two independent durable recovery
state machines — key + object — in one document, still surfacing cross-aggregate findings after 5+ rounds), which is the
PI-TAG-001 trigger to decompose before implementation. R2 remains SUSPENDED.
**Repo:** `D:\Task\Remote Signing\TagEkyc` · **Baseline:** `23e0154805e468919e513bf04b0a24d374d1ef13` · **Tier-0.**

---
*Historical content below is preserved verbatim as of v7 and is NOT the build contract. Use the two decomposed documents.*

Preserved-closed: WrappingSuiteId/Version not in R1; internal profile `AES-256-GCM`/`1`; prepare takes no
caller-authoritative selector/fingerprint/suite/token; provider recovery + cleanup are reconciler-only;
ProviderOutcomeUnknown blocks every fresh DEK; recovered results use the reconciler-only activation path; MinIO
durability via a named persistent Docker volume across container replacement; AWSSDK.S3 as an S3-compatible client
against an explicit MinIO ServiceURL (no AWS infra); capability/LOGIN roles separated under E3; no real Raw BIO/plaintext.

## 1. Two-layer key state (HIGH-1) — current head + append-only preparation history

**A. Current reservation head `tagekyc.raw_export_attempt_key_reservations`** (owner `tagekyc_raw_export_deployer`;
direct DML denied; PK `AttemptKeyReservationId uuid`): `AttemptId uuid` · `AttemptKeyContextFingerprint bytea CHECK
octet_length=32` · `EncryptionAttemptFingerprint bytea CHECK octet_length=32` · `KeyProviderId text` · `KekId text` ·
`KekVersion integer` · `KekFingerprint text` · `WrappingSuiteId text` · `WrappingSuiteVersion integer` ·
`PreparationDisposition text` · `CurrentPreparationId uuid NULL` · `CurrentPreparationFence bigint NOT NULL DEFAULT 0` ·
`CurrentPreparationLeaseExpiresAtUtc timestamptz NULL` · `CurrentProviderOperationToken text NULL` ·
`WrappedDekCiphertext bytea NULL` · `WrappedDekNonce bytea NULL CHECK octet_length=12` · `WrappedDekTag bytea NULL CHECK
octet_length=16` · `WrappedDekMetadataDigest bytea NULL CHECK octet_length=32` · `PreparedAtUtc timestamptz NULL` ·
`RevokedAtUtc timestamptz NULL` · `RowRevision bigint NOT NULL` · `RowFence bigint NOT NULL` · `CreatedAtUtc timestamptz`
· `UpdatedAtUtc timestamptz`. `PreparationDisposition ∈ {PreparingLive, PreparingExpiredAwaitingResolution,
ProviderOutcomeUnknown, ProviderCorruptOrUnverifiable, ProviderCleanupRequired, ReadyForFreshPreparation, Active, Revoked}`.
The head holds ONLY current authoritative state; NO prior-generation evidence lives here (it moves to history).

**B. Append-only preparation history `tagekyc.raw_export_attempt_key_preparation_events`** (owner deployer; UPDATE/DELETE
denied by trigger; INSERT only via the SD functions). Design chosen: **separate append-only event rows** (never
enriched in place, so no prior evidence is overwritten). PK `PreparationEventId uuid`; FK `AttemptKeyReservationId uuid →
raw_export_attempt_key_reservations` (RESTRICT); UNIQUE `(AttemptKeyReservationId, PreparationId, PreparationFence,
EventKind)`; index `(AttemptKeyReservationId, PreparationId, PreparationFence)`. Columns: `PreparationId uuid` ·
`PreparationFence bigint` · `EventKind text` ∈ `{Opened, Expired, ResolvedNoResult, ResolvedOutcomeUnknown,
ResolvedCorrupt, ResolvedCleanupRequired, CleanupAcknowledged, RecoveredActivated}` · `PreparationLeaseExpiresAtUtc
timestamptz NULL` · `ProviderOperationToken text NULL` · `ProviderOperationReceipt text NULL` · `ProviderCleanupReference
text NULL` · `ProviderCleanupReceipt text NULL` · `ProviderResolutionEvidenceDigest bytea NULL CHECK octet_length=32` ·
`ProviderCleanupEvidenceDigest bytea NULL CHECK octet_length=32` · `WrappingSuiteId text NULL` · `WrappingSuiteVersion
integer NULL` · `RecoveredWrappedMetadataDigest bytea NULL CHECK octet_length=32` · `EventAtUtc timestamptz NOT NULL`.
All identifiers ≤ 63 bytes (proof: longest = `raw_export_attempt_key_preparation_events` = 41, longest constraint
`uq_raw_export_attempt_key_preparation_events_gen` = 49). Enforcement: the head + history writes go through the SD
functions under the deployer owner + the custody write-context GUC; a `deny_append_only_mutation`-style trigger blocks
UPDATE/DELETE on the history table. Down drops both tables + trigger + functions; reapply idempotent; mutation proof per
§14. **Satisfiability:** ReadyForFreshPreparation on the head clears only the head's current-preparation pointer/lease/
token and prior wrapped state; every prior generation's evidence remains in immutable history rows, so multiple fresh
preparations never erase history and the head always points to exactly one current preparation.

## 2. Provider recovery + cleanup (reconciler-only) — unchanged from v6 semantics
`IKekOperationProvider`=`WrapDek`/`UnwrapDek`. `IKekProvisioningRecoveryOperation` (reconciler composition root only):
`ResolveProvisioningOperationAsync(ProviderOperationToken, AttemptKeyContextFingerprint, CancellationToken) →
KekProvisioningResolution`; `CleanupProvisioningOperationAsync(ProviderCleanupReference, AttemptKeyContextFingerprint,
CancellationToken) → KekProvisioningCleanupResult`. Resolution ∈ {`WrappedResultRecovered`{WrappedDekCiphertext,
WrappedDekNonce, WrappedDekTag, ProviderOperationReceipt, WrappingSuiteId, WrappingSuiteVersion}, `NoProviderResult`
(provider-proven; absence/timeout/transport/missing MUST NOT map here), `ProviderOutcomeUnknown`,
`ProviderResourceCleanupRequired`{ProviderCleanupReference}, `ProviderUnavailable`, `CorruptOrUnverifiable`}. Cleanup ∈
{`Cleaned`{ProviderCleanupReceipt}, `AlreadyAbsent`{ProviderCleanupReceipt}, `CleanupUnavailable`, `CleanupOutcomeUnknown`,
`CleanupFailed`}. Only `Cleaned`/proven `AlreadyAbsent` may reach `ReadyForFreshPreparation`; `CleanupOutcomeUnknown`
keeps blocking. Readiness `PROD_RAW_EXPORT_KEY_OPERATION_RECOVERY_UNSUPPORTED`.

## 3. Suite-verification authority split (HIGH-3)
C# reconciler receives the typed provider result INCLUDING `WrappingSuiteId/Version`; **C# rejects any suite ≠ the
internally persisted profile (`AES-256-GCM`/`1`)** before calling SQL. The SQL recovered-activation function does NOT
receive or inspect a provider suite field; it **independently recomputes** `AttemptKeyContextFingerprint` and
`WrappedDekMetadataDigest` using the head's **persisted** `WrappingSuiteId/Version` + the supplied wrapped bytes, and
accepts only a matching digest. Provider result, C# suite check, and SQL activation are bound through
`ProviderResolutionEvidenceDigest` (§13). A provider-reported suite mismatch cannot activate (§14 test).

## 4. Object write-session graph — no-upload vs provider-multipart split (HIGH-2)
States: `Initiated → Writing → Finalizing`; **no-provider-upload path** `Initiated (UploadId NULL) →
NoProviderUploadEstablished` (terminal; no provider abort call; UploadId stays NULL; local termination evidence +
timestamp; retry idempotent; no return to Writing/Finalizing); **provider-multipart path** `Writing|Finalizing (UploadId
NOT NULL) → MultipartAbortPending → MultipartAborted` (requires exact provider UploadId + abort request/ack evidence;
NO `MultipartAborted` without that evidence; an unknown abort outcome remains `MultipartAbortPending`, never treated as
aborted); **commit path** `Finalizing → ObjectCommittedPendingVerification → VerifiedCompleted`; and
`ObjectCommittedPendingVerification → CleanupPending → Deleted | Quarantined`. Terminal: `NoProviderUploadEstablished`,
`MultipartAborted`, `VerifiedCompleted`, `Deleted`, `Quarantined`. Every transition = guarded SD-CAS on `(WriteSessionId,
exact source state, expected SessionFence)`; SessionFence increments per transition.

## 5. Write-session registry + parts
`tagekyc.raw_export_provisional_write_sessions` (owner deployer; DML denied; PK `WriteSessionId uuid`; UNIQUE
`(AttemptId, ProvisionalObjectIdentity)`): `AttemptId uuid` · `ProvisionalObjectIdentity uuid` · `ObjectKey text` ·
`UploadId text NULL` · `SessionState text` · `SessionFence bigint NOT NULL` · `FinalizationNonce bytea NULL` ·
`ObjectKeyCommitted text NULL` · `VersionId text NULL` · `ProviderCommitReceipt text NULL` · `AbortAcknowledgementReceipt
text NULL` · `LocalTerminationEvidence text NULL` · `CreatedAtUtc timestamptz` · `UpdatedAtUtc timestamptz`. Child
`tagekyc.raw_export_provisional_write_session_parts` (owner deployer; append-only; PK `(WriteSessionId, PartOrdinal)`;
FK `WriteSessionId`): `PartOrdinal integer CHECK (PartOrdinal >= 0 AND PartOrdinal < 10000)` · `ProviderPartReceipt text`
· `RecordedAtUtc timestamptz`.

## 6. KEK seam + frame-agnostic AEAD (two non-assignable contracts)
`IKekOperationProvider` `WrapDek`/`UnwrapDek`; production-qualified ⇒ KEK never in process; fixture KEK =
`ProcessLocalFixture` (fails `PROD_RAW_EXPORT_KEK_NOT_QUALIFIED`). `IAttemptAeadEncryptionOperation`
(`EncryptBoundedChunk(keyContext, nonce, aad, plaintext) → (ciphertext, tag)`) and `IAttemptAeadVerificationOperation`
(`DecryptAndVerifyBoundedChunk(keyContext, nonce, aad, ciphertext, tag) → plaintext | verifyFailure`) — frame-agnostic;
DEK confined in an internal non-assignable lease, zeroized per op; encryptor resolves encryption only, reconciler
verification only, runtime neither (ArchTest).

## 7. Lost-response / orphan recovery — disclosed privilege
The reconciler/lifecycle S3 adapter holds a bucket-level `ListMultipartUploads`, used only with an exact-key prefix +
exact-equality filtering + attempt/session validation + strict pagination/result bounds + audit; non-exact results
rejected; never exposed to R2/domain/reconciler-contract. Bucket lifecycle = delayed backstop (readiness verifies the
abort-incomplete-multipart policy). Five registry recoveries resolve by exact `UploadId`/key; IAM-not-exact-scopable is
an open gate.

## 8. EXACT SQL/function/ACL manifest (MEDIUM-1) — all `SECURITY DEFINER`, owner `tagekyc_raw_export_deployer`, `SET search_path=pg_catalog`, `REVOKE ALL ON EACH FUNCTION FROM PUBLIC, tagekyc_runtime`, exactly one overload per name (Up creates, Down drops it; no orphan)
Roles literal: `tagekyc_raw_export_custody_encryptor`, `tagekyc_raw_export_reconciler`, `tagekyc_raw_export_lifecycle`.
Format — `name(args) RETURNS shape | GRANT EXECUTE <role> | pred→succ | success outcomes | failure SQLSTATE + message`.
Key reservation + history:
- `tagekyc.raw_export_prepare_attempt_key_reservation(p_attempt_key_reservation_id uuid, p_attempt_id uuid, p_source_artifact_id uuid) RETURNS TABLE(outcome text, preparation_id uuid, preparation_fence bigint, provider_operation_token text, preparation_lease_expires_at_utc timestamptz)` | GRANT EXECUTE tagekyc_raw_export_custody_encryptor | {Absent, ReadyForFreshPreparation}→PreparingLive (writes head + `Opened` history row) | Prepared, InProgress, ExistingMatch, Conflict, HeadNotReserved, Terminated, StaleFence | SQLSTATE P0001 message `RAW_EXPORT_KEY_RESERVATION_PREPARE_INVALID`.
- `tagekyc.raw_export_activate_attempt_key_reservation(p_attempt_key_reservation_id uuid, p_preparation_id uuid, p_preparation_fence bigint, p_wrapped_dek_ciphertext bytea, p_wrapped_dek_nonce bytea, p_wrapped_dek_tag bytea, p_wrapped_dek_metadata_digest bytea, p_provider_operation_receipt text) RETURNS text` | GRANT EXECUTE tagekyc_raw_export_custody_encryptor | PreparingLive→Active | Activated, StalePreparation, DigestMismatch | P0001 `RAW_EXPORT_KEY_ACTIVATION_INVALID`.
- `tagekyc.raw_export_mark_attempt_key_preparation_expired(p_attempt_key_reservation_id uuid, p_preparation_id uuid, p_preparation_fence bigint) RETURNS text` | GRANT EXECUTE tagekyc_raw_export_reconciler | PreparingLive→PreparingExpiredAwaitingResolution (DB time ≥ lease; writes `Expired` history row) | Expired, NotDue, StalePreparation | P0001 `RAW_EXPORT_KEY_PREP_EXPIRY_INVALID`.
- `tagekyc.raw_export_inspect_attempt_key_reservation(p_attempt_key_reservation_id uuid) RETURNS TABLE(preparation_disposition text, attempt_id uuid, attempt_key_context_fingerprint bytea, current_preparation_id uuid, current_preparation_fence bigint, current_preparation_lease_expires_at_utc timestamptz, current_provider_operation_token text, prepared_at_utc timestamptz, revoked_at_utc timestamptz)` | GRANT EXECUTE tagekyc_raw_export_custody_encryptor, tagekyc_raw_export_reconciler | read | (rows or none) | (none).
- `tagekyc.raw_export_resolve_attempt_key_provider_outcome(p_attempt_key_reservation_id uuid, p_preparation_id uuid, p_preparation_fence bigint, p_resolution text, p_provider_cleanup_reference text, p_provider_resolution_evidence_digest bytea) RETURNS text` | GRANT EXECUTE tagekyc_raw_export_reconciler | {PreparingLive, PreparingExpiredAwaitingResolution}→{ReadyForFreshPreparation, ProviderOutcomeUnknown, ProviderCorruptOrUnverifiable, ProviderCleanupRequired} (writes the matching `Resolved*` history row) | Resolved, StalePreparation, IllegalResolution, EvidenceMissing | P0001 `RAW_EXPORT_KEY_RESOLUTION_INVALID`.
- `tagekyc.raw_export_activate_recovered_attempt_key_reservation(p_attempt_key_reservation_id uuid, p_preparation_id uuid, p_preparation_fence bigint, p_wrapped_dek_ciphertext bytea, p_wrapped_dek_nonce bytea, p_wrapped_dek_tag bytea, p_wrapped_dek_metadata_digest bytea, p_provider_operation_receipt text, p_provider_resolution_evidence_digest bytea) RETURNS text` | GRANT EXECUTE tagekyc_raw_export_reconciler | {PreparingLive, PreparingExpiredAwaitingResolution}→Active (recompute context+metadata digest with persisted suite; writes `RecoveredActivated` history row) | ActivatedRecovered, StalePreparation, DigestMismatch | P0001 `RAW_EXPORT_KEY_RECOVERED_ACTIVATION_INVALID`.
- `tagekyc.raw_export_acknowledge_attempt_key_cleanup(p_attempt_key_reservation_id uuid, p_preparation_id uuid, p_preparation_fence bigint, p_provider_cleanup_receipt text, p_provider_cleanup_evidence_digest bytea) RETURNS text` | GRANT EXECUTE tagekyc_raw_export_reconciler | ProviderCleanupRequired→ReadyForFreshPreparation (writes `CleanupAcknowledged` history row; evidence digest NOT NULL) | Acknowledged, StalePreparation, EvidenceMissing | P0001 `RAW_EXPORT_KEY_CLEANUP_ACK_INVALID`.
- `tagekyc.raw_export_revoke_attempt_key_reservation(p_attempt_key_reservation_id uuid) RETURNS text` | GRANT EXECUTE tagekyc_raw_export_reconciler, tagekyc_raw_export_lifecycle | Active→Revoked | Revoked, AlreadyRevoked, NotActive | P0001 `RAW_EXPORT_KEY_REVOKE_INVALID`.
Write session (each its own RETURNS):
- `tagekyc.raw_export_begin_write_session(p_write_session_id uuid, p_attempt_id uuid, p_provisional_object_identity uuid, p_object_key text) RETURNS bigint` | GRANT EXECUTE tagekyc_raw_export_custody_encryptor | Absent→Initiated | (new session_fence) | P0001 `RAW_EXPORT_WRITE_SESSION_BEGIN_INVALID`.
- `tagekyc.raw_export_attach_upload_id_enter_writing(p_write_session_id uuid, p_expected_session_fence bigint, p_upload_id text) RETURNS bigint` | GRANT EXECUTE tagekyc_raw_export_custody_encryptor | Initiated→Writing | (next fence) | P0001 `RAW_EXPORT_WRITE_SESSION_STATE_INVALID`.
- `tagekyc.raw_export_record_write_part(p_write_session_id uuid, p_expected_session_fence bigint, p_part_ordinal integer, p_provider_part_receipt text) RETURNS bigint` | GRANT EXECUTE tagekyc_raw_export_custody_encryptor | Writing→Writing | (next fence) | P0001 `RAW_EXPORT_WRITE_SESSION_STATE_INVALID`.
- `tagekyc.raw_export_reserve_finalization_nonce(p_write_session_id uuid, p_expected_session_fence bigint, p_finalization_nonce bytea) RETURNS TABLE(outcome text, finalization_nonce bytea, session_fence bigint)` | GRANT EXECUTE tagekyc_raw_export_custody_encryptor | Writing→Finalizing OR Finalizing→Finalizing | Reserved, ReplayedSame | P0001 `RAW_EXPORT_WRITE_SESSION_FINALIZATION_NONCE_MISMATCH` (different candidate) / `RAW_EXPORT_WRITE_SESSION_STATE_INVALID` (other state/fence).
- `tagekyc.raw_export_terminate_write_session_no_upload(p_write_session_id uuid, p_expected_session_fence bigint, p_local_termination_evidence text) RETURNS bigint` | GRANT EXECUTE tagekyc_raw_export_reconciler | Initiated(UploadId NULL)→NoProviderUploadEstablished | (next fence) | P0001 `RAW_EXPORT_WRITE_SESSION_NO_UPLOAD_TERMINATION_INVALID`.
- `tagekyc.raw_export_record_object_commit(p_write_session_id uuid, p_expected_session_fence bigint, p_object_key_committed text, p_version_id text, p_provider_commit_receipt text) RETURNS bigint` | GRANT EXECUTE tagekyc_raw_export_custody_encryptor | Finalizing→ObjectCommittedPendingVerification | (next fence) | P0001 `RAW_EXPORT_WRITE_SESSION_COMMIT_INVALID`.
- `tagekyc.raw_export_recover_object_commit(p_write_session_id uuid, p_expected_session_fence bigint, p_object_key_committed text, p_version_id text, p_provider_commit_receipt text) RETURNS bigint` | GRANT EXECUTE tagekyc_raw_export_reconciler | Finalizing→ObjectCommittedPendingVerification | (next fence) | P0001 `RAW_EXPORT_WRITE_SESSION_COMMIT_INVALID`.
- `tagekyc.raw_export_mark_object_verified(p_write_session_id uuid, p_expected_session_fence bigint) RETURNS bigint` | GRANT EXECUTE tagekyc_raw_export_reconciler | ObjectCommittedPendingVerification→VerifiedCompleted | (next fence) | P0001 `RAW_EXPORT_WRITE_SESSION_STATE_INVALID`.
- `tagekyc.raw_export_mark_multipart_abort_pending(p_write_session_id uuid, p_expected_session_fence bigint) RETURNS bigint` | GRANT EXECUTE tagekyc_raw_export_reconciler | {Writing, Finalizing}(UploadId NOT NULL)→MultipartAbortPending | (next fence) | P0001 `RAW_EXPORT_WRITE_SESSION_STATE_INVALID`.
- `tagekyc.raw_export_mark_multipart_aborted(p_write_session_id uuid, p_expected_session_fence bigint, p_abort_acknowledgement_receipt text) RETURNS bigint` | GRANT EXECUTE tagekyc_raw_export_reconciler | MultipartAbortPending→MultipartAborted (receipt NOT NULL) | (next fence) | P0001 `RAW_EXPORT_WRITE_SESSION_ABORT_INVALID`.
- `tagekyc.raw_export_mark_cleanup_pending(p_write_session_id uuid, p_expected_session_fence bigint) RETURNS bigint` | GRANT EXECUTE tagekyc_raw_export_reconciler | ObjectCommittedPendingVerification→CleanupPending | (next fence) | P0001 `RAW_EXPORT_WRITE_SESSION_STATE_INVALID`.
- `tagekyc.raw_export_mark_object_deleted(p_write_session_id uuid, p_expected_session_fence bigint) RETURNS bigint` | GRANT EXECUTE tagekyc_raw_export_lifecycle | CleanupPending→Deleted | (next fence) | P0001 `RAW_EXPORT_WRITE_SESSION_STATE_INVALID`.
- `tagekyc.raw_export_mark_object_quarantined(p_write_session_id uuid, p_expected_session_fence bigint) RETURNS bigint` | GRANT EXECUTE tagekyc_raw_export_lifecycle | CleanupPending→Quarantined | (next fence) | P0001 `RAW_EXPORT_WRITE_SESSION_STATE_INVALID`.
- `tagekyc.raw_export_inspect_write_session(p_write_session_id uuid) RETURNS TABLE(session_state text, session_fence bigint, upload_id text, object_key text, object_key_committed text, version_id text, provider_commit_receipt text, abort_acknowledgement_receipt text, finalization_nonce bytea)` | GRANT EXECUTE tagekyc_raw_export_reconciler, tagekyc_raw_export_lifecycle | read | (rows or none) | (none).
Attempt:
- `tagekyc.raw_export_terminate_source_encryption_attempt(p_attempt_id uuid, p_expected_encryption_attempt_revision bigint, p_expected_fence bigint, p_disposition text) RETURNS text` | GRANT EXECUTE tagekyc_raw_export_reconciler | (R2TerminationDisposition NULL, R2TerminatedAtUtc NULL)→('Terminated'|'TerminatedBeforeStart', statement_timestamp()) | Terminated, StaleFence, IllegalDisposition | P0001 `RAW_EXPORT_SOURCE_TERMINATION_CONTEXT_INVALID`.
Read: the two functions in §12.

## 9. Sparse / immutability matrices (MEDIUM-5) — FULL per-column, for all four tables
**Reservation head** (18 columns) per PreparationDisposition — N=NULL required, X=NON-NULL required, G=generated-internally,
C=allowed-to-change, P=prohibited-to-change; CurrentPreparationFence/RowRevision/RowFence always X:
- `PreparingLive`/`PreparingExpiredAwaitingResolution`: CurrentPreparationId X, CurrentPreparationLease X, CurrentProviderOperationToken X, WrappingSuiteId X(G, P), WrappingSuiteVersion X(G, P), WrappedDekCiphertext/Nonce/Tag/MetadataDigest N, PreparedAtUtc N, RevokedAtUtc N.
- `ProviderOutcomeUnknown`/`ProviderCorruptOrUnverifiable`/`ProviderCleanupRequired`: CurrentPreparationId X, CurrentProviderOperationToken X (CleanupRequired also head-visible cleanup pointer via history), lease X; wrapped N; PreparedAtUtc N; RevokedAtUtc N.
- `ReadyForFreshPreparation`: CurrentPreparationId N, CurrentPreparationLease N, CurrentProviderOperationToken N, wrapped N, PreparedAtUtc N, RevokedAtUtc N (prior evidence lives ONLY in history; head cleared).
- `Active`: wrapped Ciphertext/Nonce/Tag/MetadataDigest X (P), WrappingSuiteId/Version X (P), PreparedAtUtc X (P), RevokedAtUtc N; CurrentPreparationId X (the activated generation).
- `Revoked`: all Active wrapped fields retained X (P; Destroy deferred), RevokedAtUtc X (P). Only Active→Revoked.
Immutable (P) after first set: AttemptId, AttemptKeyContextFingerprint, EncryptionAttemptFingerprint, KeyProviderId, KekId, KekVersion, KekFingerprint, WrappingSuiteId, WrappingSuiteVersion.
**Preparation-history** (append-only; every column immutable after insert): per EventKind, the relevant evidence column is X and the rest N (Opened: PreparationLease/ProviderOperationToken/WrappingSuite X; ResolvedNoResult/Unknown/Corrupt: ProviderResolutionEvidenceDigest X; ResolvedCleanupRequired: ProviderCleanupReference + ProviderResolutionEvidenceDigest X; CleanupAcknowledged: ProviderCleanupReceipt + ProviderCleanupEvidenceDigest X; RecoveredActivated: RecoveredWrappedMetadataDigest + ProviderOperationReceipt + ProviderResolutionEvidenceDigest X). No row is ever updated/deleted.
**Write-session** (columns UploadId, FinalizationNonce, ObjectKeyCommitted, VersionId, ProviderCommitReceipt, AbortAcknowledgementReceipt, LocalTerminationEvidence; SessionFence≥1 always): `Initiated` all committed/upload fields N; `Writing` UploadId X, rest N; `Finalizing` UploadId X, FinalizationNonce X, rest N; `NoProviderUploadEstablished` UploadId N, LocalTerminationEvidence X, rest N; `MultipartAbortPending` UploadId X, others N; `MultipartAborted` UploadId X, AbortAcknowledgementReceipt X, others N; `ObjectCommittedPendingVerification`/`VerifiedCompleted`/`CleanupPending`/`Deleted`/`Quarantined` UploadId X, FinalizationNonce X, ObjectKeyCommitted X (=ObjectKey), VersionId X, ProviderCommitReceipt X.
**Write-session-part** (immutable append): PartOrdinal X (0..9999), ProviderPartReceipt X, RecordedAtUtc X; no update/delete.

## 10. Wrapped-key + evidence preimages (MEDIUM-3) — LP = landed C1HashCanonical (u32-BE length prefix; UUID = RFC-4122 network byte order 16 bytes; integers big-endian fixed width; NFC UTF-8 text; raw bytes for bytea; absent field = zero-length; max text 512)
`AttemptKeyContextFingerprint = C1HashCanonical("tip-88c1-attempt-key-context-v1", { AttemptKeyReservationId(uuid),
AttemptId(uuid), EncryptionAttemptFingerprint(bytea), KeyProviderId(text), KekId(text), KekVersion(u32), KekFingerprint(text),
WrappingSuiteId(text), WrappingSuiteVersion(u32) })`. Wrapped-DEK AEAD AAD = `LP("tip-88c1-wrapped-dek-aad-v1") ‖
AttemptKeyContextFingerprint`. `WrappedDekMetadataDigest = C1HashCanonical("tip-88c1-wrapped-dek-metadata-v1", {
AttemptKeyContextFingerprint(bytea), WrappingSuiteId(text), WrappingSuiteVersion(u32), WrappedDekNonce(bytea),
WrappedDekCiphertext(bytea), WrappedDekTag(bytea) })`. `ProviderResolutionEvidenceDigest = C1HashCanonical(
"tip-88c1-key-provider-resolution-evidence-v1", { AttemptKeyContextFingerprint(bytea), PreparationId(uuid),
PreparationFence(u64), ResolutionKind(text), ProviderOperationToken(text), ProviderOperationReceipt(text, absent→zero-len),
ProviderCleanupReference(text, absent→zero-len), WrappedDekMetadataDigest(bytea, absent→zero-len) })`.
`ProviderCleanupEvidenceDigest = C1HashCanonical("tip-88c1-key-provider-cleanup-evidence-v1", {
AttemptKeyContextFingerprint(bytea), PreparationId(uuid), PreparationFence(u64), CleanupKind(text),
ProviderCleanupReference(text), ProviderCleanupReceipt(text) })`. SD functions **recompute** both digests from persisted
trusted context + the exact external receipt/reference inputs; they do NOT accept a caller-computed 32-byte digest as
authoritative. Independent golden vectors for all four constructions.

## 11. Readiness literals + first-match precedence (MEDIUM-2/13)
`PROD_RAW_EXPORT_PROVIDER_TOPOLOGY_INVALID`, `PROD_RAW_EXPORT_CUSTODY_ROLE_MISSING`, `PROD_RAW_EXPORT_CUSTODY_ROLE_ATTRIBUTE_INVALID`,
`PROD_RAW_EXPORT_CUSTODY_LOGIN_ATTRIBUTE_INVALID`, `PROD_RAW_EXPORT_CUSTODY_GRANT_INVALID`, `PROD_RAW_EXPORT_CUSTODY_CROSS_MEMBERSHIP`,
`PROD_RAW_EXPORT_CUSTODY_SET_ROLE_ENABLED`, `PROD_RAW_EXPORT_KEK_NOT_QUALIFIED`, `PROD_RAW_EXPORT_S3_ENDPOINT_MISSING`,
`PROD_RAW_EXPORT_S3_ENDPOINT_NOT_ALLOWED`, `PROD_RAW_EXPORT_S3_CREDENTIAL_SOURCE_INVALID`, `PROD_RAW_EXPORT_S3_AMBIENT_CREDENTIALS_FORBIDDEN`,
`PROD_RAW_EXPORT_OBJECT_VERSIONING_INVALID`, `PROD_RAW_EXPORT_OBJECT_LOCK_POSTURE_INVALID`, `PROD_RAW_EXPORT_MULTIPART_LIFECYCLE_INVALID`,
`PROD_RAW_EXPORT_MULTIPART_DISCOVERY_INVALID`, `PROD_RAW_EXPORT_KEY_RESERVATION_STATE_INVALID`, `PROD_RAW_EXPORT_KEY_PREPARATION_HISTORY_INVALID`,
`PROD_RAW_EXPORT_KEY_PREPARATION_LEASE_INVALID`, `PROD_RAW_EXPORT_KEY_OPERATION_RECOVERY_UNSUPPORTED`, `PROD_RAW_EXPORT_KEY_PROVIDER_CLEANUP_UNSUPPORTED`,
`PROD_RAW_EXPORT_KEY_PROVIDER_CLEANUP_OUTCOME_UNKNOWN`, `PROD_RAW_EXPORT_WRITE_SESSION_STATE_INVALID`, `PROD_RAW_EXPORT_WRITE_SESSION_ABORT_OUTCOME_INVALID`.
First-match precedence (component in parentheses): provider topology (composition-root) → role existence/attributes/
membership/ACL (CustodyRoleReadinessValidator, querying pg_roles+pg_auth_members) → KEK qualification (KekReadiness) →
S3 endpoint/credentials (ObjectStoreReadinessValidator) → object versioning/lock/lifecycle/discovery (ObjectStoreReadiness) →
key reservation-state/history/lease/recovery/cleanup (KeyReservationReadiness) → write-session state/abort (WriteSessionReadiness).

## 12. Read projections (verified on-code parity; MinIO/S3 §16)
`tagekyc.raw_export_read_source_encryption_context(p_source_artifact_id uuid, p_attempt_id uuid) RETURNS TABLE(attempt_id uuid,
attempt_key_reservation_id uuid, encryption_attempt_revision bigint, fence bigint, r2_termination_disposition text,
r2_terminated_at_utc timestamptz, stable_data_scope_id text, controller_identity text, verification_session_id uuid,
capture_artifact_id uuid, capture_revision integer, raw_class text, claimed_plaintext_length bigint, media_type text,
content_commitment bytea, content_commitment_schema_version integer, content_commitment_key_id text, content_commitment_key_version integer,
key_provider_id text, kek_id text, kek_version integer, kek_fingerprint text, provisional_object_identity uuid, encryption_suite_id text,
encryption_framing_version integer, chunk_size integer, nonce_strategy_id text, nonce_derivation_seed_reference_or_wrapped_seed text,
nonce_derivation_seed_commitment bytea, framing_parameters_digest bytea, encryption_attempt_fingerprint bytea, authority_snapshot_id uuid,
effective_plaintext_retention_expires_at_utc timestamptz, absolute_source_expires_at_utc timestamptz, reservation_expires_at_utc timestamptz,
ownership_lease_expires_at_utc timestamptz, current_encryption_attempt_id uuid, head_reservation_revision bigint, head_fence bigint,
head_custody_state text)` | GRANT EXECUTE tagekyc_raw_export_custody_encryptor | no rows unless head CustodyState='Reserved'.
`tagekyc.raw_export_read_source_verification_context(p_source_artifact_id uuid, p_attempt_id uuid) RETURNS TABLE(attempt_id uuid,
attempt_key_reservation_id uuid, encryption_attempt_revision bigint, fence bigint, r2_termination_disposition text, r2_terminated_at_utc timestamptz,
content_commitment bytea, content_commitment_key_id text, content_commitment_key_version integer, provisional_object_identity uuid,
encryption_attempt_fingerprint bytea, source_reservation_fingerprint bytea)` | GRANT EXECUTE tagekyc_raw_export_reconciler. Parity verified on-code:
`authority_snapshot_id` = `RawExportSourceReservationRow.AuthoritySnapshotId` (Guid/uuid); `reservation_expires_at_utc`=reservation;
`ownership_lease_expires_at_utc`=attempt; head fields = `RawExportSourceHeadRow`. No `OwnerKind`/`OwnerId` (not landed).

## 13. Termination guard + role catalog (unchanged from v6, exact)
Guard `enforce_raw_export_source_core_write()` requires simultaneously `current_user='tagekyc_raw_export_deployer'`,
`TG_TABLE_SCHEMA='tagekyc'` + `TG_TABLE_NAME` routing, attempt-UPDATE only when transaction-local GUC
`tagekyc.raw_export_source_termination_context='terminate'`, OLD `(R2TerminationDisposition IS NULL AND R2TerminatedAtUtc
IS NULL)`→NEW settled, permitted-delta exactly `{R2TerminationDisposition, R2TerminatedAtUtc}` (others `NEW IS NOT DISTINCT
FROM OLD`), DELETE denied; GUC captured/reset on success+exception (test missing AND empty); SQLSTATE P0001 message
`RAW_EXPORT_SOURCE_TERMINATION_CONTEXT_INVALID`. Roles §10-v6 attributes; Down as §1.

## 14. Test → mutation → assertion (MEDIUM-4) — `TagEkyc.IntegrationTests.Tip88C1B2DurableCustodyTests` / `TagEkyc.ArchTests.Tip88C1B2DurableCustodyArchTests`; each: FQN | scratch mutation | expected failing assertion; positive controls distinct; restart cannot skip; every mutation restored pre-green
`KeyPreparation_MultipleFreshPreparations_PreserveEveryPriorEvidence` | overwrite/delete a prior history row | `Assert.Equal(expectedHistoryRows, readback)` fails (row count/evidence differs). ·
`Prepare_RejectsCallerSuppliedSelectorAttempt` | add+trust a caller selector param | forged fingerprint persisted → `Assert.Throws<PostgresException>` fails. ·
`KeyPreparation_ExpiryTransition_OnlyWhenDue` | allow expiry before lease | pre-lease expiry returns Expired → `Assert.Equal("NotDue")` fails. ·
`KeyRecovery_ProviderCleanupOperationAbsent_FailsReadiness` | provider lacks recovery op | `Assert.Contains("PROD_RAW_EXPORT_KEY_OPERATION_RECOVERY_UNSUPPORTED")` fails. ·
`KeyRecovery_CleanupOutcomeUnknown_BlocksFreshPreparation` | treat Unknown as Cleaned | fresh prep proceeds → `Assert.Equal("ProviderCleanupRequired")` fails. ·
`KeyRecovery_CleanupAckWithoutEvidence_Rejected` | make evidence digest nullable | bare ack succeeds → `Assert.Throws` fails. ·
`KeyRecovery_RecoveredWrappedResult_ActivatesViaReconcilerFn` (positive) | make the encryptor path accept recovered | wrong-actor activation succeeds → ACL `Assert.Throws` fails. ·
`KeyRecovery_ProviderOutcomeUnknown_NeverCreatesSecondDek` | let Unknown fall through to prepare | 2nd DEK → `Assert.Equal("ProviderOutcomeUnknown")` fails. ·
`KeyRecovery_WrappingSuiteMismatch_CannotActivate` | skip the C# suite check | mismatched suite activates → `Assert.Throws`/`Assert.Equal("DigestMismatch")` fails. ·
`WrappedDek_MetadataDigestBindsSuiteNonceCiphertextTag` | drop ciphertext/tag from preimage | cross-attempt wrapped verifies → `Assert.False(activated)` fails. ·
`Evidence_ResolutionAndCleanupDigests_GoldenVectors` | flip one preimage byte | hex `Assert.Equal` fails. ·
`Evidence_DigestRecomputed_NotCallerAuthoritative` | accept caller digest | a swapped receipt with old digest activates → `Assert.Throws` fails. ·
`WriteSession_ObjectStateGraph_ExactTransitions` | permit VerifiedCompleted→MultipartAbortPending | illegal transition succeeds → `Assert.Equal("RAW_EXPORT_WRITE_SESSION_STATE_INVALID")` fails. ·
`WriteSession_InitiatedWithoutUpload_TerminatesLocally` (positive) | route no-upload through MultipartAbortPending | UploadId-NULL abort attempted → `Assert.Equal("NoProviderUploadEstablished")` fails. ·
`WriteSession_ProviderUpload_RequiresAbortAcknowledgement` | allow MultipartAborted without receipt | aborted w/o receipt → `Assert.Throws` fails. ·
`WriteSession_UnknownAbortOutcome_RemainsBlocking` | mark aborted on unknown | unknown→MultipartAborted → `Assert.Equal("MultipartAbortPending")` fails. ·
`WriteSession_RecordObjectCommit_RequiresKeyVersionReceipt` | allow empty VersionId | empty version commit → `Assert.Throws` fails. ·
`WriteSession_FinalizationNonce_SameValueReplays` (positive) | mutate replay to regenerate | 2nd call differs → `Assert.Equal(first, second)` fails. ·
`WriteSession_FinalizationNonce_DifferentValueMismatch` | accept a different nonce | different nonce succeeds → `Assert.Throws` fails. ·
`WriteSession_TransitionRequiresExactSessionFence` | ignore expected fence | stale-fence transition succeeds → `Assert.Throws` fails. ·
`Recovery_LostUploadId_ResolvedByExactKey_NoGenericList` | use ListObjects | audit `Assert.DoesNotContain("ListObjects")` fails. ·
`ObjectStore_RequiresVersioning_ExactVersionId_NotEtag` | accept ETag | `Assert.Contains("PROD_RAW_EXPORT_OBJECT_VERSIONING_INVALID")` fails. ·
`S3_EndpointMissing_FailsReadiness` / `S3_AwsFallback_FailsReadiness` / `S3_AmbientCredentials_FailsReadiness` / `S3_OnlyConfiguredEndpointContacted` | remove the corresponding guard | `Assert.Contains(<the matching literal>)` / endpoint `Assert.Equal` fails. ·
`Minio_PersistentVolume_ObjectSurvivesContainerReplacement` (**mandatory; fails-not-skips**) | remove the fixture-provisioned assert | fixture missing → skip → `Assert.True(fixtureProvisioned)` fails. ·
`Termination_UpdateAllowedUnderContext_PositiveControl` | guard blocks all UPDATE | correct-context UPDATE rejected → `Assert.Equal("Terminated")` fails. ·
`Termination_UpdateRejectedWithoutContext` / `Termination_RejectsNonDispositionColumnChange` / `Termination_DeleteDenied` / `Termination_AppendOnce_RejectsRewrite` / `Termination_ContextRestored_OnExceptionAndSuccess_MissingAndEmpty` | drop the respective predicate | the illegal op succeeds / GUC leaks → `Assert.Throws` / `Assert.Equal("", current_setting)` fails. ·
`Roles_CapabilityAttributes_Exact` / `Roles_LoginAttributes_Exact` | flip an attribute | pg_roles mismatch → `Assert.Equal` fails. ·
`Roles_CrossMembership_FailsReadiness` / `Roles_SetRoleEnabled_FailsReadiness` / `Roles_GrantToRuntimeOrPublic_Rejected` | add cross-membership/SET/runtime grant | `Assert.Contains(<literal>)` / `Assert.Throws` fails. ·
`ReadSurface_EncryptionContext_ExactSignatureAndAcl` / `ReadSurface_VerificationContext_ExactSignatureAndAcl` | change owner/add grantee/overload | catalog `Assert.Equal(manifest)` fails. ·
`Kek_DurableStoreWithFixtureKek_FailsReadiness` | mark fixture KEK qualified | `Assert.Contains("PROD_RAW_EXPORT_KEK_NOT_QUALIFIED")` fails. ·
`Aead_PurposeIsolation_EncryptCannotVerify` | share one service | `Assert.Throws` fails. ·
`SparseState_KeyReservationHead_AllDispositionsExact` / `SparseState_PreparationHistory_AppendOnlyImmutable` / `SparseState_WriteSession_AllStatesExact` / `SparseState_WriteSessionParts_Immutable` | violate a shape/append-only | CHECK/trigger `Assert.Throws` fails. ·
`Readiness_FirstMatchPrecedence_Exact` | reorder two components | first-match differs → `Assert.Equal(expectedCode)` fails. ·
`Migration_DownReapply_UnderProductionLikeMemberships` | leave an orphan grant in Down | residual ACL/membership → `Assert.Empty(orphans)` fails.

## 15. Exact buildable file allowlist (no `.../`; locked migration timestamp rule only)
`src/TagEkyc.Infrastructure/Persistence/Migrations/{UTCyyyyMMddHHmmss}_Tip88C1B2DurableCustodyFoundation.cs` + `.Designer.cs`
(the `{UTCyyyyMMddHHmmss}` token is a LOCKED filename-generation rule, not an unresolved placeholder),
`src/TagEkyc.Infrastructure/Persistence/Migrations/TagEkycDbContextModelSnapshot.cs`,
`src/TagEkyc.Infrastructure/Persistence/TagEkycDbContext.cs`, `src/TagEkyc.Infrastructure/TagEkyc.Infrastructure.csproj`,
`src/TagEkyc.Contracts/RawExport/AttemptKeyReservationContracts.cs`, `src/TagEkyc.Contracts/RawExport/ProvisionalObjectStoreContracts.cs`,
`src/TagEkyc.Contracts/RawExport/KekOperationContracts.cs`, `src/TagEkyc.Contracts/RawExport/AttemptAeadOperationContracts.cs`,
`src/TagEkyc.Infrastructure/RawExport/PostgresAttemptKeyReservationProvider.cs`, `src/TagEkyc.Infrastructure/RawExport/AttemptAeadOperationService.cs`,
`src/TagEkyc.Infrastructure/RawExport/S3ProvisionalObjectStore.cs`, `src/TagEkyc.Infrastructure/RawExport/ProvisionalWriteSessionRegistry.cs`,
`src/TagEkyc.Infrastructure/RawExport/FixtureKekOperationProvider.cs`, `src/TagEkyc.Infrastructure/RawExport/KekProvisioningRecoveryOperation.cs`,
`src/TagEkyc.Infrastructure/RawExport/CustodyRoleReadinessValidator.cs`, `src/TagEkyc.Infrastructure/RawExport/ObjectStoreReadinessValidator.cs`,
`src/TagEkyc.Infrastructure/RawExport/DurableCustodyServiceCollectionExtensions.cs`,
`src/TagEkyc.Infrastructure/Persistence/Entities/RawExportAttemptKeyReservationRow.cs`,
`src/TagEkyc.Infrastructure/Persistence/Entities/RawExportAttemptKeyPreparationEventRow.cs`,
`src/TagEkyc.Infrastructure/Persistence/Entities/RawExportProvisionalWriteSessionRow.cs`,
`src/TagEkyc.Infrastructure/Persistence/Entities/RawExportProvisionalWriteSessionPartRow.cs`,
`src/TagEkyc.Api/Program.cs`, `src/TagEkyc.Api/ReadinessEndpoint.cs`, `src/TagEkyc.Api/appsettings.json`,
`tests/TagEkyc.IntegrationTests/Tip88C1B2DurableCustodyTests.cs`, `tests/TagEkyc.IntegrationTests/DurableCustodyMinioFixture.cs`,
`tests/TagEkyc.IntegrationTests/TagEkyc.IntegrationTests.csproj` (adds `Testcontainers` `3.10.0` + `Testcontainers.Minio` `3.10.0`),
`tests/TagEkyc.ArchTests/Tip88C1B2DurableCustodyArchTests.cs`,
`docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_b2_durable_custody_as_built.md`,
`docs/phase1_scope_and_debt_registry_v0_1.md`. S3 SDK pinned `AWSSDK.S3` `3.7.405.4`. Any file outside this list is STOP/RRI.

## 16. MinIO/S3 execution + persistent-volume restart (unchanged v6)
Config fail-closed: explicit non-empty ServiceURL; ForcePathStyle=true; no AWS default fallback; IMDS discovery disabled;
explicit custody creds only; TLS outside loopback fixture. Restart proof: named volume `tagekyc-tip88c1-minio-data-<run-id>`
mounted `/data`; create V → start MinIO A → configure bucket+versioning+abort-multipart-lifecycle → persist object, capture
ObjectKey+VersionId → recreate clients + prove read → stop/remove A keeping V → start MinIO B on V → recreate clients →
read exact ObjectKey+VersionId + verify bytes → cleanup. Fails-not-skips on any fixture failure. Scope: proves
PostgreSQL-backed state across DbContext/provider reconstruction + MinIO data across container replacement; does NOT claim
PostgreSQL-container replacement.

**Open deployment gates:** production OpenBao/Vault/HSM KEK; S3 endpoint/creds + versioning + abort-multipart lifecycle +
Object Lock; three LOGIN credential provisionings; DistributedDurable; residual multipart-list IAM scope; DestroyReservation/
crypto-shred; PostgreSQL-container-replacement durability.
