# A3 v0.6 — retention snapshot and custody checkpoint contract

Status: candidate implementation contract, documentation only; not implementation authority. Consumes the unchanged E01 v0.5 reference/permit contract bound by Parent v0.6. No new subject agreement, no early export authorization, no change to the export `Completed` guard. Existing migrations are evidence, never the mutation targets.

## CP01 — reuse census and precise boundary

The existing `raw_export_authority_snapshots` table already is the source-authority event/provenance store. A3 extends it with a closed authority discriminator and immutable retention references; it does not build a parallel R2–R6 custody store. The existing `ConsentPolicyId/ConsentPolicyVersion` columns reference `raw_export_policy_versions`, NOT a B2 consent-event record. On a retained row they identify the actual server-selected policy whose later export consent must independently be satisfied. They do not assert that a subject-export consent event exists before Completed.

| Source file, relative to `src/TagEkyc.Infrastructure/Persistence/Migrations/` | Landed executable boundary | Candidate change |
| --- | --- | --- |
| `20260731045718_Tip88C1B2AuthoritySnapshotLifecycle.cs` | table, insert guard, grant/withdraw/revoke append | Tagged retention linkage; preserve append-only lifecycle |
| `20260731082733_Tip88C1B2CoreNewCandidate.cs` | `raw_export_append_authority_snapshot`; `raw_export_resolve_current_authority_for_source`; New-candidate completion | Keep legacy writer; add narrowly named retention writer; current resolver preserves return shape but validates retained references |
| `20260731130919_Tip88C1B2BetaExistingCandidates.cs` | `complete_raw_export_source_ingress_claim` | Select exact persisted authority kind before consent predicate; never select latest export decision |
| `20260906055502_Tip88C1C6BIngressSqlComposition.cs` | retained begin/core; complete42 handoff | New retained overload consumes exact permit; existing complete42 keeps shape, delegates to amended completion |
| `20260810120000_Tip88C1B2R3VerifiedCiphertextStaging.cs:277–405` | `raw_export_stage_verified_source_ciphertext` | Same fresh staging checkpoint, lock shared reference before source row locks, tagged predicate |
| `20260812120000_Tip88C1B2R4R6SourceFinalization.cs:295–505` | `raw_export_commit_staged_source`; `raw_export_publish_available_source` | Same commit/publication points, tagged predicate, unchanged replay/CAS/digests |
| `20260812120000_Tip88C1B2R4R6SourceFinalization.cs:546–683` | read/complete/finalize cleanup | No new purge or cleanup authority; consume existing exact cleanup items |
| `20260815120000_Tip88C1C1ResolverAssembly.cs:430–576,656–767` | `raw_export_freeze_job_source_bindings`; `raw_export_seal_authenticated_assembly` | Keep independent export consent; acquire retained reference lock before custody locks; current source authority remains mandatory |
| `20260904154848_Tip88C1C6AProductionAuthorityDeliveryBarrier.cs:15–262` | `raw_export_c3_current_authority_eligible` | Retention is source eligibility only; exact current export decision/permit/consent remains separately required before Streaming |
| `20260819120000_Tip88C1C3AuthenticatedPackageDelivery.cs:334–401` plus C6A's installed body amendment | `raw_export_begin_recipient_package_delivery_stream(uuid,uuid,uuid,uuid,bytea)` | Retained branch preacquires every admission lock before one shared fresh clock; outer delivery/key expiry and helper eligibility use that same time |

All changed function bodies are installed by the A3 forward migration. Preserve every historical function body and migration file. Schema/model amendments and body replacements must be in the same atomic migration. Down refuses populated retained linkage rather than transforming retention records into export authority.

## CP02 — exact additive snapshot DDL

Add the following columns to `tagekyc.raw_export_authority_snapshots`. All nullable columns have no default. The discriminator default only labels pre-existing rows according to their existing, enforced purpose; it creates no new authority.

| Column | SQL type / nullability / default | Writer / rule |
| --- | --- | --- |
| `AuthorityKind` | `varchar(32) NOT NULL DEFAULT 'LegacyExport'` | Closed `LegacyExport`, `SourceRetention` |
| `RetentionAuthorityId` | `uuid NULL` | Retention grant only, nonzero |
| `RetentionAuthorityRevision` | `bigint NULL` | Retention grant only, >=1 |
| `ConsentBindingId` | `uuid NULL` | Retention grant only, nonzero |
| `CustodyPrincipalId` | `uuid NULL` | Retention grant only, nonzero; equal `CapturedByPrincipalId` |
| `RuntimeBindingId` | `uuid NULL` | Retention grant only, nonzero; exact A1 execution binding |

New constraints:

```sql
CHECK ("AuthorityKind" IN ('LegacyExport','SourceRetention'));
CHECK (
 ("AuthorityKind"='LegacyExport'
  AND "RetentionAuthorityId" IS NULL AND "RetentionAuthorityRevision" IS NULL
  AND "ConsentBindingId" IS NULL AND "CustodyPrincipalId" IS NULL
  AND "RuntimeBindingId" IS NULL
  AND ("ApprovedPurpose" IS NULL OR "ApprovedPurpose"='SubjectRawBiometricExport'))
 OR
 ("AuthorityKind"='SourceRetention' AND (
  ("EventType"='Granted' AND "ApprovedPurpose"='SourceRetention'
   AND "RetentionAuthorityId" IS NOT NULL AND "RetentionAuthorityRevision">=1
   AND "ConsentBindingId" IS NOT NULL AND "CustodyPrincipalId" IS NOT NULL
   AND "RuntimeBindingId" IS NOT NULL
   AND "CustodyPrincipalId"="CapturedByPrincipalId")
  OR
  ("EventType" IN ('Withdrawn','Revoked')
   AND "RetentionAuthorityId" IS NULL AND "RetentionAuthorityRevision" IS NULL
   AND "ConsentBindingId" IS NULL AND "CustodyPrincipalId" IS NULL
   AND "RuntimeBindingId" IS NULL AND "ApprovedPurpose" IS NULL)
 )));
FOREIGN KEY ("RetentionAuthorityId","RetentionAuthorityRevision")
 REFERENCES tagekyc.raw_source_retention_permits("RetentionAuthorityId","Revision") ON DELETE RESTRICT;
FOREIGN KEY ("ConsentBindingId")
 REFERENCES tagekyc.raw_source_consent_bindings("ConsentBindingId") ON DELETE RESTRICT;
FOREIGN KEY ("RuntimeBindingId")
 REFERENCES tagekyc.capture_execution_bindings("CaptureExecutionBindingId") ON DELETE RESTRICT;
```

Name these `ck_a3_snapshot_authority_kind`, `ck_a3_snapshot_retention_shape`, `fk_a3_snapshot_retention_permit`, `fk_a3_snapshot_consent_binding`, `fk_a3_snapshot_runtime_binding`. The existing value CHECK changes only its purpose atom to the tagged domain above; all nonzero IDs, time inequalities and closed raw-class/profile rules remain. Add `CHECK` nonzero UUID atoms for the four new nullable UUID columns when not NULL (`RetentionAuthorityId`, `ConsentBindingId`, `CustodyPrincipalId`, `RuntimeBindingId`); the bigint revision is positive. No new default principal or permit.

Add index `ix_a3_snapshot_retention_permit` on `(RetentionAuthorityId,RetentionAuthorityRevision)` WHERE `AuthorityKind='SourceRetention' AND EventType='Granted'`. Keep existing PK and scope/revision unique constraint unchanged. Add a partial unique index `uq_a3_snapshot_retained_grant` on `(ClientApplicationId,VerificationSessionId,CaptureAcceptanceId,RawClass,RetentionAuthorityId,RetentionAuthorityRevision,RuntimeBindingId)` WHERE `AuthorityKind='SourceRetention' AND EventType='Granted'`: a retry cannot append a second grant for the same exact lineage. A terminal snapshot cannot be bypassed by granting that same permit again.

The amended existing `enforce_raw_export_authority_snapshot_insert()` performs a cross-table equality check at INSERT, under the reference lock: permit and consent binding match PrincipalId/ClientApplicationId/VerificationSessionId; permit matches PolicyId/PolicyVersion, exact reference revision, allowed RawClass; runtime binding matches same immutable principal/permit/binding; acceptance matches session/client/artifact/class. Every mismatch raises `A3_RETENTION_SNAPSHOT_LINEAGE_MISMATCH`. Direct-table INSERT remains forbidden by the existing deployer/context guard. UPDATE/DELETE remain forbidden. For Withdrawn/Revoked the target grant's `AuthorityKind` must equal the new row's kind, and all added grant-only columns are NULL.

Existing legacy grant function keeps its signature and writes `AuthorityKind='LegacyExport'` explicitly. Existing snapshot withdraw/revoke functions copy kind from their locked target grant; they do not copy grant-only retention values into terminal events. E01 reference withdrawal does not require appending one snapshot event per source: synchronous reference revision invalidation is sufficient and every fresh resolver checks it.

## CP03 — retention snapshot writer and source resolver

New internal function:

```sql
tagekyc.raw_export_append_retained_authority_snapshot(
 p_runtime_binding_id uuid,p_capture_acceptance_id uuid,p_raw_class text,
 p_retention_authority_id uuid,p_retention_authority_revision bigint)
RETURNS TABLE("Revision" bigint,"AuthoritySnapshotId" uuid)
```

The function consumes only exact IDs. It derives Principal/Client/session/policy/profile/evidence from locked E01 permit and runtime binding. It is not a second grant API. It checks `raw_export_current_actor() = permit.PrincipalId`, current effective consent reference at one post-lock DB `clock_timestamp()`, current permit, exact immutable binding, and class membership. Existing exact snapshot tuple returns its stored Revision/Id; terminal latest snapshot or wrong linkage raises `A3_RETENTION_SNAPSHOT_NOT_AUTHORIZED`, with no append. Otherwise append through the existing grant-context guard, scope revision = current max +1, fresh UUID snapshot ID.

Retained snapshot schema stays `AuthoritySnapshotSchemaVersion=1`; the existing schema describes custody fields, not export permission. `AuthorityArtifactId=RetentionAuthorityId`, `AuthorityArtifactVersion=1` identifies the new retention artifact schema; actual permit revision is the new bigint column and is never narrowed. `CapturedByPrincipalId=CustodyPrincipalId=permit.PrincipalId`. `ApprovedPurpose='SourceRetention'`, `ReuseDisposition='FreshAuthorityRequired'`, `ExtensionDisposition='Forbidden'`. `ConsentPolicyId/Version=permit.PolicyId/PolicyVersion`. Other existing profile columns copy the exact server-selected profile bound by the permit, never Agent fields. `AbsoluteSourceExpiresAtUtc` is the permit's finite source horizon; `ValidFromUtc=EvaluatedAtUtc=now`, `ValidUntilUtc=LEAST(permit.ExpiresAtUtc, evidence.ValidUntilUtc, source horizon)`. All horizons must exceed now.

`raw_export_resolve_current_authority_for_source(uuid,uuid,uuid,text,timestamptz)` retains its exact existing 23-column return shape. It first selects latest overall snapshot by Revision, then tests Granted/validity. For `LegacyExport`, existing behavior is byte-equivalent. For `SourceRetention`, it additionally resolves the snapshot's exact permit/consent binding/current reference and requires all CP02 equality and freshness predicates. It must NOT filter ineffective events before latest selection, search latest permit, silently substitute a newer reference revision, or require session still non-Completed after a valid pre-Completed issuance. Missing/updated/withdrawn/expired reference yields zero rows. Unknown kind yields zero rows.

New reader-only helper used by all tagged branches:

```sql
tagekyc.raw_export_retained_snapshot_is_current(
 p_authority_snapshot_id uuid,p_evaluated_at_utc timestamptz)
RETURNS boolean
```

It performs exact snapshot -> permit -> binding -> current reference joins and reference shared locking as in CP04, calling E01 `raw_source_resolve_retention_authority(uuid,bigint,uuid,uuid,uuid,text,timestamptz)` with snapshot-derived IDs and the same `p_evaluated_at_utc`; returns FALSE for missing/terminal/wrong lineage/current-revision mismatch/expired row. It is not an export permit validator. It accepts no Client/Principal supplied by a producer. It preserves a true `Granted` positive control before Completed and after Completed until actual retention authority loss.

## CP04 — ordering and withdrawal races

The common E01 lock is the signed bigint `hashtextextended('tip88c1:a3:consent-reference:' || ClientApplicationId::text || ':' || ExternalConsentArtifactRef,0)`. Readers use `pg_advisory_xact_lock_shared`; reference update/withdraw and permit revocation use `pg_advisory_xact_lock`. PostgreSQL UUID text and the exact persisted opaque external reference are used; no trimming/case folding. The E01 writer must never acquire custody/source/job rows while holding this lock.

The common retained prefix is an actual row lock, not merely A1 advisory domain70: internally probe the immutable owned VerificationSessionId(s) from the exact binding/reservation/attempt/publication/job; deduplicate and sort PostgreSQL UUID values ascending; lock every corresponding `tagekyc.verification_sessions` row `FOR UPDATE` in that order BEFORE any reference, source, job, publication, attempt, key or object row lock. Rejoin and compare the exact probed Client/session/source identities after acquiring the prefix. Missing or changed identity uses the callable's existing nondisclosing invalid/conflict result; never retry a probe against another target. The probe is not authorization and has no public result. Existing runtime/installation/generation/session70/capability80 locks, where already required, remain before this actual row prefix. A multi-session C1/C3 job locks its complete immutable session set before any reference key; it must not lock one session, then a reference, then another session. All retained replay branches take the prefix too, including an ExistingMatch return before a fresh-authority check.

R1/B: existing A1 locks -> actual owned session-row prefix -> reference shared lock -> existing snapshot-scope authority lock -> existing alias/exact locks in LEAST/GREATEST order -> claim/reservation mutation. The bound reader returns only after reference validation. The lock remains held to B commit, after complete42. Reference update first means fresh begin/complete rejects; R1 first means withdrawal waits until committed custody exists, then invalidates future checkpoints. No HTTP transaction/ambient transaction bridges N to B.

R3/R4/R5: internal read-only probe derives the owned session and reference key; actual session-row prefix -> shared reference -> existing custody/source-authority locks in their existing relative order -> revalidate exact snapshot/ref/permit/lineage. Probe mismatch fails closed `StateConflict`. Sample the fresh authority clock only after all these locks. The shared prefix serializes the retained broker's claim->head->attempt order against R3's attempt->head->claim order without rewriting either legacy order. Existing successful replay may return its frozen result but cannot authorize a next stage without that next stage's fresh check.

C1 freeze/seal and C3 begin-stream: actual complete session-row prefix -> deduplicated retained reference keys sorted signed bigint ascending -> existing job/source/export authority/consent locks in their existing relative order -> exact session/reference-set revalidation. Read one fresh admission time after all required locks. The existing B2 consent resolver itself takes session FOR UPDATE; that call must encounter the already-held row, never first acquire it after a reference. Actual B2 withdrawal retains session FOR UPDATE -> E01 reference -> B2 consent scope. Thus a real B2 withdrawal cannot hold session while C1/C3 holds reference and waits for that session. Holding the shared reference through Streaming CAS preserves withdrawal serialization. E01 reference-only withdrawal never acquires a session/source/provider lock after its reference lock; it does not add a synthetic session lookup.

Finite prefix installation census (retained branch only; preserve exact signatures, grants, result/replay and legacy body order): broker B-R/B-B/B-C and owner-only RE01 from the broker contract; `raw_export_append_retained_authority_snapshot`; tagged `complete_raw_export_source_ingress_claim` and its `complete_raw_export_source_ingress_claim_with_r2_handoff` wrapper; `raw_export_stage_verified_source_ciphertext`; `raw_export_commit_staged_source`; `raw_export_publish_available_source`; `raw_export_freeze_job_source_bindings`; `raw_export_seal_authenticated_assembly`; outer `raw_export_begin_recipient_package_delivery_stream(uuid,uuid,uuid,uuid,bytea)` and `raw_export_c3_current_authority_eligible` before their first export/source lock; TI01/TI02/NPS01; `raw_export_prepare_attempt_key_reservation`; `raw_export_begin_provisional_object_custody`; and still-callable `raw_export_terminate_source_encryption_attempt(uuid,bigint,bigint,text)`, including its already-terminated replay. Prefix acquisition is reentrant inside one transaction. CP03 current-retention resolvers are called only under this caller-owned prefix and acquire the shared reference reentrantly. The old `raw_export_record_r2_terminal_outcome(uuid,uuid,bigint,bigint,text,text)` rejects tagged retained attempts before UPDATE; it cannot bypass TI02 or create a second retained semantic finalizer.

For retained `raw_export_prepare_attempt_key_reservation`, preserve the exact signature and legacy branch but replace the pre-lock statement_timestamp admission value with clock_timestamp sampled after the session prefix and the exact attempt/head/reservation/key locks/revalidation. Both OwnershipLeaseExpiresAtUtc and ReservationExpiresAtUtc must exceed this same fresh value before any key/provider-operation insert; intent/operational termination still denies preparation. A wait across the owner/reservation lease is not permission to prepare using the call's old start time. RE01 renews only ReservationExpiresAtUtc by its existing bounded reservation-lease formula under the same CAS; it never renews frozen producer, source, snapshot, consent or permit horizons.

Lock census exclusions are explicit, not assumed from names. DurableKeyProd's record-wrapped-result, activate, mark-preparation-expired, resolve-provider-outcome, mark-cleanup-required, record-cleanup-observation, acknowledge-cleanup, record-recovered-result, request/finalize-abandon, revoke, and internal activate-recovered key functions lock only the exact key followed by provider operation/event rows; none acquires session/reference/head/attempt afterwards. DurableObjectCustody's arm-put, record-not-armed, record-put-result, resolve-put-outcome, mark-verified, mark-cleanup-required, record-delete-acknowledged, record-absence-confirmed and record-quarantined functions lock only object rows; their attempt checks are ordinary MVCC SELECT, not FOR UPDATE. Their immutable exact resource joins/CAS remain unchanged. They may run as separate provider-role transactions but must not be chained in one transaction into a later session/source-locking callable. Read-only context readers take no row locks. R6 read-next/complete/finalize lock publication -> cleanup/resource/event rows and never acquire session/reference/head/attempt afterwards; preserve that order and separate R6 transactions. No new purge permission follows from these exclusions. The forward migration and function manifest include the three existing R2 mutation body replacements named above (prepare-key, begin-object, operational terminator) and the old semantic-writer retained rejection; historical migrations remain untouched.

## CP05 — exact predicate changes and result residue

For `complete_raw_export_source_ingress_claim` (the base completion called by the 42-input `complete_raw_export_source_ingress_claim_with_r2_handoff` wrapper), R3, R4 and R5, the legacy branch still uses its exact current snapshot + `SubjectRawBiometricExport` consent resolver. The retained branch requires snapshot kind SourceRetention, purpose SourceRetention, CP03 helper TRUE, and all existing snapshot/reservation equality, class/controller/scope, expiry, CAS/fence, object/key integrity checks. Fresh R3/R4/R5 additionally require exact attempt.R2TerminalIntentCode IS NULL before stage/commit/publication CAS; a persisted terminal cleanup intent belongs to the broker companion's durable terminalizer and cannot advance staging. Existing successful historic replay is not a new stage authorization. Claim re-entry/complete must not re-arm an intent-bearing attempt: until terminal settlement exposes its finalized exact terminal code it returns the existing busy/no-body classification. It does not call B2 export consent before Completed. The branch is chosen by persisted discriminator, never by missing B2 consent or by trying legacy then retained.

| Boundary | Fresh denial | Durable residue | Public A3 projection |
| --- | --- | --- | --- |
| Begin/complete before B/R1 commit | Existing `SOURCE_RETENTION_NOT_AUTHORIZED` | No new reservation/body writer; existing replay result not erased | O13, 403 outcome-only, P3 |
| R3 after VerifiedCompleted ciphertext | Existing `SourceRetentionNotAuthorized` | Existing Reserved/head, complete verified provisional object, key context unchanged; no staging mutation | O21 `RAW_EXPORT_SOURCE_RESUME_PENDING`, 202 outcome-only |
| R4 after Staged | Same | Staged state/evidence unchanged; no publication insert | O21; no body resend |
| R5 after Committed | Same | Committed publication remains; no Available/head/locator transition | O21; no body resend |
| C1 freeze/seal | Existing AuthorityInvalid/SourceUnavailable/Expired precedence | No provider/read/binding/seal side effect beyond already-owned work | Existing C1 mapping, not an R27 outcome |
| C3 before Streaming | FALSE -> existing Ineligible | No Streaming event, reader.OpenCount=0 | Existing C3 mapping, not O13 |

O21 describes unresolved complete ciphertext, NOT a promise that revoked authority will recover. The bounded continuation worker re-enters the exact checkpoint. It does not auto-renew consent/permit, replace evidence, restart R2, upload body again or relabel authority loss as CONTENT_COMMITMENT_MISMATCH. Existing provisional/key expiry cleanup can execute only through its existing `SourceExpired` cleanup guards. A3 adds no Available-source purge or legal-hold enforcement; those remain A4/lifecycle-gated. HTTP cancellation never becomes deletion authority.

No post-R1 SourceRetentionNotAuthorized is mechanically uppercased into frozen P3 O13. No new R2TerminalOutcomeCode is invented for it. Existing terminal outcomes retain their three-code allowlist; no terminal attempt row is forged merely to make the HTTP response durable.

## CP06 — C1/C3 independent export compatibility

Retained source authority is necessary source eligibility, never sufficient read permission. `EfRawExportAuthorizationRepository` remains unchanged. C1 keeps the separately authorized job, permit, Completed-derived export authorization, exact class selection and current B2 consent for the source's actual policy. Do not replace those checks with CP03. The same underlying external consent artifact may produce the later B2 event under its existing authorized recorder path; that is not another subject action.

C1 current authority resolver now validates retained source references via CP03 while preserving return shape, so existing snapshot/policy/revision equality and binding fingerprint inputs remain valid. C1 freeze and seal add CP04 lock acquisition/revalidation; no new public outcome or permit is added. Retained policy/version and B2 current consent policy/version must match the already-selected exact source policy; mismatched export policy must fail instead of translating it implicitly.

C3 changes only the source-purpose atom within `raw_export_c3_current_authority_eligible`: derive exact snapshot discriminator by the source binding's persisted AuthoritySnapshotId. LegacyExport still requires `authority.ApprovedPurpose=identity.PurposeCode`. SourceRetention requires `authority.ApprovedPurpose='SourceRetention'` AND CP03 TRUE. After that branch, ALL existing job/decision/permit/class-policy/grant/latest-fulfillment/Completed/principal/recipient/B2 consent checks remain mandatory. In particular the independent B2 `consent.PurposeCode=identity.PurposeCode` check is not changed. This is a tagged source check under an AND with export authority, not an OR accepting either permit class.

The forward migration also replaces the exact installed outer callable `tagekyc.raw_export_begin_recipient_package_delivery_stream(p_recipient uuid,p_delivery uuid,p_api_key uuid,p_principal uuid,p_correlation bytea)`, preserving its 21-column return shape, owner/search_path/EXECUTE and existing authenticated-call context. Do not patch only its boolean helper. For a retained package the owned delivery probe derives the immutable job/source session set without disclosure; CP04 session/reference prefix is acquired before recipient-key/package/delivery row locks. Acquire the helper's complete existing lock set before sampling time: rule-set publication, principal/policy grant, policy lifecycle, non-ConsentArtifact fulfillment keys sorted RequirementType, each source's exact B2 authority-scope key, and each existing B2 consent-scope lock using the existing raw_export_consent_scope_hash/raw_export_consent_lock_key functions. Keep source ordinals and existing per-family lock order. Revalidate owned delivery/package/key/job/source/session/reference membership under these locks; a mismatch uses existing Ineligible, never a replacement target. The helper's later repeated acquisitions must be reentrant, not the first blocking acquisition of a new lock.

Only then assign one `now_utc := pg_catalog.clock_timestamp()`. Pass that exact value as `p_evaluated_at_utc` to `raw_export_c3_current_authority_eligible`; use the same value for outer AuthorizationExpiresAtUtc, recipient-key ValidFromUtc/ValidUntilUtc, current stream lease checks, expiry event bytes/timestamp and Streaming CAS/start/lease event bytes. The helper must not resample independently, and outer code must not reuse a pre-prefix/pre-helper timestamp. Time-based predicates execute after all required locks, not during the probe. Preserve existing terminal/replay precedence, expiry materialization and committed event identities; delivery authorization expiry still materializes its existing Expired event, while key/retention ineligibility creates no Streaming event. Completed/in-progress replay cannot open a second provider reader. Legacy-only packages retain their existing branch semantics; no export purpose, recipient authority, credential or disclosure ceiling is broadened. This is an outer-clock correction, not a claim about an additional deadlock.

## CP07 — ACL and mutation ownership

All new helpers are `SECURITY DEFINER SET search_path=pg_catalog`, owner `tagekyc_raw_export_deployer`. Revoke PUBLIC EXECUTE at creation in the same migration transaction. CP03 append EXECUTE goes only to `tagekyc_raw_export_claim_broker`; not runtime. CP03 boolean helper has no external grants (deployer-owned resolver/checkpoints call it). Existing current resolver/checkpoint function grants are preserved exactly, not expanded. New table columns do not grant direct table privileges. API never gains broker login/password or retained begin EXECUTE.

E01 tables own reference/permit transitions. CP owns only snapshot linkage, tagged current resolver, checkpoint predicate/locking changes and continuation metadata. Existing publication, staged fingerprint, commit/available evidence encodings remain unchanged because they already bind the immutable snapshot ID/revision; the insertion guard transitively binds that snapshot to the new authority tuple. Do not silently change canonical fingerprint labels or hash formats.

## CP08/CP09 — durable continuation metadata, not a second body-ingress API

New SQL reader `tagekyc.raw_export_read_retained_source_continuation(p_source_artifact_id uuid)` returns exactly:

```text
SourceArtifactId uuid; CustodyPrincipalId uuid; ClientApplicationId uuid;
VerificationSessionId uuid; RuntimeBindingId uuid;
RetentionAuthorityId uuid; RetentionAuthorityRevision bigint;
CustodyState text; ReservationRevision bigint; Fence bigint;
AttemptId uuid; EncryptionAttemptRevision bigint; AttemptKeyReservationId uuid;
ObjectCustodyId uuid?; ObjectState text?; ObjectStateRevision bigint?;
SourcePublicationId uuid?; PublicationRevision bigint?; PublicationState text?;
CleanupDisposition text?;
R2TerminalIntentCode text?; R2TerminalIntentDisposition text?;
R2TerminalIntentAtUtc timestamptz?;
R2TerminationDisposition text?; R2TerminatedAtUtc timestamptz?;
R2TerminalOutcomeCode text?
```

The return shape is exactly 26 columns, in the order above. One SELECT joins source reservation -> its exact retained snapshot -> claim -> current head -> current attempt; LEFT JOIN exact key by attempt.AttemptKeyReservationId and exact object/publication. AttemptKeyReservationId is returned from the attempt, not the optional key. Fresh R1/RE01 allocates this ID before key provisioning: a missing key row is valid unprepared state and MUST return the complete pre-key row. If present, key AttemptId/SourceArtifactId/AttemptKeyReservationId must match the exact attempt/source; a contradictory present key returns zero rows. Object cannot be present without its matching key. Missing/nonretained/contradictory immutable required joins return zero rows; no latest Client credential query. Object tuple is all NULL or all non-NULL; publication tuple is all NULL or all non-NULL. Head fence/current attempt must equal attempt; object must match attempt/key/source and stored identity. No raw bytes, secret, token, key envelope, provider locator or external consent reference is returned.

All six terminal fields are copied from the same exact persisted attempt, never synthesized: intent triple is all NULL or all non-NULL; operational disposition/time is both NULL or both non-NULL, disposition exactly Terminated|TerminatedBeforeStart; final semantic code is NULL or one of O06/O19/O20's exact broker code literals. For retained rows a non-NULL final code requires a complete intent triple, complete operational pair, finalcode=intentcode, operationaldisposition=intentdisposition, and R2TerminatedAtUtc>=R2TerminalIntentAtUtc except TI01's explicit expired-transient O20 case where pre-existing operational termination predates new intent. No equality between operational and semantic timestamps is invented. A non-NULL operational pair with NULL finalcode and NULL intent is valid interrupted/transient R2 state, not final RECAPTURE_REQUIRED. NPS01's exact TerminatedBeforeStart/time witness with its expired-owner-lease and complete no-provider-evidence predicate is another valid operationally settled state; it requires no key row and creates no fake key/provider receipt. Invalid combinations return zero rows and fail readiness/proof rather than inventing a code. TI02's exact matching-code replay and resource-settlement predicates remain authoritative.

Readback precedence: finalized semantic code plus TI02-proven settlement -> replay that exact durable outcome; non-NULL intent without finalized code/settlement -> pending terminal cleanup, never stage or report that intent as final; operational termination without intent/final -> only broker RE01 eligible same-owner retry or, when the original exact horizon H has expired, TI01 O20 finalization; otherwise normal fresh/staged/committed continuation from actual state/revisions. CP08 is metadata, not an authority decision; every mutation revalidates CP04/CP05 and existing CAS in its own transaction. Appending these fields does not add another final-code column or another terminalizer.

`tagekyc.raw_export_list_retained_source_continuations(p_after_source_artifact_id uuid,p_limit integer)` returns TABLE(`SourceArtifactId uuid`). NULL cursor starts the scan; otherwise strict UUID order `SourceArtifactId > cursor`. Limit must be 1..100 or SQLSTATE P0001 / `A3_CONTINUATION_SCAN_ARGUMENT_INVALID`. Include exact retained-linked heads in Reserved or Staged, plus Available publications with CleanupDisposition Pending. Also include pending intent and current operationally-terminated/no-intent/no-final attempts once original horizon H<=one DB clock_timestamp sampled for this scan. H is exactly LEAST(binding.ExecutionExpiresAtUtc, reservation.EffectivePlaintextRetentionExpiresAtUtc, reservation.AbsoluteSourceExpiresAtUtc, original snapshot.ValidUntilUtc), the broker RE01/TI01 horizon, not a renewed config or guessed budget. Workers must recheck H under mutation locks. A positive but insufficient margin is not expiry. Retain intent work until TI02 finalcode/disposition equality AND one exact settlement branch: (a) key PreparationDisposition Revoked|ReservationAbandoned AND its object predicate (Terminated requires matching Deleted|Quarantined; TerminatedBeforeStart requires matching NoObjectEstablished or no object with terminal key), or (b) the complete NPS01 TerminatedBeforeStart/expired-owner-lease/no-provider-evidence witness defined below. Never exclude merely because operational termination or intent is non-NULL. Fully finalized and settled terminal work is excluded even if custody head remains Reserved; available PendingR6 remains independently eligible. Contradictory immutable links are not executable work. Order UUID ascending, LIMIT p_limit. This is a bounded census, not a lease/exactly-once queue. Restoring an unconditional terminated-attempt filter or dropping expired no-intent rows must make restart finalization proof RED.

CP09 uses the same optional-key rule as CP08: a fresh valid Reserved R1/RE01 row with neither key nor object remains discoverable. Discovery/readback does not create a key. Missing key by itself never means settled. The only additional no-provider settlement alternative is NPS01's exact persisted TerminatedBeforeStart/time witness, expired original attempt OwnershipLeaseExpiresAtUtc, and the complete absence predicate rechecked under the common session prefix and exact attempt/head/reservation locks: no key reservation, no preparation event, no provider operation and no provisional object by any of that attempt's allocated attempt/key/object identities. This is the broker companion's NPS01 predicate, not merely a LEFT JOIN yielding NULL. TI02 may finalize O20 against that witness; until finalcode/intent equality is persisted CP09 retains the work. Without the witness, CP09 retains no-key rows for fenced NPS01 evaluation after owner-lease expiry; while owner lease is live it permits no operational termination, re-entry or semantic finalization. Competing key preparation shares the same prefix: key/provider evidence committed first makes NPS01 reject; NPS01 committed first makes preparation reject. No NPS01-created key, wrap/unwrap, provider call, fabricated evidence receipt, principal or original-horizon extension is allowed.

NPS01 is the exact new callable `raw_export_terminate_retained_r2_before_provider_start(uuid,uuid,bigint,bigint)`, returning Outcome text/R2TerminationDisposition text/R2TerminatedAtUtc timestamptz with broker-pinned closed outcomes. It is deployer SD/pg_catalog, PUBLIC revoked, EXECUTE only to existing reconciler role; broker B-C may invoke it only internally under its SD owner. Add this one signature/owner/grant to the forward manifest; no public HTTP route, new table/column or runtime/claim-broker EXECUTE. CP08 remains exactly 26 fields: its existing operational pair represents the durable witness, and NPS01/TI02/RE01 recheck the full predicate instead of trusting the metadata reader as authority.

Both functions: deployer-owned SD/pg_catalog. CP08 EXECUTE grants exactly `tagekyc_raw_export_custody_encryptor`, `tagekyc_raw_export_reconciler`, `tagekyc_raw_export_lifecycle`; CP09 grants exactly reconciler and lifecycle. Revoke PUBLIC, runtime, authenticator, operator and standalone claim-broker callers. No new login. API pipeline and bounded continuation worker consume the existing per-role DB/provider scopes, these internal metadata readers and existing per-stage actor commands; the actor is frozen CustodyPrincipalId from readback, never worker/service identity. R3/R4/R5 remain reconciler-only, R6 complete/finalize lifecycle-only; do not grant those functions to runtime. They reread after each successful stage to obtain actual CAS revisions, never assume `+1` from an HTTP result. Parallel workers are arbitrated by existing stage CAS/fence/provider idempotency; no new owner/lease table. Incomplete R2 object goes only to existing object/key reconciliation and expiry cleanup; a scanner cannot reconstruct plaintext or invoke the body encryptor.

## CP10 — migration/Down and discriminating proofs

Forward migration creates E01 objects before snapshot FKs/functions and updates EF model/Designer. Install helper before its consumers; patch canonical readiness function manifest in lockstep, checking exact owner/search_path/EXECUTE. It must not touch historical migrations or rewrite old source authority/events. Validate tagged CHECK and cross-table insert guards before admitting new retained work.

Down first blocks if ANY SourceRetention snapshot, retained-linked reservation/publication, capability or binding exists, including terminal rows: `A3_RETENTION_DOWN_POPULATED`. No fixture exception by string/name; tests remove their isolated database, not bypass Down. If empty, restore pinned predecessor consumer bodies/ACL before dropping helpers, indexes/FKs/columns and then E01 objects in reverse dependency order. Body drift aborts Down atomically; never replace a newer unrelated body. Existing LegacyExport bytes and canonical evidence hashes survive Up/Down/Up.

### CP10.1 — exact SQL representation and body-drift comparison

The only migration pair remains `20260913120000_Tip88C1C6BA3RetainedIngressComposition`. Every multiline SQL payload supplied to `migrationBuilder.Sql` by its Up or Down MUST pass through exactly this ordinal transform, once at the execution boundary:

```csharp
sql.Replace("\r\n", "\n", StringComparison.Ordinal)
   .Replace("\n", "\r\n", StringComparison.Ordinal)
```

This is a required implementation setting, not an optional formatting convention. It applies uniformly to additive DDL, every forward CREATE/REPLACE body, manifest/owner/ACL/search_path installation, Down guards and predecessor body restoration. No helper/call site may bypass it by passing a raw C# multiline literal directly. Single-line payloads are unaffected. No trim, indentation normalization, SQL parsing/reformatting, Unicode normalization, or other byte transform is permitted. No `.gitattributes`, historical migration, runtime validator or shared normalization framework is added or edited by this correction. The actual E3 predecessor migration `20260724015546_Tip88B1E3ResolverReadBoundary.cs` uses this transform at both Up and Down execution boundaries; its historical two-MD5 compatibility exception is NOT reused as an A3 hash rule.

The migration's predecessor/current body-drift guards compare exact `pg_proc.prosrc` bodies using only `CRLF -> LF` canonicalization, separately from the CRLF execution representation above:

```text
CanonicalBodyBytes(body) = UTF8(body.Replace(CRLF, LF, Ordinal))
CanonicalBodyHash(body)  = lowercase hexadecimal SHA-256(CanonicalBodyBytes(body))
```

For each exact function signature and expected lifecycle state, pin exactly one canonical expected body hash derived from that state's reviewed body literal, not from the database body under inspection. The predecessor expectation is derived from the exact consumed predecessor body; the post-Up expectation is derived from the candidate A3 body. Record those per-signature expectations with the implementation function manifest before executing the migration. Compare actual canonical body bytes/hash to that single state's expected bytes/hash; do not accept a set of old-or-new representation hashes, compare only hash prefixes, or overwrite the expected value using live database content. The expected literals must contain no lone CR. A lone CR in an actual body remains in CanonicalBodyBytes and is a mismatch; adding/removing whitespace, an SQL token, indentation or any non-EOL byte likewise remains a mismatch. Only replacement of CRLF by LF is representation-equivalent. Apply these same rules to all forward predecessor checks and all Down current-body checks/restoration surfaces; Down must validate every expected current body before replacing any of them and abort atomically on drift.

Representation does not relax CP10's population guard or change SQL semantics, retained authority, lock order, ACL, function signatures or readiness. The restored predecessor body is executed in canonical CRLF form; its comparison identity remains the one LF-canonical expected hash. Up/Down/Up must therefore converge without rewriting historical source files.

The exact proof owner is the already-inventoried `tests/TagEkyc.IntegrationTests/Tip88C1C6BA3MigrationTests.cs`, method `A3_RetentionMigration_CheckoutRepresentationsConverge`. Build/run the actual migration independently from (A) a normal Windows/core.autocrlf working checkout and (B) the exact `git checkout-index` LF snapshot of the same candidate. Verify equal non-EOL source content and assert that the migration source really has different CRLF/LF representations; two accidentally identical copies do not qualify. Isolate build/obj/output and PostgreSQL databases so no assembly or applied migration from A can satisfy B. Capture actual installed `pg_proc.prosrc` bytes through an independent observer connection, not a hash recomputed only from test inputs. Compare the complete A3 function manifest by exact signatures, byte-identical installed bodies, canonical body hashes, owner, ACL, search_path, result signatures and the retained DDL catalogue; assert the complete manifest counts, not just an intersection.

Seed the pre-A3 databases with equivalent predecessor bodies in CRLF and LF respectively; their forward predecessor guards must both accept. On empty retained state in each database, Down must succeed despite that EOL-only predecessor representation difference and restore the same canonical predecessor bodies from the independently compiled restoration literals. Also change a current A3 body only from CRLF to LF: Down's current-body guard must accept that equivalent representation, without ever treating a predecessor body as the expected current A3 body. Up/Down/Up then returns to the identical post-Up catalogue. Separately alter one current A3 function body by a real non-EOL byte while preserving its signature: Down must reject and the observer must see no partial rollback/restoration. A lone-CR-only mutation is also rejected. Removing the execution transform from one actual CREATE/REPLACE or one restoration payload must make the cross-representation comparison RED; broadening drift normalization to trim/ignore a non-EOL mutation must make the drift-negative control RED. Positive controls execute the unchanged real Up/Down/Up chain; neither a test-local serializer nor inspection of source text alone discharges this proof. These are future executable obligations, not tests run by drafting.

| Proof | Positive / negative execution and required RED mutation |
| --- | --- |
| `A3_RetentionSnapshot_InsertLineageIsClosed` | Real permitted append succeeds; mutate exactly one Principal, Client, session, binding, permit revision, consent binding, policy or class at INSERT -> named lineage constraint/trigger; removing corresponding predicate RED |
| `A3_RetentionSnapshot_ExactPermitRevision` | Actual issued permit revision1 joins exactly; injected revision2/missing revision is rejected; catalogue asserts bigint linkage and no narrowing cast; actual artifact schema version stays1 |
| `A3_RetentionCheckpoint_WithdrawalSerializesWithR1R3R4R5` | Two real PG connections block in both orders at each checkpoint; observer sees only committed state; remove shared reference lock -> intended race RED |
| `A3_ExportCheckpoint_ActualB2WithdrawalSessionOrder` | Real C1 freeze, C1 seal and C3 Streaming admission each race the actual B2 withdrawal callable in both acquisition orders on retained sources. Use barriers/pg_locks to prove the loser waits at the common actual session row, not a test-local advisory surrogate; withdrawal-first denies admission with no provider read, admission-first completes its existing atomic boundary before withdrawal. Include a multi-session job with reversed input order; rows lock sorted UUID. Remove the session prefix while retaining shared reference locks -> intended deadlock/order proof RED, not a timeout counted as PASS |
| `A3_C3_OuterAdmissionClockAfterAllLocks` | Three independent real outer begin-stream cases hold its owned session row while (a) delivery AuthorizationExpiresAtUtc, (b) recipient-key ValidUntilUtc, or (c) retained-source horizon crosses; all other horizons remain live. Release lock after DB clock passes target. Case(a) exact existing Expired event/return; (b)/(c) Ineligible; all cases zero Streaming events and provider reader.OpenCount=0. Each has a sibling positive control releasing before the target horizon and producing one Started/Streaming/provider admission. Move outer now_utc before prefix while keeping helper fresh -> intended outer authorization/key negatives RED; stale shared-time mutation also makes retention case RED. Assert the production outer function actually waited, not test-local sleep alone |
| `A3_R2_KeyPreparationUsesPostLockLeaseClock` | Hold actual retained session prefix across original owner/reservation lease expiry, invoke real prepare-key from second connection, then release: HeadNotReserved and zero key/preparation/provider-operation insert. Live-lease control creates expected row. Restore statement_timestamp/pre-prefix clock -> RED |
| `A3_RetentionReplay_CommonSessionPrefix` | Actual public same-source replay races each TI01, TI02, RE01, R3, R4 and R5 in both orders; exact replay/finalization or existing conflict/busy result, no deadlock, duplicate event or body read. Repeat key preparation, provisional-object begin and already-terminated operational-terminator replay against broker replay. Assert both callers entered production SQL and the second blocked at the same session row; remove one named callable's prefix -> its order control RED |
| `A3_RetentionCheckpoint_LatestReferenceNotOldEffective` | Same reference updated/withdrawn while old event remains effective: every fresh checkpoint denies; prefilter-latest mutation RED |
| `A3_RetentionCheckpoint_CompleteCiphertextDenialResidue` | Seed via real preceding stages, withdraw then invoke each actual R3/R4/R5; exact table snapshots unchanged at failing stage, O21, no new terminal/Available/bodyread; O13/terminalization mutation RED |
| `A3_RetentionCheckpoint_CompletedIsNotRetentionRevocation` | Retention issued before Completed remains valid at exact later source check until real horizon/revocation; adding NonCompleted to all resolvers RED |
| `A3_ExportRequiresIndependentAuthorityForRetainedSource` | Retention-only source cannot C1 seal/C3 stream; valid post-Completed export decision+permit+B2 current consent succeeds; remove any export predicate -> negative control RED |
| `A3_ExportWithdrawal_BlocksBeforeProviderRead` | Real retained source/export package positive control, then underlying reference withdrawal; C3 Ineligible, reader0, no Streaming append; ignore retention branch mutation RED |
| `A3_Continuation_ReadsFrozenActorAndActualRevisions` | Restart without caller credential uses durable principal and exact returned revisions; replace with current Client key's principal or assumed revision -> RED |
| `A3_Continuation_PreKeyReadbackAndClosed26Fields` | Real R1 and RE01 before key preparation return exactly 26 typed columns and the allocated attempt key ID with no key row; after preparation return the same lineage. Inner-key-join mutation RED. Seed each legal empty/intent-pending/operational-only/finalized shape through its owning SQL; NULL finalcode is never replaced by intentcode. Tamper one exact key join or finalcode/disposition equality -> invalid readback; omission/reordering of each of the three final operational/semantic columns RED |
| `A3_Continuation_DuplicateWorkersDoNotRestartR2` | Two real workers compete from complete provisional/staged/committed points, one durable stage event/state transition, zero body reads/re-encryption, provider idempotency preserved; duplicate mutation RED |
| `A3_Continuation_TerminalIntentSurvivesRestart` | Broker companion's three-code terminal intent exists on exact attempt; crash before cleanup, after operational termination, and before semantic code each remain discoverable through CP09 and readable through CP08; worker finishes exact code once, never stages/re-encrypts. Removing intent fields, filtering all terminated attempts, or advancing R3 with intent must turn RED |
| `A3_Continuation_ExpiredTransientWithoutIntent` | Real operationally terminated current attempt has no intent/finalcode; before original H remains nonterminal and may use only eligible RE01; after H it is discovered, TI01 creates only authorized O20 intent and TI02 finalizes once after exact settlement. Observer verifies old operational timestamp remains unchanged; duplicate workers create no second finalization. Remove no-intent expiry scan branch or derive H from a later config -> RED. Fully settled final outcome disappears from scan while available PendingR6 remains |
| `A3_Continuation_NoProviderStartSettlementIsFenced` | Real committed R1 with lost broker response has no key/object/provider evidence: scanner discovers it; NPS01 before owner-lease expiry is LeaseLive, after expiry writes only exact TerminatedBeforeStart/time; RE01 before immutable H can append eligible fresh attempt, or after H TI01/TI02 produce one O20 without fake key/receipt. Race NPS01 with actual key preparation in both orders; preparation-first returns ProviderEvidencePresent, NPS01-first rejects preparation. Inject any one allocated-identity key/event/provider-operation/object row -> NPS01 rejects. Missing-key-alone shortcut, omitted evidence predicate, early lease clock, scanner exclusion or provider call during NPS01 -> RED |
| `A3_RetentionMigration_LegacyRoundtripAndPopulatedDown` | Up/Down/Up empty retained state preserves legacy fixture bytes; one retained terminal row makes Down fail atomically; bypass mutation RED |
| `A3_RetentionMigration_CheckoutRepresentationsConverge` | CP10.1: independently compiled Windows CRLF checkout and exact checkout-index LF snapshot install identical observed prosrc/owner/ACL/search_path/result/DDL catalogues with full counts; EOL-only predecessor representation is accepted, lone CR/non-EOL body drift rejected atomically, Up/Down/Up converges. Remove one execution normalization or weaken one drift predicate -> its intended RED |


## CP11 — capability/binding persistence and exact R20 amendment

Append `Guid? ConsentBindingId = null` to existing `CaptureCapabilityRequest` after ExpectedRevision. This is the E01 local provenance binding returned to the authenticated Client, not an Agent field or new consent action. Issue with present nonempty ConsentBindingId means retained authority requested; Issue with the member absent remains non-retained. Replace requires ConsentBindingId absent and preserves old lineage. Empty UUID and an explicitly-null consentBindingId are invalid JSON shapes, not aliases for omission. No public PrincipalId, policy/profile, permit, raw-class selection or source horizon is added.

Amend the actual API grammar in `CaptureRuntimeExecutionEndpoints.cs:35–38` together with the DTO. After existing authentication/route/Idempotency-Key/closed JSON checks, validate both exact property-name set and count (not count alone):

| Public operation | Exact case-sensitive JSON property set | Count / value constraints |
| --- | --- | --- |
| Non-retained Issue | `action` | 1; action="Issue"; all other members absent |
| Retained Issue | `action`, `consentBindingId` | 2; action="Issue"; consentBindingId nonempty UUID accepted by existing UUID JSON representation; no CurrentCapabilityId/ExpectedRevision member, even NULL |
| Replace | `action`, `currentCapabilityId`, `expectedRevision` | 3; action="Replace"; nonempty current UUID; positive Int64 expectedRevision; consentBindingId forbidden even NULL |

Unknown, duplicate, wrongly-cased or explicit-null required members return existing `400 REQUEST_INVALID`; current16KiB body limit is unchanged. The old Issue count==1 atom must not remain as a second guard rejecting valid retained Issue. Service/SQL typed calls mirror value grammar: Issue CurrentCapabilityId/ExpectedRevision NULL and optional nonempty binding; Replace valid current/revision and NULL binding. The API alone distinguishes absent from explicit JSON null; internal NULL is only the already-validated absence representation. Add actual endpoint tests for allthree accepted sets and every forbidden/null/extra-member case, including a two-property Issue with the wrong second member. Canonical A1 OM/API test expectations must be updated together; the DTO constructor default is not the wire grammar.

Application requires nonempty authenticated actor.PrincipalId and ClientApplicationId. Request fingerprint still uses existing `CaptureRuntimeHttpFingerprint.Compute`, exact R20a/R20b route/body/idempotency inputs. Actor partition becomes exactly 32 bytes: RFC/network-order ClientApplicationId bytes16 followed by RFC/network-order PrincipalId bytes16. No Guid .NET little-endian default. Exact body binds ConsentBindingId; generated permit/snapshot IDs, current profile/time/pepper/secret are excluded. Same logical retry with a rotated API key but same principal is identical; P1->P2 is not. R21 fingerprint/CRT1/body stays unchanged because it consumes server capability lineage, not a new principal wire field.

Replace the existing 12-input `capture_runtime_issue_or_replace_capability` with the following 15-input successor; revoke/drop the old callable in the same forward migration so it cannot create unbound new rows:

```sql
tagekyc.capture_runtime_issue_or_replace_capability(
 p_client_application_id uuid,p_verification_session_id uuid,p_action text,
 p_current_capability_id uuid,p_expected_revision bigint,p_idempotency_key uuid,
 p_new_capability_id uuid,p_key_lookup_prefix text,p_secret_digest bytea,
 p_verifier_pepper_version integer,p_request_fingerprint bytea,p_now timestamptz,
 p_principal_id uuid,p_consent_binding_id uuid,p_retention_profile jsonb)
RETURNS TABLE(result_code text,capture_capability_id uuid,secret_available boolean,
 expires_at_utc timestamptz,state text,revision bigint)
```

The internal JSON profile has exactly12 required members: PolicyId, PolicyVersion, RawClasses, ControllerIdentity, StableDataScopeId, RetentionPolicyId, RetentionPolicyVersion, RetentionClass, RevocationPolicyId, PurgePolicyId, LegalHoldPolicyId, MaximumRetentionSeconds. Types/bounds are E01 §5, PascalCase literal member names, no extra/duplicate/null members. The gateway serializes the closed typed server configuration record excluding ClientApplicationId, using System.Text.Json with explicit property names; SQL verifies object key set/count/types before casting. This is internal trusted configuration transport, not a signing/canonicalization scheme and not a public extension bag. Profile must be NULL for non-retained Issue and for Replace. Retained Issue requires profile selected by exact authenticated Client and no default entry.

Inside the existing one R20 B: owned-session/lock/argument checks -> current principal equality for Replace -> existing idempotent replay/conflict classification -> E01 Recorder authority shared lock before reference, current binding/evidence/policy locks -> call E01 `raw_source_issue_retention_authority` with its exact18 inputs, using E01 §4.1 post-all-lock admission clock and direct current-registry predicate -> insert new capability/operation/event -> commit. Do not call issuer before idempotent classification. Replay never creates another permit/class row; failed capability issuance rolls the new permit back. Existing expiry-on-denied materialization still commits before denial mapping. Existing SQL public result-code set and secret-once replay remain unchanged. No early secret is returned on a denial. Existing 5-minute capability horizon is additionally capped by retained permit expiry; never extended. Fresh retained capability ExpiresAt must still exceed the issuer's post-lock admission_now; an old p_now cannot authorize an already-expired returned capability.

Application `CaptureCapabilityPersistenceRequest` appends PrincipalId and `RawSourceRetentionProfile?` while request.ConsentBindingId carries the reference. Gateway sets transaction-local actor from PrincipalId, passes15 exact parameters, reads unchanged six-column result, commits expiry-on-denied as before. R20 successor keeps deployer SD/pg_catalog and existing EXECUTE grants to `tagekyc_capture_runtime_application`, `tagekyc_runtime`; no new DB capability.

Add to BOTH `capture_capabilities` and `capture_execution_bindings`:

| Column | Type/nullability/default | Constraint |
| --- | --- | --- |
| AuthorityMode | varchar(24) NOT NULL DEFAULT 'HistoricalNonRetained' | exact HistoricalNonRetained, NonRetained, SourceRetention |
| PrincipalId | uuid NULL, no default | nonzero when present |
| RetentionAuthorityId | uuid NULL, no default | nonzero when present |
| RetentionAuthorityRevision | bigint NULL, no default | positive when present |
| ConsentBindingId | uuid NULL, no default | nonzero when present |

For each table, new named `ck_a3_<capability|binding>_authority_shape` CHECK permits exactly: HistoricalNonRetained => all4 nullable columns NULL; NonRetained => PrincipalId non-NULL and other3 NULL; SourceRetention => all4 non-NULL. Add FK `(RetentionAuthorityId,RetentionAuthorityRevision)` to E01 permit PK and ConsentBindingId FK to E01 binding, DELETE RESTRICT. Add indexes on `(RetentionAuthorityId,RetentionAuthorityRevision)` and ConsentBindingId. Keep existing capability/session/binding unique/FK/state domains unchanged.

New insert/mutation guard `enforce_a3_capture_authority_lineage()` RETURNS trigger, deployer SD/pg_catalog/no external EXECUTE: new rows cannot use HistoricalNonRetained; validate capability P/Client/session equals exact consent binding and permit; binding matches ALL five new columns of its exact capability with `IS NOT DISTINCT FROM`; Replace successor matches predecessor's allfive; UPDATE cannot modify any of allfive. Mismatch raises `A3_CAPTURE_AUTHORITY_LINEAGE_MISMATCH`. Existing A1 mutable lifecycle fields remain writable only through existing guards. Install BEFORE INSERT OR UPDATE triggers `tr_a3_capability_authority_lineage`, `tr_a3_binding_authority_lineage` on respective tables, plus named FK checks. Negative proofs must mutate INSERT independently, not only updater branches.

HistoricalNonRetained is only migration classification of bytes that previously had no retention authority, not a production legacy fallback. Before adding defaults, forward migration refuses a live ActiveUnbound/Bound capability whose ExpiresAtUtc is future or a binding whose ExecutionExpiresAtUtc is future: `A3_CAPTURE_LINEAGE_CUTOVER_ACTIVE`. It must not derive historic principal from audit/current keys. Expired/terminal history is preserved under the explicit historical tag. A3 R27 reader rejects NonRetained and HistoricalNonRetained with O02; non-retained verification remains independently available.

R21 `capture_runtime_bind_capability` retains exact9-input signature/result and copies allfive columns from its locked capability on the existing binding INSERT. For retained capability it validates current E01 reference/permit under shared lock before binding; no new wire fields. R22 returns stored binding without reauthorization or mutation. R24/R25 read exact new lineage for audit but retain their current authority-neutral append semantics. R26 cancellation keeps its historical/A1 distinctions; new columns are immutable and its per-family operation/event semantics do not change.

Canonical A1 OM R20a/R20b signature/DTO/fingerprint/persistence and R21 binding rows, function manifest, `CaptureRuntimeStartupCatalogue` signatures/ACL, and Parent consumed-source freeze must change in lockstep. An A3 overlay alone is not sufficient. Add named proofs `A3_R20_PrincipalPartitionAndSecretOnceReplay`, `A3_R20_ReplaceCannotTransferPrincipal`, `A3_CapabilityBinding_InsertLineageMismatch`, `A3_R20_ReplayDoesNotMintPermit`, `A3_Cutover_RefusesActiveUnboundHistory` with actual SQL/HTTP positives and single-atom mutations RED; no new test-local authority shortcuts.

## CP12 — concrete source/mutation paths

Existing product files consumed and modified by CP (all server-relative):

- `src/TagEkyc.Contracts/CaptureRuntime/CaptureRuntimeExecutionContracts.cs`
- `src/TagEkyc.Api/CaptureRuntimeExecutionEndpoints.cs`
- `src/TagEkyc.Application/CaptureRuntime/CaptureRuntimeExecutionApplicationService.cs`
- `src/TagEkyc.Application/Ports/CaptureRuntimePorts.cs`
- `src/TagEkyc.Infrastructure/CaptureRuntime/CaptureRuntimeExecutionPersistenceBoundary.cs`
- `src/TagEkyc.Infrastructure/CaptureRuntime/CaptureRuntimeStartupCatalogue.cs`
- `src/TagEkyc.Infrastructure/Persistence/Entities/CaptureRuntimeEntities.cs`
- `src/TagEkyc.Infrastructure/Persistence/Entities/RawExportAuthoritySnapshotRow.cs`
- `src/TagEkyc.Infrastructure/Persistence/Configurations/CaptureRuntimeEntityConfigurations.cs`
- `src/TagEkyc.Infrastructure/Persistence/RawExportAuthoritySnapshotReadinessValidator.cs`
- `src/TagEkyc.Infrastructure/Persistence/TagEkycDbContext.cs`
- `src/TagEkyc.Infrastructure/Persistence/Migrations/TagEkycDbContextModelSnapshot.cs`

New CP files: `src/TagEkyc.Infrastructure/RawExport/RawSourceRetentionContinuationRepository.cs`; `src/TagEkyc.Infrastructure/RawExport/RawSourceRetentionContinuationContracts.cs`; `tests/TagEkyc.IntegrationTests/Tip88C1C6BA3RetentionCheckpointTests.cs`; `tests/TagEkyc.IntegrationTests/Tip88C1C6BA3CapabilityLineageTests.cs`. All SQL replacements/additive DDL/trigger work belongs to the one A3 migration pair selected by Parent, not additional historical migration edits. Snapshot entity is `RawExportAuthoritySnapshotRow.cs`; its actual model configuration is inside `TagEkycDbContext.cs` near line751, not a new imagined separate configuration file. Parent's inventory binds exact source SHA; no new path is represented as already landed.

No listed proof has been executed by this documentation change. Product implementation and external final review remain separate gates.

## v0.6 bounded correction record

External F03 is closed at the contract level by CP10.1 and its exact proof row. All v0.5 retention/consent/checkpoint/recovery semantics are preserved; the v0.5 predecessor file remains byte-exact. No product, migration, test, configuration, stage, commit or push action is authorized by this successor.
