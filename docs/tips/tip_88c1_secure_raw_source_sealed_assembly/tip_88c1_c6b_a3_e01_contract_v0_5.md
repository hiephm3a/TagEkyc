# A3 E01 — Existing-consent reference and retained-authority contract

**Version:** v0.5 candidate companion. **Date:** 2026-09-13.
**Status:** proposed technical contract, not independently ratified and not implementation permission.
**Scope:** server metadata integration of the one existing personal-data consent; no new subject agreement, consent text, checkbox, signature, biometric collection or consent-purpose literal.

## 1. Repository facts and trust boundary

The landed B2 implementation is an **authorized recorder attestation**, not an HTTP lookup into an external consent database. `EfRawExportSubjectConsentRepository.cs:22-53,56-86,167-181` sets the authenticated actor and invokes SQL. `20260720022629_Tip88B2SubjectExportConsent.cs:198-226,673-755,778-831` checks the existing authority registry, owns/locks the session, and persists provenance. Latest E3 `20260724015546_Tip88B1E3ResolverReadBoundary.cs:34-128` additionally preserves the Completed requirement. No product API caller of `RecordSubjectConsentGrantedAsync`/`RecordSubjectConsentWithdrawnAsync` was found. Those are landed repository methods, not landed public routes.

A3 reuses that **trust pattern** and the existing `SubjectConsentRecorder` / `SubjectConsentWithdrawer` authority rows. Their use for pre-completion source-reference projection is an explicit proposed technical extension, not a claim that B2 already implements this path. The authenticated recorder asserts the existing source record/version and its relation to the owned subject/session; TagEkyc verifies the recorder's current authority and durable identity/provenance, not the contents of an unavailable external database. An ordinary BusinessConsumer API key plus a nonempty reference is insufficient. No external consent resolver, new trust provider, consent engine or self-granted recorder privilege is invented.

`raw_export_append_subject_consent_authority` remains the existing administrative authority mechanism; A3 does not grant it to the Client, Agent or broker, nor create another authority role. Its self-escalation prohibition and current authority lifecycle remain. Synthetic fixtures seed its administrative grant through that real function, not direct table mutation. Operational provisioning of those already-existing permissions remains a deployment input, never inferred from API-key possession.

## 2. Closed metadata integration operations

These are **new integration operations**, not new subject-consent actions or raw ingress endpoints. They accept no DG2/selfie bytes. They use ordinary authenticated JSON, not CRT1/Expect transport qualification.

| ID | Method/path | Actor | Request | Transaction/result |
| --- | --- | --- | --- | --- |
| E01-R | `POST /api/ekyc/verification-sessions/{sessionId}/source-consent-reference` | Existing `AuthenticatedClientContext`; BusinessConsumer; nonempty PrincipalId and ClientApplicationId; current existing SubjectConsentRecorder for that Client | E01RecordRequest below; mandatory Idempotency-Key | Application owns one transaction; session ownership/state -> recorder authority -> reference lock/CAS -> source projection/binding -> commit; 200 E01BoundResponse |
| E01-W | `POST /api/ekyc/source-consent-references/{referenceId}/withdraw` | Same client context/category; current SubjectConsentWithdrawer for owned reference Client | E01WithdrawRequest below; mandatory Idempotency-Key | Application owns one transaction; reference ownership -> withdrawer authority -> reference lock/CAS -> Withdrawn event/head -> commit; 200 E01WithdrawnResponse |

No target lookup precedes Client authentication. Mixed platform/runtime authentication is rejected as in R20. No new API-key scope is invented: current B2 authority is the additional permission predicate. No Agent, runtime credential or platform context is accepted. Missing/foreign reference, denied recorder/withdrawer and foreign session all return existing `403 ACCESS_DENIED` without target detail. Invalid route/header/closed JSON returns `400 REQUEST_INVALID` after authentication; stale expected revision or changed same-key body returns `409 CONFLICT`; missing dependencies/unexpected SQL failure returns `503 NOT_READY`. Error body is the existing `{code,correlationId}` envelope. Ordinary denial/conflict writes no authoritative row. These administrative integration results are not additions to the 36 raw-ingress outcomes.

Exact closed JSON names/types:

```text
E01RecordRequest {
 externalConsentArtifactRef: string,
 sourceVersion: string,
 expectedReferenceRevision: int64,
 consentTextVersion: string,
 consentTextContentHash: string,
 validFromUtc: timestamp,
 validUntilUtc: timestamp
}
E01BoundResponse {
 consentReferenceId: uuid,
 consentReferenceRevision: int64,
 consentBindingId: uuid
}
E01WithdrawRequest {
 expectedReferenceRevision: int64,
 sourceVersion: string,
 decisionRef: string
}
E01WithdrawnResponse {
 consentReferenceId: uuid,
 consentReferenceRevision: int64,
 state: "Withdrawn"
}
```

All members required, explicit null/duplicates/unknown members rejected by the existing closed JSON reader; no extension bag. Reference 1..512 UTF-8 bytes, sourceVersion/consentTextVersion 1..128, decisionRef 1..256, consentTextContentHash 1..256; exact opaque strings, no trim/case/Unicode rewriting, reject empty/whitespace-only, NUL and CR/LF. ConsentTextContentHash preserves the existing source's evidence string under B2's bounded provenance contract; it does not demand a new source hashing/signature recipe. UUIDs use existing N-lowercase route/header grammar; positive nonempty values. `expectedReferenceRevision=0` only for creation; otherwise positive. UTC timestamps use existing API timestamp parsing and are persisted as UTC timestamptz with microsecond precision; reject a timestamp whose ticks are not a multiple of 10 rather than silently truncating it. Require finite `validFromUtc < validUntilUtc` syntactically; require `validFromUtc <= admission_now < validUntilUtc` for a new record/update, **after** exact replay classification and after all required advisory/row locks, where admission_now is one `pg_catalog.clock_timestamp()` sampled inside SQL as specified in §4.1. Transaction-start/API timestamps are forbidden for this admission. Body maximum 8192 bytes. Existing HTTP fingerprint helper binds exact received JSON bytes, operation ID E01-R/E01-W, exact route, Idempotency-Key and actor partition `ClientApplicationId` 16 RFC-order bytes followed by `PrincipalId` 16 RFC-order bytes. No new JSON canonicalization recipe.

Record authoritatively binds SubjectRef from the locked session; no subject/client/principal IDs are accepted in JSON. Record permits owned nonterminal non-Completed sessions only; later reference withdrawal is allowed after Completed. The existing source-reference head's SubjectRef must match exactly. `SourceVersion` is an immutable opaque upstream record/version identity, not a locally guessed UUID, numeric revision or session ID.

## 3. Finite DDL master

All new tables are in `tagekyc`, owned by `tagekyc_raw_export_deployer`. UUID columns reject all-zero. No raw bytes, API keys, capability secrets, consent document body or pepper enters these tables. Every listed field is NOT NULL unless marked NULL; default is **none** unless shown. No undocumented audit columns. Timestamps are `timestamptz`, revisions `bigint`, UUIDs PostgreSQL `uuid`; strings explicitly typed below. Primary/unique/FK indexes are the only indexes except those explicitly listed. New mutable heads are not consent evidence; append-only events are the evidence source.

| Table | Exact columns | Keys/constraints/indexes | Writer / reader |
| --- | --- | --- | --- |
| `raw_source_consent_references` | `ConsentReferenceId uuid`; `ClientApplicationId uuid`; `SubjectRef text`; `ExternalConsentArtifactRef varchar(512)`; `CurrentRevision bigint` | PK ConsentReferenceId; UNIQUE(ClientApplicationId,ExternalConsentArtifactRef); UNIQUE(ConsentReferenceId,ClientApplicationId); FK ClientApplicationId -> client_applications.Id RESTRICT; CurrentRevision>=1; deferred FK(ConsentReferenceId,CurrentRevision) -> reference_events; reference byte/string checks §2 | E01-R creates/head-CAS; E01-W head-CAS; resolver reads |
| `raw_source_consent_reference_events` | `ConsentReferenceId uuid`; `Revision bigint`; `EventType varchar(16)`; `SourceVersion varchar(128)`; `ConsentTextVersion varchar(128)`; `ConsentTextContentHash varchar(256)`; `ValidFromUtc timestamptz`; `ValidUntilUtc timestamptz`; `RecordedByPrincipalId uuid`; `RecordedAtUtc timestamptz`; `OperationDomain varchar(16)`; `IdempotencyKey uuid`; `RequestFingerprint bytea`; `DecisionRef varchar(256) NULL` | PK(ConsentReferenceId,Revision); FK ConsentReferenceId -> references RESTRICT DEFERRABLE; UNIQUE(RecordedByPrincipalId,OperationDomain,IdempotencyKey); Revision>=1; EventType IN('Recorded','Updated','Withdrawn'); OperationDomain IN('E01-R','E01-W','B2-Withdrawal'); hash32 bytes; validity finite and From<Until; Event Recorded iff revision1; Recorded/Updated domain E01-R and DecisionRef NULL; Withdrawn domain E01-W/B2-Withdrawal and provenance/version/validity copied from predecessor; E01-W DecisionRef nonempty, B2-Withdrawal preserves nullable B2 DecisionRef | E01-R appends Recorded/Updated; E01-W/B2 hook append Withdrawn; resolver reads latest via exact head FK |
| `raw_source_consent_bindings` | `ConsentBindingId uuid`; `ConsentReferenceId uuid`; `ConsentReferenceRevision bigint`; `PrincipalId uuid`; `ClientApplicationId uuid`; `VerificationSessionId uuid`; `SubjectRef text`; `IdempotencyKey uuid`; `RequestFingerprint bytea`; `RecordedAtUtc timestamptz` | PK ConsentBindingId; UNIQUE(PrincipalId,IdempotencyKey); FK(ConsentReferenceId,ConsentReferenceRevision)->reference_events RESTRICT; FK(ConsentReferenceId,ClientApplicationId)->references RESTRICT; FK VerificationSessionId->verification_sessions.Id RESTRICT; revision>=1; fingerprint length32; index(VerificationSessionId,PrincipalId); deferred invariant trigger verifies exact source Client/SubjectRef and session Client/SubjectRef at INSERT | E01-R only inserts; R20/retention resolver reads; no Update/Delete |
| `raw_source_retention_permits` | `RetentionAuthorityId uuid`; `Revision bigint`; `ConsentBindingId uuid`; `PrincipalId uuid`; `ClientApplicationId uuid`; `VerificationSessionId uuid`; `PolicyId uuid`; `PolicyVersion integer`; `Purpose varchar(32)`; `IssuedAtUtc timestamptz`; `ExpiresAtUtc timestamptz`; `ControllerIdentity varchar(128)`; `StableDataScopeId varchar(128)`; `RetentionPolicyId varchar(128)`; `RetentionPolicyVersion integer`; `RetentionClass varchar(64)`; `RetentionStartEvent varchar(32)`; `RevocationPolicyId varchar(128)`; `PurgePolicyId varchar(128)`; `LegalHoldPolicyId varchar(128)`; `MaximumRetentionSeconds integer`; `IssueOperationId uuid`; `RequestFingerprint bytea` | PK(RetentionAuthorityId,Revision); UNIQUE RetentionAuthorityId; UNIQUE(PrincipalId,IssueOperationId); FK ConsentBindingId->bindings RESTRICT; FK(PolicyId,PolicyVersion)->raw_export_policy_versions RESTRICT; Revision=1; PolicyVersion/RetentionPolicyVersion>=1; Purpose='SourceRetention'; RetentionStartEvent='ServerCustodyAccepted'; 1<=MaximumRetentionSeconds<=31536000; finite Issued<Expires; hash32; nonempty profile strings; deferred trigger exact binding P/Client/session equality; index(ConsentBindingId) | R20 issuance B only INSERT; all retention checkpoints SELECT; no update/delete |
| `raw_source_retention_permit_classes` | `RetentionAuthorityId uuid`; `Revision bigint`; `RawClass varchar(32)` | PK(all3); FK(authority,revision)->permits RESTRICT; RawClass IN('ChipDg2Portrait','LiveSelfieImage'); deferred trigger exact two-row set ChipDg2Portrait + LiveSelfieImage and policy coverage of both | R20 issuance INSERT in same B; checkpoints SELECT; no update/delete |

An immutable permit row represents the granted retention decision and its permit together, not two independently mutable copies. `Granted/Revoked/Expired` are resolver states: Granted only while reference/current policy/finite permit remain effective; superseded/Withdrawn reference makes it Revoked, time makes it Expired. No fabricated terminal event is inserted by a read. `ConsentEvidenceId/Revision` in parent logical tables maps exactly to binding's ConsentReferenceId/Revision, while `ConsentBindingId` links the exact actor/session.

DDL triggers deny UPDATE/DELETE of bindings/events/permits/classes; head updates permit only CurrentRevision+1, with the event and old immutable fields checked by a deferred trigger. The SQL owner must be deployer and the exact function-local write context must be present. Runtime/application/broker cannot gain write authority by setting that context: they have no table DML grants. Deferred checks ensure successful operation cannot commit without its exact event/head/binding/class graph. Event operation fingerprints do not accept arbitrary Denied/Replay/Conflict event variants.

Down refuses **any** row in these five new tables or any populated retained authority/capability/binding/snapshot/reservation relationship, including terminal and synthetic rows; no fixture-identity exception or conversion to export authority. On an empty A3 data set only, Down order: drop A3 consumers and their FKs/functions/triggers, permit_classes, permits, bindings, drop deferred reference-head FK, reference_events, references. Reapply proof must recreate exact constraints/ACL. Historical migrations remain byte-identical.

Exact constraint/trigger names (the target predicates are the corresponding finite DDL row above): references `PK_a3_consent_reference`, `UQ_a3_consent_reference_client_ref`, `UQ_a3_consent_reference_id_client`, `FK_a3_consent_reference_client`, `FK_a3_consent_reference_current`, `CK_a3_consent_reference_values`; events `PK_a3_consent_reference_event`, `FK_a3_consent_reference_event_head`, `UQ_a3_consent_reference_event_operation`, `CK_a3_consent_reference_event_shape`; bindings `PK_a3_consent_binding`, `UQ_a3_consent_binding_operation`, `FK_a3_consent_binding_event`, `FK_a3_consent_binding_client`, `FK_a3_consent_binding_session`, `CK_a3_consent_binding_values`, `IX_a3_consent_binding_session_principal`; permits `PK_a3_retention_permit`, `UQ_a3_retention_permit_id`, `UQ_a3_retention_permit_operation`, `FK_a3_retention_permit_binding`, `FK_a3_retention_permit_policy`, `CK_a3_retention_permit_values`, `IX_a3_retention_permit_binding`; classes `PK_a3_retention_permit_class`, `FK_a3_retention_permit_class_permit`, `CK_a3_retention_permit_class`.

`raw_source_enforce_authority_write()` trigger function is installed BEFORE INSERT/UPDATE/DELETE as `tr_a3_reference_write`, `tr_a3_event_write`, `tr_a3_binding_write`, `tr_a3_permit_write`, `tr_a3_class_write` on the five listed tables. It accepts deployer current_user plus transaction-local setting `tagekyc.a3_authority_write` exactly `E01-R`, `E01-W`, `R20-Issue` or `B2-Withdrawal`, with the matching operation/table permissions above; wrong principal still fails semantic trigger checks. `raw_source_require_authority_graph()` is a DEFERRABLE INITIALLY DEFERRED constraint-trigger function, installed AFTER permitted mutations as `tr_a3_reference_graph`, `tr_a3_event_graph`, `tr_a3_binding_graph`, `tr_a3_permit_graph`, `tr_a3_class_graph`; it validates complete exact graph, revision progression, predecessor immutability, source/session lineage, event-domain mapping and the exact two policy-covered classes. Both helpers are trigger-only, grant EXECUTE to no application login/role.

## 4. Exact functions and transaction ownership

All functions below are proposed new functions, SECURITY DEFINER, search_path=pg_catalog, explicit owner deployer. API functions E01-R/E01-W grant EXECUTE only to exact existing `tagekyc_capture_runtime_application`. R20-issue and resolver are internal owner-only functions: all callers are deployer-owned SECURITY DEFINER wrappers, so no external EXECUTE grant is needed. Revoke PUBLIC, tagekyc_runtime, tagekyc_capture_runtime_application, tagekyc_capture_runtime_authenticator, tagekyc_capture_runtime_operator and tagekyc_raw_export_claim_broker on those two helpers. No table SELECT/DML is granted as an alternative. App uses its existing dedicated application gateway connection; broker never invokes record/withdraw/issue directly.

```sql
tagekyc.raw_source_record_consent_reference(
 p_principal_id uuid,p_client_id uuid,p_session_id uuid,p_external_ref text,
 p_source_version text,p_expected_revision bigint,p_text_version text,
 p_text_hash text,p_valid_from timestamptz,p_valid_until timestamptz,
 p_idempotency_key uuid,p_request_fingerprint bytea)
RETURNS TABLE(result_code text,consent_reference_id uuid,
 consent_reference_revision bigint,consent_binding_id uuid);

tagekyc.raw_source_withdraw_consent_reference(
 p_principal_id uuid,p_client_id uuid,p_reference_id uuid,
 p_expected_revision bigint,p_source_version text,p_decision_ref text,
 p_idempotency_key uuid,p_request_fingerprint bytea)
RETURNS TABLE(result_code text,consent_reference_id uuid,
 consent_reference_revision bigint);

tagekyc.raw_source_issue_retention_authority(
 p_principal_id uuid,p_client_id uuid,p_session_id uuid,p_consent_binding_id uuid,
 p_policy_id uuid,p_policy_version integer,p_raw_classes text[],
 p_controller_identity text,p_stable_scope_id text,p_retention_policy_id text,
 p_retention_policy_version integer,p_retention_class text,
 p_revocation_policy_id text,p_purge_policy_id text,p_legal_hold_policy_id text,
 p_maximum_retention_seconds integer,p_issue_operation_id uuid,
 p_request_fingerprint bytea)
RETURNS TABLE(result_code text,retention_authority_id uuid,
 retention_authority_revision bigint,expires_at_utc timestamptz);

tagekyc.raw_source_resolve_retention_authority(
 p_authority_id uuid,p_authority_revision bigint,p_principal_id uuid,
 p_client_id uuid,p_session_id uuid,p_raw_class text,p_now timestamptz)
RETURNS TABLE(is_effective boolean,consent_binding_id uuid,
 consent_reference_id uuid,consent_reference_revision bigint,
 policy_id uuid,policy_version integer,expires_at_utc timestamptz);
```

No current actor comes from JSON: Application passes its authenticated context and sets the same transaction-local actor convention as the existing repository; the three mutation functions require p_principal_id equal `raw_export_current_actor()`. The owner-only read resolver instead checks its snapshot-derived custody principal against the immutable permit/binding; it must not require that principal to equal the current C1/C3 export caller, whose export authority is independently checked. The API gateway is a trusted middle tier exactly as A1. Class/profile values are selected by the closed server profile, not the record route or Agent. Resolver performs no subject action or external request.

E01-R result_code set: `Bound`, `Replay`, `Conflict`, `Denied`. Bound/Replay require all3 response values; Conflict/Denied all3 NULL. E01-W set: `Withdrawn`, `Replay`, `Conflict`, `Denied`; positive values exact persisted event ID/revision; other fields NULL. R20-issue: `Granted`, `Replay`, `Denied`, `Conflict`; positive requires ID/revision/expiry; denial/conflict all NULL. Unexpected shape -> rollback/new work or NOT_READY without hidden partial-success response. Internal resolver returns exactly one fully populated row with is_effective=true, or zero rows; no partially informative denial row.

For E01-R, replay first means exact same authenticated actor/idempotency/fingerprint returns the stored binding including its original reference revision, even if later withdrawn; replay does not reauthorize retention. Lookup ownership still precedes replay. First use locks owned session then recorder authority then source-reference key. If head absent require expected0 and insert Recorded revision1. If head exists require expected=current, same subject, not Withdrawn. Same SourceVersion requires exact immutable provenance+validity equality and creates only the new session binding, no Updated event. Changed SourceVersion must not equal a historical Recorded/Updated source version; append Updated revision+1 and invalidate all old-revision bindings immediately. Same actor/idempotency but changed fingerprint -> Conflict, no event/binding. Exact same actor/session/source revision with a new idempotency key creates a distinct immutable provenance binding; it does not extend source validity. These bindings do not authorize capture by themselves.

For E01-W, exact same actor/idempotency/fingerprint returns original Withdrawn revision. Otherwise current revision/sourceVersion must match, current event not Withdrawn; append one Withdrawn and advance head atomically. Existing Withdrawn with another key -> Conflict, no extra event. No route revives a Withdrawn head. A subsequent genuine source consent is a different external source record, not a replay of this one.

R20 Issue passes a required ConsentBindingId for retained mode; exact actor/client/session must match the binding and current effective reference. Non-retained mode has no permit/binding requirement and no fake lineage values. Existing R20 SQL owns idempotent classification before calling the issuer in that **same** transaction; the Application cannot create a permit in an earlier transaction. The successor R20 gateway transports the exact profile's 12 values as a closed internal JSON object, not a new public JSON bag. Issue computes new permit only after that classification and before capability insert. A failed capability issue rolls back the new permit/classes. Replace reuses its immutable permit/binding/P and does not call issuer. Parent/checkpoint companion owns the exact capability DTO/fingerprint amendment and canonical OM change.

### 4.1 Fresh admission clock and authority expiry — literal correction

Keep all four §4 function signatures unchanged. Do not amend or invoke the existing three-argument `raw_export_subject_consent_has_current_authority` for new E01 admission: its landed SQL uses transaction_timestamp(), which can predate a blocking lock. E01-R and E01-W inline the following exact predicate over the existing authority registry, with `required_type='SubjectConsentRecorder'` for Record and retained R20 issuance and `required_type='SubjectConsentWithdrawer'` for Withdraw. No new authority role/table/helper/provider is introduced.

```sql
WITH latest AS (
 SELECT a."EventType",a."ValidFromUtc",a."ValidUntilUtc"
 FROM tagekyc.raw_export_subject_consent_authorities a
 WHERE a."AuthorityPrincipalId"=p_principal_id
   AND a."ClientApplicationId"=p_client_id
   AND a."AuthorityType"=required_type
 ORDER BY a."Revision" DESC LIMIT 1
)
SELECT EXISTS (
 SELECT 1 FROM latest
 WHERE "EventType"='Granted' AND "ValidFromUtc"<=admission_now
   AND ("ValidUntilUtc" IS NULL OR admission_now<"ValidUntilUtc")
) INTO has_admission_authority;
```

The existing shared authority key is `hashtext('tip88b2:subject-consent-authority:' || p_principal_id::text || ':' || p_client_id::text || ':' || required_type)`. Hold its `pg_advisory_xact_lock_shared` through commit, so the existing authority writer's exclusive lock serializes revocation. Latest overall is selected BEFORE status/validity filtering. False returns ordinary Denied, no reference/event/binding/permit/class mutation.

For a new E01-R/E01-W operation: acquire owned session row when applicable, the authority key, operation key, reference key and all required reference head/event row locks in §6 order; classify exact stored replay/conflict; then assign `admission_now := pg_catalog.clock_timestamp()` once. Evaluate the inline current-authority predicate with that value. E01-R additionally requires request.ValidFromUtc<=admission_now<request.ValidUntilUtc, owned session not expired at admission_now and eligible nonterminal/non-Completed state. For same-SourceVersion binding to an existing current event, that event must also be effective at admission_now; a genuine new source-version update tests the new supplied interval and may supersede an expired but non-Withdrawn older event, without reviving any old binding. New event/binding RecordedAtUtc equals admission_now. Withdraw copies source validity/provenance, so an expired source may still be withdrawn; its withdrawer authority must be current at admission_now. Neither function substitutes transaction_timestamp(), statement_timestamp() or an API-supplied timestamp. Exact stored replay remains a receipt, not a new grant: after current Client authentication and exact target ownership/operation matching, return its persisted tuple without inserting or extending a row; do not apply fresh-record validity to that stored receipt.

For fresh retained R20 issuance: existing SQL classification precedes issuer; take the Recorder authority key before reference, then reference and exact binding/head/event locks; take existing policy publication/lifecycle and required-fulfillment locks before evaluating policy. The policy lock strings remain `tip88b1:raw_export_requirement_rule_set_publish`, `tip88b1:lifecycle:<PolicyId>:<PolicyVersion>`, and `tip88b1:fulfillment:<PolicyId>:<PolicyVersion>:<RequirementType>` with requirement types sorted ordinal; no new requirement semantics. In `raw_source_issue_retention_authority`, sample one admission_now ONLY after those locks and evaluate the same Recorder registry predicate, exact current reference/validity, policy/fulfillment and session expiry using admission_now. Use admission_now for IssuedAtUtc and all min-horizon arithmetic. Do not use R20's older p_now or transaction start. R20 stored replay does not invoke issuer and preserves its secret-once receipt. A recorder whose grant expires while issuance is waiting cannot mint a permit merely because its source assertion was recorded earlier.

The existing B2 three-argument helper and unrelated B2 operations retain their existing semantics; this correction owns only new E01 operations and new retained permit admission. Post-R1 checkpoints still use CP's post-lock clock and exact retained resolver rather than re-authenticating the recorder/API key.

## 5. Profile, horizons and checkpoint predicate

The server-owned closed configuration is `RawSourceRetentionProfiles:Entries`, an array of `RawSourceRetentionProfile` records in new `src/TagEkyc.Application/CaptureRuntime/RawSourceRetentionProfile.cs`; options binding/readiness is implemented in new `src/TagEkyc.Infrastructure/RawExport/RawSourceRetentionProfileValidator.cs` and registered by existing Program/ingress registration. It has no runtime default, environment fallback, open extension bag or Client/Agent-settable API. Exact record members: `Guid ClientApplicationId`, `Guid PolicyId`, `int PolicyVersion`, `string[] RawClasses`, `string ControllerIdentity`, `string StableDataScopeId`, `string RetentionPolicyId`, `int RetentionPolicyVersion`, `string RetentionClass`, `string RevocationPolicyId`, `string PurgePolicyId`, `string LegalHoldPolicyId`, `int MaximumRetentionSeconds`. Every member required, no default. Exactly one entry per configured retained Client (duplicate Client entries fail readiness); Guid nonempty; versions positive; strings use the exact field widths/nonempty rules in the permit DDL; RawClasses must equal exactly the ordered two-value array `["ChipDg2Portrait","LiveSelfieImage"]`; MaximumRetentionSeconds in [1,31536000]. Empty, singleton, duplicate, reordered or extra class arrays fail readiness and fresh SQL issuance; they cannot silently enable a single-class retained session that completion requires to contain both classes. Empty/missing entries do not enable retained mode for any Client. Lookup uses authenticated Client, never first/default entry.

The profile supplies immutable issued policy/classes/source labels; configuration changes affect **future** permits, not existing finite permit horizons. This is not a new RetentionProfileRef resolver or generic policy engine. `PermitTtlSeconds` is not interpreted as source lifetime. Profile policy/version must exist, be CatalogApproved, have mode EncryptedRawVaultRetained, and cover BOTH ChipDg2Portrait and LiveSelfieImage. Each permit inserts exactly those two class rows, enforced at commit; no profile, API or SQL path can create a one-class permit. Existing required policy fulfillment must be evaluated **latest revision overall then effective**, not latest still-Accepted; no new requirement or silent bypass. Checkpoint resolver rereads durable policy state/fulfillment, not mutable process configuration. Existing two-class Available requirement at session completion stays unchanged.

At issue, finite permit ExpiresAt = minimum(existing source event ValidUntilUtc, owned session ExpiresAt, post-lock admission_now + MaximumRetentionSeconds), with ExpiresAt>admission_now. At committed R1, absolute source expiry = minimum(permit ExpiresAt, original source event ValidUntilUtc, custody accepted time + MaximumRetentionSeconds). No retry, config increase, new API key or new binding extends either persisted horizon. Both fixed classes use the shortest applicable bound. Stored profile IDs are evidence labels only; purge/legal-hold enforcement remains A4/separate lifecycle and blocks production, not claimed built here.

Resolver: CP04's actual session-row FOR UPDATE prefix precedes the shared reference lock; exact permit ID/revision/P/Client/session; exact binding FK and current head revision; Recorded/Updated event effective at p_now; finite permit expiry >p_now; requested class in permit and current approved policy; required policy fulfillments current; immutable session ownership/SubjectRef unchanged. Callers acquire prefix/reference/policy locks before sampling the shared p_now; repeated acquisition is reentrant, not permission to supply a stale pre-wait clock. Completed is **not** a rejection after issuance. No latest-permit search, no export decision fallback, no new runtime-auth read in post-R1 custody. Every R1/R3/R4/R5/C1/C3 retained-source consumer calls this exact predicate in its existing transaction; checkpoint companion owns stage transitions and public result mapping.

## 6. Withdrawal/update synchronization and lock graph

Common exact reference key:
`pg_catalog.hashtextextended('tip88c1:a3:consent-reference:' || ClientApplicationId::text || ':' || ExternalConsentArtifactRef, 0)`.
UUID database text is PostgreSQL canonical D-lowercase; external reference exact bytes. Writers take `pg_advisory_xact_lock`, validators shared form, through transaction commit. Hash collision only over-serializes, never joins identities. Updates use exact PK/Client/ref predicates after locking.

Ordering: existing runtime10 -> installation20 -> generation30 -> session70/capability80 (where that route has them) -> actual owned verification_sessions row FOR UPDATE -> acceptance -> B2 recorder/withdrawer authority key (when required) -> E01 operation lock (E01-R/E01-W only) -> E01 reference key -> existing B2 consent-scope (where required) -> retention/snapshot -> existing source alias/exact order. For multi-session source operations CP04 locks all participating session rows in UUID ascending order before reference/source locks. Session70 is advisory, NOT a substitute for the actual row lock: existing B2 consent resolver and withdrawal use session FOR UPDATE. E01 operation key is `hashtextextended('tip88c1:a3:consent-operation:' || PrincipalId::text || ':' || OperationDomain || ':' || IdempotencyKey::text,0)` and is always exclusive; it serializes same actor/domain/key across different reference targets before exact replay/conflict lookup, so uniqueness races map Conflict rather than an accidental database error. A reference-only E01-W takes authority, operation then reference and **never** acquires session, capability, source or provider locks afterward; no artificial session selection is introduced. All identifiers discovered before lock are revalidated after lock. The B2 withdrawal successor keeps its existing actual session-row prefix and prelocks the derived reference **before** existing consent-scope, matching retained-source C1/C3 validation; an AFTER INSERT hook first obtaining reference after consent-scope is forbidden.

The new reference route is the deterministic notification consumer of the existing consent owner's update/withdrawal. It acknowledges only after head/event commit; no async cache expiry/outbox lag is accepted as authorization. Every retained checkpoint reads the current head, so one withdrawal invalidates every existing binding of that reference across sessions, not just the notifying session. External consent changes cannot be magically detected before their authenticated notification arrives; this is the same source-owner reporting trust as B2, stated explicitly, not a claim of continuous external database observation.

Also close the landed B2 withdrawal bypass: a new forward migration replaces the body of existing `raw_export_append_subject_consent_withdrawn(uuid,uuid,integer,integer,integer,text,text)` with a narrow synchronous extension, preserving its signature/return/owner/ACL and every current authorization/target/expiry check. After session ownership and withdrawer-authority lock/check, probe the exact target Granted event using owned session, subject, policy/version, SubjectRawBiometricExport, owned Client and targetRevision; derive its ExternalConsentArtifactRef, find the owned E01 head and acquire reference lock before existing consent-scope. After consent-scope lock, revalidate both target event identity and current head and execute all original B2 checks; no target fallback or trusting optional caller-supplied reference. Insert the original B2 event and capture its real `SubjectConsentRecordId` through RETURNING. Then append at most one E01 Withdrawn projection and head CAS in the same transaction. No matching E01 reference leaves original B2 behavior unchanged; an already Withdrawn head has no duplicate E01 event. The extension does not acquire any earlier lock after reference/consent-scope.

The projection preserves the same underlying withdrawal DecisionRef, actor and current source version; it is not a second user withdrawal. Its OperationDomain is exact `B2-Withdrawal`, distinct in the event UNIQUE key from E01-R/E01-W. IdempotencyKey is the immutable newly inserted B2 `SubjectConsentRecordId` UUID (verified actual PK in the B2 migration), RequestFingerprint is raw SHA256 of that UUID's RFC-order16 bytes. This is internal event identity, not a caller's request digest. No caller can select B2-Withdrawal in JSON or functions. The new code uses/restores the `tagekyc.a3_authority_write` function-local context around its projection insert/head update; direct DML remains denied.

Conversely E01 withdrawal/update immediately denies access to all retained sources through the retained-source current authority predicate, including C1/C3 retained-source checks. It does not fabricate B2 events for sessions not yet Completed. The existing consent owner still records the actual B2 projection through its existing workflow when applicable; A3 neither grants export nor turns a stale B2 grant into authority to bypass a withdrawn retained source.

| Race | Serialization / winner | Losing/replay result | Proof |
| --- | --- | --- | --- |
| Record/Record same expected revision | exact reference X lock/CAS | one new event; changed body Conflict; exact retry stored binding | E01_Race_RecordCas |
| Bind permit / Withdraw before R1 | reference shared vs exclusive | withdrawal first: no permit/capability/newR1; issue first: subsequent R1 rejects current head | E01_Race_IssueWithdrawal |
| R1 / Withdraw | reference shared/exclusive through B commit | R1 first commits custody; withdrawal first denies no body; later checkpoints deny | E01_Race_R1Withdrawal |
| R3/R4/R5 / Withdraw | same lock through existing stage commit | first winner determines current stage; next checkpoint observes loss; no Available after withdrawal wins R5 lock | E01_Race_PublicationWithdrawal |
| Update / old binding replay | head revision and exact frozen binding | replay receipt survives; no new permit from old revision | E01_UpdateDoesNotReviveBinding |
| B2 withdrawal / E01-W | same reference lock | exactly one reference terminal projection; B2's own required event still commits | E01_B2WithdrawalSynchronizes |

## 7. Named proofs and bounded file set

Every proof executes actual API/Application/SQL or actual checkpoint with a separate observer connection. No proof seeds the decision/permit being tested. Existing source assertions and existing administrative recorder/withdrawer grants are valid fixture inputs.

| Claim | Positive control | Intended RED mutation / observable evidence |
| --- | --- | --- |
| ExistingConsentReference_IsBoundNotAssumed | authorized recorder + owned subject/session + valid provenance | bypass recorder predicate; ordinary Client string-only attempt succeeds -> RED; wrong subject/client and stale expected source revision leave zero new rows; wrong binding revision denies issue. This does not pretend to independently verify source truth asserted by an authorized recorder |
| ExistingConsentReuse_NoSecondSubjectAction | one synthetic existing source reference, preCompleted retained permit, later Completed B2 projection using same reference | add requirement for separate subject confirmation/new SourceRetention consent purpose -> RED; observer shows shared source ref, distinct permits |
| E01_RevisionInvalidationReachesEveryCheckpoint | active reference reaches each actual stage | disable head-revision comparison; Updated/Withdrawn old permit passes -> RED at R1/R3/R4/R5/C1/C3 |
| E01_RecorderRevocationRacesRecord | valid current recorder first | remove existing shared authority lock; controlled revoke-before-record interleaving commits -> RED |
| E01_ExactReplayDoesNotReauthorize | record then withdraw then replay | replay must return stored binding but issuer denies it; mutation replay=>effective grants -> RED |
| E01_NoCompletedCycle | nonCompleted session uses E01 and retained permit | restore Completed gate before retention issuance -> RED; ordinary B2 preCompleted still rejects |
| E01_B2WithdrawalSynchronizes | actual B2 withdrawal without optional externalRef | remove hook -> reference remains current/retainedcheckpoint succeeds -> RED |
| E01_DdlGraphAndAcl | real functions create graph | direct DML, head-without-event, crossClient binding, empty classes, fake actor FK, broken permit binding -> named CHECK/FK/trigger or ACL denial |
| E01_ProfileHorizonFrozen | config/source/session differing finite horizons | max instead of min / reinterpret permit TTL / extend on retry -> RED on observer expiry |
| E01_Record_PostLockRecorderExpiry | real E01-R baseline with live recorder and valid source creates binding | second PG connection holds reference X lock; start real E01-R transaction before recorder expiry, confirm its lock wait, wait until DB clock crosses grant expiry, release; denied, observer finds zero new event/head/binding. Replacing admission_now with transaction_timestamp or old three-argument helper must let mutation through and turn test RED |
| E01_Record_PostLockSourceExpiry | same successful baseline with source interval valid at start | hold reference X lock across supplied ValidUntilUtc while recorder stays valid; release after DB clock crosses interval; denial has zero new rows. Mutation transaction-start clock must make this intended negative RED |
| E01_Issue_PostLockRecorderAndSourceExpiry | actual E01-R and R20 retained Issue positive control mint exactly one permit and two classes | two independent cases: block reference lock across recorder expiry with source still valid; block across source expiry with recorder still valid. Start R20 before expiry, confirm pg_stat_activity lock wait, release after DB clock; denied, zero new permit/classes/capability. Frozen p_now / transaction_timestamp mutation must turn each case RED |
| E01_ExactBothClassesRequired | profile and actual SQL issuance exactly DG2+selfie, completion consumes both Available sources | remove either profile class or either INSERT class row while all other authority remains valid: readiness/SQL named graph guard denies; removing exact-set predicate must turn intended negative RED; no completion relaxation |

The three blocking-clock proofs use real database expiry instants and a second connection's lock, not an injected clock or a permanently unauthorized fixture. First demonstrate the same API/SQL input succeeds without crossing expiry. Confirm the contender began before expiry and actually waits on the intended lock, then poll database clock with a bounded timeout before releasing it; a test failing to establish either condition fails setup rather than claiming the authorization negative. Observer assertions compare persisted row sets, not just the returned denial. Run each deliberate clock mutation independently and restore before positive rerun. No product test-only time provider or authority bypass is authorized.

Proposed new server files: `src/TagEkyc.Contracts/CaptureRuntime/RawSourceConsentContracts.cs`; `src/TagEkyc.Application/CaptureRuntime/RawSourceConsentApplicationService.cs`; `src/TagEkyc.Application/Ports/RawSourceRetentionPorts.cs`; `src/TagEkyc.Infrastructure/Persistence/RawSourceRetentionGateway.cs`; `src/TagEkyc.Infrastructure/Persistence/Entities/RawSourceRetentionEntities.cs`; `src/TagEkyc.Infrastructure/Persistence/RawSourceRetentionEntityConfigurations.cs`; `src/TagEkyc.Api/RawSourceConsentEndpoints.cs`; `tests/TagEkyc.IntegrationTests/Tip88C1C6BA3ConsentRetentionTests.cs`. Existing modifications consumed: `CaptureRuntimeExecutionApplicationService.cs`, `CaptureRuntimeExecutionContracts.cs`, `CaptureRuntimePorts.cs`, `CaptureRuntimeExecutionEndpoints.cs`, `Program.cs`, `TagEkycDbContext.cs`, A3 forward migration pair/model snapshot, exact B2 function successor body in that forward migration only. Parent's mutation/source-freeze ledger binds exact paths and SHA; this list alone does not authorize edits. No existing B2/E3 historical migration file mutation.

E01 provenance source paths are those cited in §1 plus `src/TagEkyc.Domain/RawExportSubjectConsent.cs`, `src/TagEkyc.Domain/RawExportPolicyCatalog.cs`, `src/TagEkyc.Infrastructure/Persistence/Entities/VerificationSessionRow.cs`, and `src/TagEkyc.Api/CaptureRuntimeExecutionEndpoints.cs`. Parent must bind their full live64 SHA; no partial-hash comparison or invented landed route is permitted.
