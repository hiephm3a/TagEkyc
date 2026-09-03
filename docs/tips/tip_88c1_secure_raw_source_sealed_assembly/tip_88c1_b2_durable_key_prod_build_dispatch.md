# TIP-88C1-B2-DURABLE-KEY-PROD — Durable Production-Key Custody Contract — BUILD DISPATCH (v0.5)

**Status: HOMEOWNER-CORRECTED — NARROW PROOF BUILD AUTHORIZED — IMPLEMENTATION IN PROGRESS — NOT PRODUCTION.** Standalone
production-key custody contract (post-decomposition lead). Proof-build authority is confined to the exact M8 allowlist. DO NOT
COMMIT/PUSH/MERGE/PR/DEPLOY; do not install/relocate/drop any PostgreSQL extension. R2/DURABLE-OBJECT/S3/MinIO/Raw BIO blocked.
**Repo:** `D:\Task\Remote Signing\TagEkyc` · **Baseline:** `23e0154805e468919e513bf04b0a24d374d1ef13` · **Tier-0.**
This document is **self-contained**: every build-critical fact is inline; the superseded monolith
`tip_88c1_b2_durable_key_custody_build_dispatch.md` is historical provenance only. **The contract is NOT blocked** by the
production CSPRNG extension-owner literal — that literal + deployment provisioning are a PRODUCTION-ACTIVATION gate only, not a
docs/proof-build/dev-test blocker (§6c, L).

**Dependency direction (phase-specific, one-directional):** (1) DK-PROD docs closure does NOT depend on fixture
implementation; it reviews the neutral provider port (§3) independently. (2) DK-FIXTURE-PROOF depends on the frozen
version/hash of DK-PROD §3 port + §8 digest contract. (3) DK-PROD proof build MAY use the frozen fixture test double for
fresh-process integration tests; it never imports fixture schema. (4) Production activation depends on a separately-qualified
native durable KMS/HSM provider; the fixture never qualifies production. CSPRNG topology ratified (Option 1, §6c).

## Changelog

### v0.5 — deadline-only current-profile authority correction
- Fixed the current timing profile; configuration is conformance input only and cannot change operational timing.
- Classified `FUTURE_GATE T34a/T34b`, `FUTURE_GATE #26` and `FUTURE_GATE MaxAttemptExceeded` under
  `DK-PROD-DURABLE-CONFIG-SNAPSHOT-V1`; the current profile retains T33 deadline exhaustion only.
- Recomputed the active contract to 46 mutating edges and 78 active tests while preserving 18 reachable pairs and 48 rejected
  cells; expanded the checker to ten negative self-tests.

### v0.4 — executable state-model reconciliation
- Replaced the impossible cross-table CHECK claim with local CHECKs plus two deferred constraint triggers.
- Added the executable pair/field-delta model as the sole authority for reachability and sparse-state census.
- Corrected PE/PL retry-clock, AR cleanup-clock and RA terminal cleanup semantics.
- Corrected test/helper/trigger/index/allowlist censuses and narrowed provider-result/cancellation claims to the actual trust boundary.
- Closed execution dataflow, fresh-generation counter reset, boolean value semantics, dependency-safe Down order and post-RO
  lease behavior; checker now parses J1/M6/M8/SQL manifest and carries six negative self-tests.

### v0.3 — contractor reconciliation
- Restored provider recovery/cleanup contracts, direct corruption, M7, effective ACL checks and two-lineage cleanup shape.

## TIP Analytical Summary / Intent Ledger

### Intent
Define the smallest durable production wrapped-DEK reservation/recovery contract required before R2 may use a key after process
restart, without implementing Raw BIO, ciphertext object custody or delivery.

### Expected Outcome
An authorized builder can implement one durable reservation per encryption attempt, publish only committed wrapped-key state,
recover/reconcile it without process memory and reject illegal head/mapping combinations at commit.

### Accepted Decisions
| Decision | Why accepted | Scope impact | Non-claims |
|---|---|---|---|
| Durable provider port + PostgreSQL wrapped-result metadata | survives restart | DK-PROD only | no provider qualification |
| Executable pair/delta model is normative | eliminates hand-counted matrix drift | docs tooling + generated contract | not runtime code |
| Local CHECKs + deferred pair triggers | PostgreSQL CHECK cannot read another table | two owner-only constraint triggers | no generic validator |
| Trusted reconciler asserts provider result | matches callable SQL boundary | capability + reference binding | no provenance against compromised reconciler |

### Rejected / Deferred Branches
| Branch / option | Disposition | Why | Follow-up debt/gate |
|---|---|---|---|
| Process-local key store | rejected for production | cannot survive restart | fixture proof only |
| Raw encryption/object storage | deferred | owned by later slices | no Raw BIO capability here |
| Exact production pgcrypto owner | activation gate | deployment-specific | close before production activation |

### Debt / Gap Impact
| Debt/gap | Action | Result | Carry-forward gate |
|---|---|---|---|
| DK-FIXTURE-PROOF stale anchors | recorded, not silently edited | sibling remains draft | synchronize after freeze |
| Native durable provider qualification | deferred | dev/test topology only | qualification before activation |

### Non-Claims
No Raw BIO access, encryption, object persistence, delivery, real-provider qualification, production readiness or deployment is
created or proven by this document or its executable model.

### Dispatch Readiness
The Homeowner-authorized narrow proof build may proceed only within the exact M8 allowlist after the Phase-A checker,
preservation, census and hash gates pass. This is not Raw BIO, R2, provider qualification, production activation or deployment
authority.

## K1. Landed codec
`tagekyc.raw_export_c1_hash_canonical(p_domain text, VARIADIC p_fields text[]) RETURNS bytea` (landed; no second helper).
`C1HashCanonical(domain,s0..s(n-1))` = SHA-256 over `LP(domain)` then `LP(s_i)`, `LP(s)=u32-BE(octet_length(NFC-UTF8(s))) ‖
NFC-UTF8(s)`. Guid→lowercase 32-char "N" text; byte[]→lowercase hex; integer→invariant decimal; text→NFC. Optional: absent→
one scalar `"0"`; present→`"1"` then value. Present token/reference/receipt/actor values: NFC+trimmed, non-empty,
control-char-free, ≤512 UTF-8 bytes (§M7).

## 1. Operational transition summary

The executable normative source for predecessor/successor pairs and pair-specific field deltas is
`tip_88c1_b2_durable_key_prod_state_model.py`. The checker must exit 0 before review or dispatch. This table owns operation,
role, event, outcome, readiness and proof semantics; brace/group notation here is presentation-only and MUST NOT be used to
generate a CHECK, trigger predicate, field delta or census. Each grouped predecessor expands to the individually named edge in
the executable model (for example T7a/T7b and T12a/T12b/T12c).
Head H = { none, PreparingLive=PL, PreparingExpiredAwaitingResolution=PE, ProviderOutcomeUnknown=POU,
ProviderCorruptOrUnverifiable=PC, ProviderCleanupRequired=PCR, ReadyForFreshPreparation=RF, Active=AC, Revoked=RV,
AbandonRequested=AR, ReservationAbandoned=RA } — **11 conceptual head values incl. `none`; 10 persisted
`PreparationDisposition` literals** (all except `none`). Mapping M = { none, Issued=IS, ResultObserved=RO, CleanupRequired=CR,
CleanedUp=CU, AbsenceProven=AP } — **6 conceptual; 5 persisted `ProviderOperationState` literals** (all except `none`).
Every transition = one guarded CAS under a `FOR UPDATE` lock of the head (+ its mapping); time via
`pg_catalog.statement_timestamp()`. **Resolution/expiry invariant (derivation basis):** the awaiting-resolution operations
(`mark-expired`, `resolve`, direct-corrupt and deadline exhaustion) apply **only when mapping = `IS`** — once the wrapped result
is durably observed (`RO`), the reservation is not lease-subject (the DEK is wrapped; activation is a local step), so `(PE,RO)`,
`(POU,RO)` and `(PC,RO)` are unreachable and rejected by the deferred pair constraint. Business conditions are typed RETURNs; only
structural/guard violations raise `P0001` (`RAW_EXPORT_KEY_WRITE_CONTEXT_INVALID` guard, `RAW_EXPORT_KEY_ARGUMENT_INVALID`
argument, `RAW_EXPORT_KEY_ACTOR_CONTEXT_MISSING` actor). **`ShapeInvalid`/`SuiteMismatch` are typed business RETURNs, never
`P0001` (Part I).** Field-delta notation (Part C): SET(v)=assigned; PRESERVE=unchanged; CLEAR=set NULL; RN=REQUIRE NULL at
destination; RNN=REQUIRE NON-NULL; EITHER (explained). The executable model has one edge per distinct predecessor and field
delta; this operational table may group edges only when operation/event/outcome semantics are identical. Rejections and
idempotent no-write replays remain function outcomes and are not counted as mutating edges.

| T | Op | Role | Fn | Prov.obs | (H,M) pred → (H,M) succ | Head-Δ | Mapping-Δ | Event | Recomputed evidence | Outcome | Retry/deadline | Readiness | Rec.family | Test |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| T1 | prepare-first | encryptor | prepare | — | (none,none)→(PL,IS) | SET disposition=PL,fence=1,PreparationId,token,lease,ContextFingerprint; counters=0; intervention=false | insert IS | Opened | ContextFingerprint | PreparingLive | — | — | preparation | #1 |
| T2 | prepare-fresh | encryptor | prepare | — | (RF,{CU,AP})→(PL,IS) | SET disposition=PL,fence+1,new PreparationId/token/lease; counters=0; intervention=false; CLEAR wrapped/revoke fields | insert new IS | Opened | ContextFingerprint | PreparingLive | fence+1 | — | preparation | #2 |
| T3 | prepare-replay | encryptor | prepare | — | (PL,IS)→(PL,IS) | PRESERVE (idempotent) | PRESERVE | — | — | InProgress | — | — | preparation | #3 |
| T4 | record-wrapped-direct | encryptor | record_wrapped | Found(direct) | (PL,IS)→(PL,RO) | CLEAR resolution clocks | SET wrapped family, recomputed MetadataDigest, ResultObservedAtUtc | — | MetadataDigest | ResultObserved,ExistingMatch,ShapeInvalid,SuiteMismatch | — | — | mapping | #4 |
| T5 | activate-direct | encryptor | activate_direct | — | (PL,RO)→(AC,RO) | SET wrapped→head,PreparedAtUtc,disposition=AC | PRESERVE | DirectActivated | (verify MetadataDigest) | Activated (lease expiry ignored after RO) | no lease gate | — | activation | #5 |
| T6 | mark-expired | reconciler | mark_expired | — | (PL,IS)→(PE,IS) | SET disposition=PE; SET ResolutionDeadlineUtc IF NULL | PRESERVE | Expired | — | Expired,NotDue | init ResolutionDeadline once | — | resolution | #6 |
| T7 | first-Unknown | reconciler | resolve | Unknown | {(PL,IS),(PE,IS)}→(POU,IS) | SET disposition=POU; +1 count; SET deadline IF NULL + Next | PRESERVE | ResolvedOutcomeUnknown | ResolutionEvidence | ResolvedOutcomeUnknown | +1 res.attempt; backoff | — | resolution | #7 |
| T8 | repeated-Unknown (C1) | reconciler | resolve | Unknown | (POU,IS)→(POU,IS) | PRESERVE disposition; +1 ResolutionAttemptCount; SET Next | PRESERVE | ResolvedOutcomeUnknown | ResolutionEvidence | ResolvedOutcomeUnknown | +1 res.attempt; backoff | — | resolution | #8 |
| T9 | Unavailable-from-PL | reconciler | resolve | Unavailable | (PL,IS)→(PL,IS) | PRESERVE disposition; +1 count; SET deadline IF NULL + Next | PRESERVE | ProviderUnavailableObserved | ResolutionEvidence (H) | ProviderUnavailableObserved | +1 res.attempt; backoff | — | resolution | #9a |
| T10 | Unavailable-from-PE | reconciler | resolve | Unavailable | (PE,IS)→(PE,IS) | PRESERVE; +1 count; SET Next | PRESERVE | ProviderUnavailableObserved | ResolutionEvidence (H) | ProviderUnavailableObserved | +1 res.attempt; backoff | — | resolution | #9b |
| T11 | Unavailable-from-POU | reconciler | resolve | Unavailable | (POU,IS)→(POU,IS) | PRESERVE; +1 count; SET Next | PRESERVE | ProviderUnavailableObserved | ResolutionEvidence (H) | ProviderUnavailableObserved | +1 res.attempt; backoff | — | resolution | #9c |
| T12 | direct-corrupt (B) | reconciler | resolve | CorruptOrUnverifiable | {(PL,IS),(PE,IS),(POU,IS)}→(PC,IS) | SET disposition=PC | PRESERVE | ResolvedCorrupt | ResolutionEvidence | ResolvedCorrupt | — (no successor) | CORRUPT_UNRESOLVED | resolution | #12 |
| T13 | positive-absence | reconciler | resolve(gated) | NoProviderResult{receipt} | (PE,IS)→(RF,AP) | SET disposition=RF; CLEAR CurrentPreparationId/lease/token/ResolutionDeadline/NextResolution/CleanupDeadline/NextCleanup | SET ProviderAbsenceProofReceipt (IS→AP) | ResolvedNoResult | AbsenceEvidence (stored in ProviderResolutionEvidenceDigest, §8) | ResolvedNoResult | now()≥lease req | — | resolution+mapping | #10,#11 |
| T14 | cleanup-required | reconciler | mark_cleanup_req | ResourceCleanupRequired{ref} | {(PL,IS),(PE,IS),(POU,IS)}→(PCR,CR) | SET disposition=PCR + CleanupDeadline; CLEAR resolution clocks | SET ProviderCleanupReference (IS→CR); wrapped RN | ResolvedCleanupRequired | ResolutionEvidence | CleanupRequired,ExistingMatch | init CleanupDeadline once | — | cleanup | #13 |
| T15 | cleanup-obs Unavailable | reconciler | record_cleanup_obs | CleanupUnavailable | (PCR,CR)→(PCR,CR) | PRESERVE; +1 CleanupAttemptCount; SET NextCleanup | PRESERVE | CleanupAttemptObserved | CleanupObservationEvidence | CleanupObserved | +1 cleanup.attempt; backoff | — | cleanup | #14 |
| T16 | cleanup-obs OutcomeUnknown | reconciler | record_cleanup_obs | CleanupOutcomeUnknown | (PCR,CR)→(PCR,CR) | PRESERVE; +1 CleanupAttemptCount; SET NextCleanup | PRESERVE | CleanupAttemptObserved | CleanupObservationEvidence | CleanupObserved | +1 cleanup.attempt; backoff | — | cleanup | #15 |
| T17 | cleanup-obs Failed | reconciler | record_cleanup_obs | CleanupFailed | (PCR,CR)→(PCR,CR) | PRESERVE; +1 CleanupAttemptCount; SET NextCleanup | PRESERVE | CleanupAttemptObserved | CleanupObservationEvidence | CleanupObserved | +1 cleanup.attempt; backoff | — | cleanup | #16 |
| T18 | cleanup-ack Cleaned | reconciler | ack_cleanup | Cleaned | (PCR,CR)→(RF,CU) | SET disposition=RF; CLEAR gen fields/clocks/intervention | SET ProviderCleanupReceipt (CR→CU); wrapped PRESERVE(EITHER, D) | CleanupAcknowledged | CleanupEvidence | Acknowledged | — | — | cleanup | #17 |
| T19 | cleanup-ack AlreadyAbsent | reconciler | ack_cleanup | AlreadyAbsent | (PCR,CR)→(RF,CU) | SET disposition=RF; CLEAR gen fields/clocks/intervention | SET ProviderCleanupReceipt (CR→CU); wrapped PRESERVE(EITHER) | CleanupAcknowledged | CleanupEvidence | Acknowledged | — | — | cleanup | #18 |
| T20 | recovered-record+activate (A) | reconciler | record_recovered | Found(lookup) | {(PL,IS),(PE,IS),(POU,IS)}→(AC,RO) | SET wrapped→head,PreparedAtUtc,disposition=AC; CLEAR all retry/cleanup clocks | SET wrapped family, recomputed digests (IS→RO) | RecoveredActivated | Metadata+Resolution | RecoveredActivated,ShapeInvalid,SuiteMismatch | — | — | activation | #19,#20 |
| T21 | request-abandon | lifecycle | request_abandon | — | {(PL,IS),(PL,RO),(PE,IS),(POU,IS),(PCR,CR)}→(AR,same M) | SET disposition=AR; RowRevision++; pair-specific clock CLEAR/PRESERVE per model | PRESERVE | AbandonRequested | (RequestingActorEvidence) | AbandonRequested,AlreadyRequested | — | ABANDON_PENDING | abandonment | #21 |
| T22 | AR cleanup-entry (B1) | reconciler | mark_cleanup_req | ResourceCleanupRequired{ref} | (AR,{IS,RO})→(AR,CR) | PRESERVE disposition=AR; SET CleanupDeadline IF NULL | SET ProviderCleanupReference ({IS,RO}→CR); wrapped EITHER(D) | ResolvedCleanupRequired | ResolutionEvidence | CleanupRequired | init CleanupDeadline once | ABANDON_PENDING | cleanup+abandon | #22a |
| T23 | AR CleanupUnavailable (B2) | reconciler | record_cleanup_obs | CleanupUnavailable | (AR,CR)→(AR,CR) | PRESERVE; +1 CleanupAttemptCount; SET Next | PRESERVE | CleanupAttemptObserved | CleanupObservationEvidence | CleanupObserved | +1 cleanup.attempt | ABANDON_PENDING | cleanup+abandon | #22b |
| T24 | AR CleanupOutcomeUnknown (B2) | reconciler | record_cleanup_obs | CleanupOutcomeUnknown | (AR,CR)→(AR,CR) | PRESERVE; +1; SET Next | PRESERVE | CleanupAttemptObserved | CleanupObservationEvidence | CleanupObserved | +1 cleanup.attempt | ABANDON_PENDING | cleanup+abandon | #22c |
| T25 | AR CleanupFailed (B2) | reconciler | record_cleanup_obs | CleanupFailed | (AR,CR)→(AR,CR) | PRESERVE; +1; SET Next | PRESERVE | CleanupAttemptObserved | CleanupObservationEvidence | CleanupObserved | +1 cleanup.attempt | ABANDON_PENDING | cleanup+abandon | #22d |
| T26 | AR Cleaned (B3) | reconciler | ack_cleanup | Cleaned | (AR,CR)→(AR,CU) | PRESERVE disposition=AR; CLEAR cleanup clocks/intervention | SET ProviderCleanupReceipt (CR→CU); wrapped EITHER | CleanupAcknowledged | CleanupEvidence | Acknowledged | — | ABANDON_PENDING | cleanup+abandon | #22e |
| T27 | AR AlreadyAbsent (B3) | reconciler | ack_cleanup | AlreadyAbsent | (AR,CR)→(AR,CU) | PRESERVE disposition=AR; CLEAR cleanup clocks/intervention | SET ProviderCleanupReceipt (CR→CU) | CleanupAcknowledged | CleanupEvidence | Acknowledged | — | ABANDON_PENDING | cleanup+abandon | #22f |
| T28 | AR positive-absence (B4) | reconciler | resolve(gated) | NoProviderResult{receipt} | (AR,IS)→(AR,AP) | PRESERVE disposition=AR; CLEAR all clocks | SET ProviderAbsenceProofReceipt (IS→AP) | ResolvedNoResult | AbsenceEvidence | ResolvedNoResult | now()≥lease req | ABANDON_PENDING | mapping+abandon | #22g |
| T29 | finalize-from-CU (B5) | lifecycle | finalize_abandon | — | (AR,CU)→(RA,CU) | SET disposition=RA; final RowRevision; CLEAR lease/all clocks | PRESERVE | ProviderOperationAbandoned | AbandonmentEvidence | ReservationAbandoned | — | RA clean | abandonment | #22h |
| T30 | finalize-from-AP (B5) | lifecycle | finalize_abandon | — | (AR,AP)→(RA,AP) | SET disposition=RA; final RowRevision; CLEAR lease/all clocks | PRESERVE | ProviderOperationAbandoned | AbandonmentEvidence | ReservationAbandoned | — | RA clean | abandonment | #22i |
| T31 | finalize-rejected | lifecycle | finalize_abandon | — | (AR,{IS,RO,CR})→(AR,same) | PRESERVE (reject) | PRESERVE | — | — | CleanupNotComplete | — | ABANDON_PENDING | abandonment | #22j |
| T32 | revoke | reconciler,lifecycle | revoke | — | (AC,RO)→(RV,RO) | SET disposition=RV,RevokedAtUtc,RevocationReasonCode | PRESERVE | Revoked | RevocationEvidence | Revoked,AlreadyRevoked,NotActive | — | RV clean | revocation | #23,#24 |
| T33 | resolution-deadline-exhaustion | reconciler | resolve | timeout | {(PE,IS),(POU,IS)}→(PC,IS) | SET disposition=PC | PRESERVE | ResolvedCorrupt | ResolutionEvidence | DeadlineExceeded | now()>ResolutionDeadline | CORRUPT_UNRESOLVED | resolution | #25 |
| T35 | cleanup-deadline-intervention | reconciler | record_cleanup_obs | timeout | (PCR,CR)→(PCR,CR) | PRESERVE; SET CleanupOperatorInterventionRequired=true | PRESERVE | CleanupAttemptObserved | CleanupObservationEvidence | DeadlineIntervention | now()>CleanupDeadline | CLEANUP_OPERATOR_INTERVENTION | cleanup | #27 |
| T36 | activate-replay | encryptor | activate_direct | — | (AC,RO)→(AC,RO) | PRESERVE (idempotent) | PRESERVE | — | — | AlreadyActivated | — | — | activation | #28 |
| T37 | finalize-replay | lifecycle | finalize_abandon | — | (RA,{CU,AP})→(RA,same) | PRESERVE (idempotent) | PRESERVE | — | — | AlreadyAbandoned | — | RA clean | abandonment | #29 |
| T38 | stale-fence prepare | encryptor | prepare | — | reject | none | none | — | — | StaleFence | — | — | — | #30 |
| T39 | stale-token resolve | reconciler | resolve | — | reject | none | none | — | — | StalePreparation | — | — | — | #31 |
| T40 | stale-state finalize | lifecycle | finalize_abandon | — | reject | none | none | — | — | NotRequested,StateConflict | — | — | — | #32 |

### Future-gated transition — `DK-PROD-DURABLE-CONFIG-SNAPSHOT-V1`

This table is **not active or callable** in the fixed current profile. `ResolutionMaxAttemptCount = 0` means deadline-only
resolution governance; the current callable resolve function never returns the reserved outcome below. The identifiers remain
reserved without renumbering T35–T40 or test #27 onward.

| T | Op | Future predecessors → successor | Reserved outcome | Reserved proof | Activation condition |
|---|---|---|---|---|---|
| T34 | resolution-maxattempt-exhaustion | {(PE,IS),(POU,IS)}→(PC,IS) | MaxAttemptExceeded | #26 | FUTURE_GATE only: positive immutable max-attempt value under `DK-PROD-DURABLE-CONFIG-SNAPSHOT-V1`; inactive while current max is 0 |

Structural SQLSTATE per row = `P0001` with MessageText `RAW_EXPORT_KEY_ARGUMENT_INVALID` / `RAW_EXPORT_KEY_WRITE_CONTEXT_INVALID` / `RAW_EXPORT_KEY_ACTOR_CONTEXT_MISSING`
(argument/guard/actor). Down impact for every function = the migration drops it (no orphan overload/grant, §M9). If any real
transition cannot be one coherent row here, STOP/RRI — no new state/table/function.

### 1a. Reachable-pair derivation (generated by the executable model, not copied)
| T | predecessor (H,M) | successor (H,M) | newly reachable pair(s) |
|---|---|---|---|
| (init) | — | (none,none) | (none,none) |
| T1 | (none,none) | (PL,IS) | (PL,IS) |
| T4 | (PL,IS) | (PL,RO) | (PL,RO) |
| T5 | (PL,RO) | (AC,RO) | (AC,RO) |
| T6 | (PL,IS) | (PE,IS) | (PE,IS) |
| T7/T8 | (PL,IS)/(PE,IS)/(POU,IS) | (POU,IS) | (POU,IS) |
| T12/T33 | (PL,IS)/(PE,IS)/(POU,IS) | (PC,IS) | (PC,IS) |
| T13 | (PE,IS) | (RF,AP) | (RF,AP) |
| T14 | (PL,IS)/(PE,IS)/(POU,IS) | (PCR,CR) | (PCR,CR) |
| T18/T19 | (PCR,CR) | (RF,CU) | (RF,CU) |
| T20 | (PL,IS)/(PE,IS)/(POU,IS) | (AC,RO) | (already) |
| T21 | (PL,IS)/(PL,RO)/(PE,IS)/(POU,IS)/(PCR,CR) | (AR,IS)/(AR,RO)/(AR,CR) | (AR,IS),(AR,RO),(AR,CR) |
| T22 | (AR,IS)/(AR,RO) | (AR,CR) | (already) |
| T26/T27 | (AR,CR) | (AR,CU) | (AR,CU) |
| T28 | (AR,IS) | (AR,AP) | (AR,AP) |
| T29 | (AR,CU) | (RA,CU) | (RA,CU) |
| T30 | (AR,AP) | (RA,AP) | (RA,AP) |
| T32 | (AC,RO) | (RV,RO) | (RV,RO) |
Run `python docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_b2_durable_key_prod_state_model.py`. Required output:
`mutating_edges=46`, followed by the census below and the exact 18 `PAIR` lines. Idempotent no-write replays and rejected
calls are function outcomes, not mutating edges.

**Census:** conceptual head values **11** (incl. `none`); persisted `PreparationDisposition` literals **10**; mapping states
conceptual **6**, persisted **5**; **exact reachable pairs = 18**; total Cartesian cells = 11×6 = **66**; rejected cells =
66−18 = **48**. (Diverges from the review's un-derived 21 because the resolution/expiry invariant makes `(PE,RO)`,`(POU,RO)`,
`(PC,RO)` unreachable.) The executable model, deferred-trigger predicate, §J1 matrix and census carry exactly these 18.

## 2. Production schema (exact, standalone)
### 2a. R1 additive binding
Landed `tagekyc.raw_export_source_encryption_attempts` has PK(`AttemptId`), UNIQUE(`SourceArtifactId`,`EncryptionAttemptRevision`),
UNIQUE(`SourceArtifactId`,`AttemptId`,`Fence`); lacks `UNIQUE(AttemptId,AttemptKeyReservationId)`. Add
`uq_raw_export_enc_attempt_attemptid_keyresvid` (45) `UNIQUE("AttemptId","AttemptKeyReservationId")`; head declares
`fk_raw_export_attempt_key_resv_attempt_composite` (48) `FOREIGN KEY("AttemptId","AttemptKeyReservationId") REFERENCES
tagekyc.raw_export_source_encryption_attempts("AttemptId","AttemptKeyReservationId") ON DELETE RESTRICT` (source order = target
order = the new AK). EF: `HasOne<RawExportSourceEncryptionAttemptRow>().WithMany().HasForeignKey(r=>new{r.AttemptId,
r.AttemptKeyReservationId}).HasPrincipalKey(a=>new{a.AttemptId,a.AttemptKeyReservationId}).OnDelete(Restrict).HasConstraintName
("fk_raw_export_attempt_key_resv_attempt_composite")`. **Up:** add the landed-attempt AK, create the head table, then create
the composite FK on the head table (either inline during `CREATE TABLE` or explicitly afterward). **Down:** drop the composite
FK while the head table still exists, drop the head table, then drop the landed-attempt AK. Catalog round-trip + `23503`
mutation (#33).

### 2b. Head `tagekyc.raw_export_attempt_key_reservations` (35; owner `tagekyc_raw_export_deployer`; direct DML denied)
Columns/types/null/default/constraint/ownership: `AttemptKeyReservationId uuid NOT NULL PK immutable`; `AttemptId uuid NOT NULL
FK immutable`; `EncryptionAttemptFingerprint bytea NOT NULL CHECK octet_length=32 immutable`; `KeyProviderId/KekId/KekFingerprint
text NOT NULL immutable (§M7)`; `KekVersion integer NOT NULL immutable`; `AttemptKeyContextFingerprint bytea NOT NULL CHECK
octet_length=32 generated(prepare)`; `WrappingSuiteId text NOT NULL ='AES-256-GCM' immutable`; `WrappingSuiteVersion integer NOT
NULL =1 immutable`; `PreparationDisposition text NOT NULL ∈ 10 persisted literals mutable`; `CurrentPreparationId uuid NULL`;
`CurrentPreparationFence bigint NOT NULL DEFAULT 1 server(+1/fresh)`; `CurrentPreparationLeaseExpiresAtUtc timestamptz NULL`;
`CurrentProviderOperationToken text NULL 43-char base64url(§6c) server`; `ResolutionAttemptCount bigint NOT NULL DEFAULT 0
server`; `NextResolutionAttemptNotBeforeUtc timestamptz NULL`; `ResolutionDeadlineUtc timestamptz NULL set-once/gen`;
`CleanupAttemptCount bigint NOT NULL DEFAULT 0 server`; `NextCleanupAttemptNotBeforeUtc timestamptz NULL`; `CleanupDeadlineUtc
timestamptz NULL set-once/gen`; `CleanupOperatorInterventionRequired boolean NOT NULL DEFAULT false`; `WrappedDekCiphertext
bytea NULL CHECK octet_length=32`; `WrappedDekNonce bytea NULL CHECK octet_length=12`; `WrappedDekTag bytea NULL CHECK
octet_length=16`; `WrappedDekMetadataDigest bytea NULL CHECK octet_length=32`; `RowRevision bigint NOT NULL DEFAULT 1
server(+1/non-idempotent)`; `PreparedAtUtc timestamptz NULL`; `RevokedAtUtc timestamptz NULL`; `RevocationReasonCode text NULL
(§M7)`; `CreatedAtUtc timestamptz NOT NULL immutable`; `UpdatedAtUtc timestamptz NOT NULL server`. No head abandonment-evidence
columns (event-only, §9). Per-row CHECKs enforce only head-local sparse predicates. Cross-table head/mapping compatibility is
enforced by the deferred constraint-trigger contract below; PostgreSQL CHECK constraints never read another table.

### 2c. Provider-operation mapping `tagekyc.raw_export_key_provider_operations` (34; owner deployer; direct DML denied)
PK `ProviderOperationId uuid`. Immutable identity: `KeyProviderId text NOT NULL`, `ProviderOperationToken text NOT NULL`,
`AttemptKeyReservationId uuid NOT NULL` (FK head RESTRICT), `PreparationId uuid NOT NULL`, `PreparationFence bigint NOT NULL`,
`AttemptKeyContextFingerprint bytea NOT NULL CHECK octet_length=32`. Mutable `ProviderOperationState text NOT NULL ∈ {Issued,
ResultObserved, CleanupRequired, CleanedUp, AbsenceProven}` (no `Abandoned`). **Wrapped-result family (D — two lineages):**
`WrappedDekCiphertext(32)`, `WrappedDekNonce(12)`, `WrappedDekTag(16)`, `WrappedDekMetadataDigest(32)`, `WrappingSuiteId`,
`WrappingSuiteVersion`, `ProviderResourceReference`, `ProviderOperationReceipt`, `ResultObservedAtUtc` — REQUIRE-NON-NULL in
`ResultObserved`; in `CleanupRequired`/`CleanedUp` they are **EITHER**: **NULL** on the `Issued→CleanupRequired→CleanedUp`
lineage, **PRESERVED byte-identical** on the `ResultObserved→CleanupRequired→CleanedUp` lineage — never erased to satisfy a
CHECK. `ProviderCleanupReference text` REQUIRE-NON-NULL in CleanupRequired/CleanedUp; `ProviderCleanupReceipt text`
REQUIRE-NON-NULL in CleanedUp; `ProviderAbsenceProofReceipt text` REQUIRE-NON-NULL in AbsenceProven (else NULL). `IssuedAtUtc
timestamptz NOT NULL`, `UpdatedAtUtc timestamptz NOT NULL`. `uq_raw_export_key_provider_op_provider_token` (44)
UNIQUE(`KeyProviderId`,`ProviderOperationToken`); `uq_raw_export_key_provider_op_reservation_fence` UNIQUE
(`AttemptKeyReservationId`,`PreparationFence`) makes the governing/latest generation deterministic. Legal UPDATE edges: `Issued→ResultObserved`, `Issued→AbsenceProven`,
`{Issued,ResultObserved}→CleanupRequired`, `CleanupRequired→CleanedUp`. `Issued` inserted only by prepare. A CHECK enforces the
two-lineage wrapped-family rule (#53/#54). **Cross-table compatibility:** owner-only
`tagekyc.raw_export_enforce_attempt_key_pair()` is attached as two `AFTER INSERT OR UPDATE FOR EACH ROW CONSTRAINT TRIGGER`s:
`trg_raw_export_attempt_key_pair_from_head` on the head and `trg_raw_export_attempt_key_pair_from_operation` on the mapping,
both `DEFERRABLE INITIALLY DEFERRED`. At constraint time it reads the head and the mapping row whose
`PreparationFence = head.CurrentPreparationFence` for that reservation (the UNIQUE above makes it singular), derives the exact
pair, and rejects a missing mapping or every pair not emitted by the executable state model with `P0001
RAW_EXPORT_KEY_STATE_PAIR_INVALID`. Both triggers are required because a
transaction may touch either side; ordinary per-table sparse CHECKs run first. Mutation proof removes each trigger separately;
apply/rollback/reapply proves both trigger identities and deferrability.

### 2d. History `tagekyc.raw_export_attempt_key_preparation_events` (41; owner deployer; append-only)
PK `PreparationEventId uuid`; FK `AttemptKeyReservationId`→head RESTRICT. Columns: `PreparationId uuid NOT NULL`;
`PreparationFence bigint NOT NULL`; `EventSequence bigint NOT NULL`; `EventKind text NOT NULL`; `ResolutionKind text NULL`;
`CleanupResultKind text NULL`; `ProviderOperationToken text NULL`; `ProviderOperationReceipt text NULL`;
`ProviderCleanupReference text NULL`; `ProviderCleanupReceipt text NULL`; `ProviderResolutionEvidenceDigest bytea NULL CHECK
octet_length=32`; `ProviderCleanupEvidenceDigest bytea NULL CHECK octet_length=32`; `CleanupObservationEvidenceDigest bytea NULL
CHECK octet_length=32`; `WrappedDekMetadataDigest bytea NULL CHECK octet_length=32`; `WrappingSuiteId text NULL`;
`WrappingSuiteVersion integer NULL`; `RevocationReasonCode text NULL`; `RevocationEvidenceDigest bytea NULL CHECK
octet_length=32`; `OperatorReasonCode text NULL`; `RequestingActorEvidence text NULL`; `FinalizingActorEvidence text NULL`;
`AbandonRequestPreparationEventId uuid NULL`; `AbandonmentEvidenceDigest bytea NULL CHECK octet_length=32`; `HeadRowRevision
bigint NULL`; `EventAtUtc timestamptz NOT NULL`. `EventKind ∈ {Opened, Expired, DirectActivated, RecoveredActivated,
ResolvedNoResult, ResolvedOutcomeUnknown, ResolvedCorrupt, ResolvedCleanupRequired, ProviderUnavailableObserved,
CleanupAttemptObserved, CleanupAcknowledged, AbandonRequested, ProviderOperationAbandoned, Revoked}` (14). `EventSequence`
allocated atomically under the locked head, starts at 1, +1/event; `uq_raw_export_attempt_key_event_sequence` (40)
UNIQUE(`AttemptKeyReservationId`,`EventSequence`). **Singleton partial UNIQUE INDEX (F — exact):**
```
CREATE UNIQUE INDEX uq_raw_export_attempt_key_event_singleton                     -- 41 bytes
  ON tagekyc.raw_export_attempt_key_preparation_events
  ("AttemptKeyReservationId","PreparationId","PreparationFence","EventKind")
  WHERE "EventKind" IN
    ('Opened','Expired','DirectActivated','RecoveredActivated','CleanupAcknowledged','AbandonRequested',
     'ProviderOperationAbandoned','Revoked');
```
Because the index key includes `PreparationId`+`PreparationFence`, a **new** preparation generation (new PreparationId/fence+1)
MAY emit its own singleton event, while the **same** generation cannot emit a duplicate (#57). Repeatable
(not in the index): `ResolvedNoResult`,`ResolvedOutcomeUnknown`,`ResolvedCorrupt`,`ResolvedCleanupRequired`,
`ProviderUnavailableObserved`,`CleanupAttemptObserved`. Append-only: direct INSERT/UPDATE/DELETE denied (§M2). **Self-FK
(lineage):** `AbandonRequestPreparationEventId` → `fk_raw_export_abandon_request_event` (35) `FOREIGN KEY
("AbandonRequestPreparationEventId") REFERENCES tagekyc.raw_export_attempt_key_preparation_events ("PreparationEventId") ON DELETE RESTRICT`. Per-EventKind NON-NULL = §M1.3.

## 3. Provider port + application interfaces (A — recovery/cleanup contract restored)
Infrastructure-internal `IKekOperationProvider` (durable + idempotent by `providerOperationToken`): `WrapDekAsync(KekReference,
string providerOperationToken, ReadOnlyMemory<byte> attemptKeyContextFingerprint, IAttemptDekCandidate, CancellationToken) →
KekWrapResult` (CreateOrGet; same token → identical wrapped result, never a second DEK); `LookupByOperationTokenAsync(string
providerOperationToken, ReadOnlyMemory<byte> attemptKeyContextFingerprint, CancellationToken) → KekOperationLookup ∈
{Found{ciphertext,nonce,tag,suiteId,suiteVersion,providerResourceReference,receipt}, PositivelyAbsent{absenceProofReceipt},
Unknown, CorruptOrUnverifiable}`; `UnwrapDekAsync(...)`. **Restored internal recovery/cleanup contract (no new provider
semantics):**
```
internal interface IKekProvisioningRecoveryOperation {
  Task<KekProvisioningResolution> ResolveProvisioningOperationAsync(
    string providerOperationToken, ReadOnlyMemory<byte> attemptKeyContextFingerprint, CancellationToken);
  Task<KekProvisioningCleanupResult> CleanupProvisioningOperationAsync(
    string providerCleanupReference, ReadOnlyMemory<byte> attemptKeyContextFingerprint, CancellationToken);
}
```
`KekProvisioningResolution ∈ {WrappedResultRecovered{ciphertext,nonce,tag,suiteId,suiteVersion,providerResourceReference,
receipt}, NoProviderResult{absenceProofReceipt}, ProviderOutcomeUnknown, ProviderResourceCleanupRequired{cleanupReference},
ProviderUnavailable, CorruptOrUnverifiable}` — the CALLABLE PRODUCER of the resolution the transition graph consumes (T7–T14,
T20, T28 and T33). `KekProvisioningCleanupResult ∈ {Cleaned{receipt}, AlreadyAbsent{receipt}, CleanupUnavailable,
CleanupOutcomeUnknown, CleanupFailed}` — produced by the trusted provider adapter and mapped to T15–T19/T23–T27. **Binding:**
`ProviderCleanupReference` passed to `CleanupProvisioningOperationAsync` is the exact reference persisted at cleanup-required
entry (§2c), bound to `AttemptKeyContextFingerprint` + the current preparation generation. Cancellation is cooperative but
does **not** prove provider-side non-execution: a cancelled/lost response is `CleanupOutcomeUnknown` and is reconciled by an
idempotent repeat under the same cleanup reference. **Authority boundary:** the application obtains cleanup kind/receipt from
`IKekProvisioningRecoveryOperation.CleanupProvisioningOperationAsync`; `ack_cleanup` is reconciler-only (ACL) and rejects any
`ProviderCleanupReference` not equal to the persisted CleanupRequired reference (`EvidenceMissing`/`StateConflict`). SQL
recomputes `ProviderCleanupEvidenceDigest` over that matched reference (#51), but cannot cryptographically prove that cleanup
occurred. A principal holding the reconciler capability is trusted for the provider-result assertion; the proof is exclusion of
all other callers plus exact-reference binding, not proof of provider provenance against a compromised reconciler. Application-facing (Contracts;
no lease/DEK crosses): `IAttemptKeyReservationProvisioningOperation.ProvisionAsync` (32-byte DEK candidate → WrapDek →
persist+activate via SD → zeroize on success/failure → typed outcome, never the DEK); `IAttemptAeadEncryptionOperation.
EncryptBoundedChunkAsync` (Active only); `IAttemptAeadVerificationOperation.DecryptAndVerifyBoundedChunkAsync` (Active only; no
new lease after Revoked). Internal DEK lease: confined, non-assignable, zeroize-on-dispose; never returned to Contracts.

## 4. SQL function manifest (all SD, owner `tagekyc_raw_export_deployer`, `SET search_path = pg_catalog`, one overload, REVOKE ALL FROM PUBLIC + tagekyc_runtime; Up creates / Down drops; every row has exactly 5 columns — N)
| Function (schema-qualified · ordered typed params) | RETURNS | EXECUTE grant | Business outcomes | Transitions |
|---|---|---|---|---|
| `raw_export_prepare_attempt_key_reservation(p_attempt_key_reservation_id uuid, p_attempt_id uuid, p_source_artifact_id uuid)` | `TABLE(outcome text, provider_operation_id uuid, preparation_id uuid, preparation_fence bigint, provider_operation_token text, preparation_lease_expires_at_utc timestamptz, attempt_key_context_fingerprint bytea, key_provider_id text, kek_id text, kek_version integer, kek_fingerprint text, wrapping_suite_id text, wrapping_suite_version integer)` | `tagekyc_raw_export_custody_encryptor` | PreparingLive,ExistingMatch,InProgress,Conflict,HeadNotReserved,Terminated,StaleFence | T1,T2,T3,T38 |
| `raw_export_record_key_provider_wrapped_result(p_provider_operation_id uuid, p_attempt_key_reservation_id uuid, p_preparation_id uuid, p_preparation_fence bigint, p_provider_operation_token text, p_wrapped_dek_ciphertext bytea, p_wrapped_dek_nonce bytea, p_wrapped_dek_tag bytea, p_wrapping_suite_id text, p_wrapping_suite_version integer, p_provider_operation_receipt text, p_provider_resource_reference text)` | `text` | `tagekyc_raw_export_custody_encryptor` | ResultObserved,ExistingMatch,ShapeInvalid,SuiteMismatch,StaleOperation,StateConflict | T4 |
| `raw_export_activate_attempt_key_reservation(p_attempt_key_reservation_id uuid, p_preparation_id uuid, p_preparation_fence bigint)` | `text` | `tagekyc_raw_export_custody_encryptor` | Activated,AlreadyActivated,StalePreparation,DigestMismatch,HeadNotCurrent,HeadNotReserved,Terminated | T5,T36 |
| `raw_export_mark_attempt_key_preparation_expired(p_attempt_key_reservation_id uuid, p_preparation_id uuid, p_preparation_fence bigint)` | `text` | `tagekyc_raw_export_reconciler` | Expired,NotDue,StalePreparation | T6 |
| `raw_export_resolve_attempt_key_provider_outcome(p_attempt_key_reservation_id uuid, p_preparation_id uuid, p_preparation_fence bigint, p_resolution text, p_provider_cleanup_reference text, p_provider_operation_receipt text, p_provider_absence_proof_receipt text)` | `text` | `tagekyc_raw_export_reconciler` | ResolvedOutcomeUnknown,ProviderUnavailableObserved,ResolvedNoResult,ResolvedCorrupt,DeadlineExceeded,RetryTooSoon,StalePreparation,IllegalResolution | T7,T8,T9,T10,T11,T12,T13,T28,T33,T39 |
| `raw_export_mark_key_provider_cleanup_required(p_provider_operation_id uuid, p_attempt_key_reservation_id uuid, p_preparation_id uuid, p_preparation_fence bigint, p_provider_operation_token text, p_provider_cleanup_reference text)` | `text` | `tagekyc_raw_export_reconciler` | CleanupRequired,ExistingMatch,StaleOperation,StateConflict | T14,T22 |
| `raw_export_record_key_provider_cleanup_observation(p_provider_operation_id uuid, p_attempt_key_reservation_id uuid, p_preparation_id uuid, p_preparation_fence bigint, p_provider_operation_token text, p_cleanup_result_kind text, p_provider_cleanup_reference text, p_provider_cleanup_receipt text)` | `text` | `tagekyc_raw_export_reconciler` | CleanupObserved,RetryTooSoon,DeadlineIntervention,StaleOperation,IllegalCleanupKind | T15,T16,T17,T23,T24,T25,T35 |
| `raw_export_acknowledge_key_provider_cleanup(p_provider_operation_id uuid, p_attempt_key_reservation_id uuid, p_preparation_id uuid, p_preparation_fence bigint, p_provider_operation_token text, p_cleanup_result_kind text, p_provider_cleanup_reference text, p_provider_cleanup_receipt text)` | `text` | `tagekyc_raw_export_reconciler` | Acknowledged,EvidenceMissing,IllegalCleanupKind,StaleOperation,StateConflict | T18,T19,T26,T27 |
| `raw_export_record_recovered_key_provider_result(p_attempt_key_reservation_id uuid, p_preparation_id uuid, p_preparation_fence bigint, p_provider_operation_token text, p_wrapped_dek_ciphertext bytea, p_wrapped_dek_nonce bytea, p_wrapped_dek_tag bytea, p_wrapping_suite_id text, p_wrapping_suite_version integer, p_provider_resource_reference text, p_provider_operation_receipt text)` | `text` | `tagekyc_raw_export_reconciler` | RecoveredActivated,ShapeInvalid,SuiteMismatch,StaleOperation,StateConflict | T20 |
| `raw_export_request_abandon_attempt_key_reservation(p_attempt_key_reservation_id uuid, p_operator_reason_code text)` | `text` | `tagekyc_raw_export_lifecycle` | AbandonRequested,AlreadyRequested,StateConflict | T21 |
| `raw_export_finalize_abandon_attempt_key_reservation(p_attempt_key_reservation_id uuid)` | `text` | `tagekyc_raw_export_lifecycle` | ReservationAbandoned,CleanupNotComplete,NotRequested,AlreadyAbandoned,StateConflict | T29,T30,T31,T37,T40 |
| `raw_export_revoke_attempt_key_reservation(p_attempt_key_reservation_id uuid, p_revocation_reason_code text)` | `text` | `tagekyc_raw_export_reconciler`, `tagekyc_raw_export_lifecycle` | Revoked,AlreadyRevoked,NotActive | T32 |
| `raw_export_inspect_attempt_key_reservation(p_attempt_key_reservation_id uuid)` | `TABLE(preparation_disposition text, attempt_id uuid, current_preparation_id uuid, current_preparation_fence bigint, wrapped_dek_metadata_digest bytea)` | `tagekyc_raw_export_custody_encryptor`, `tagekyc_raw_export_reconciler` | rows-or-none | read |
| `raw_export_read_current_attempt_key_recovery_context(p_attempt_key_reservation_id uuid)` | `TABLE(preparation_disposition text, attempt_id uuid, attempt_key_context_fingerprint bytea, current_preparation_id uuid, current_preparation_fence bigint, current_preparation_lease_expires_at_utc timestamptz, current_provider_operation_token text, resolution_attempt_count bigint, next_resolution_attempt_not_before_utc timestamptz, resolution_deadline_utc timestamptz, cleanup_attempt_count bigint, next_cleanup_attempt_not_before_utc timestamptz, cleanup_deadline_utc timestamptz, cleanup_operator_intervention_required boolean, provider_operation_id uuid, provider_operation_state text, key_provider_id text, kek_id text, kek_version integer, kek_fingerprint text, wrapping_suite_id text, wrapping_suite_version integer, wrapped_dek_ciphertext bytea, wrapped_dek_nonce bytea, wrapped_dek_tag bytea, wrapped_dek_metadata_digest bytea, provider_resource_reference text, provider_operation_receipt text, provider_cleanup_reference text, provider_cleanup_receipt text, provider_absence_proof_receipt text, latest_resolution_kind text, latest_resolution_evidence_digest bytea, latest_cleanup_result_kind text, latest_cleanup_observation_evidence_digest bytea, abandon_request_preparation_event_id uuid, abandonment_operator_reason_code text, requesting_actor_evidence text, finalizing_actor_evidence text, abandonment_head_row_revision bigint, abandonment_event_at_utc timestamptz, abandonment_evidence_digest bytea)` | `tagekyc_raw_export_reconciler` | rows-or-none | read |
| `raw_export_read_active_attempt_key_envelope(p_attempt_key_reservation_id uuid)` | `TABLE(attempt_id uuid, attempt_key_context_fingerprint bytea, key_provider_id text, kek_id text, kek_version integer, kek_fingerprint text, wrapping_suite_id text, wrapping_suite_version integer, wrapped_dek_ciphertext bytea, wrapped_dek_nonce bytea, wrapped_dek_tag bytea, wrapped_dek_metadata_digest bytea)` | `tagekyc_raw_export_custody_encryptor`, `tagekyc_raw_export_reconciler` | active-row-or-none; Revoked/terminal never returned | read |

**Internal helpers (owner-only; NOT callable):** `raw_export_activate_recovered_attempt_key_reservation_internal` (62) —
invoked ONLY inside `raw_export_record_recovered_key_provider_result`; it REVOKEs ALL EXECUTE from PUBLIC, `tagekyc_runtime`,
`tagekyc_raw_export_custody_encryptor`, `tagekyc_raw_export_reconciler` and `tagekyc_raw_export_lifecycle` (finding A). The v0.6
externally-granted `raw_export_activate_recovered_attempt_key_reservation` surface does NOT exist (no orphan overload/grant;
#20. `raw_export_enforce_attempt_key_pair()` is the deferred constraint-trigger function from §2c; zero non-owner EXECUTE,
exactly two non-internal constraint-trigger bindings, and no direct application call.
**prepare** locks the landed attempt+source head; proves same attempt/`SourceArtifactId`/`CustodyState=Reserved`/not
R2-terminated/deadlines valid; computes `AttemptKeyContextFingerprint` via the landed helper; generates
`ProviderOperationId=pg_catalog.gen_random_uuid()` and the token per §6c; INSERTs head+`Opened`+`Issued` atomically; accepts no
caller selector/suite/fingerprint/token. Its return carries the exact server-generated operation ID/context and frozen
provider/KEK/suite selectors needed by `WrapDekAsync` and `record_wrapped`; no direct mapping-table read is required. `KekReference`
is constructed only from the returned frozen provider/KEK fields. `PreparingLive`, `ExistingMatch` and `InProgress` return the
same complete frozen context for the governing generation; rejection/terminal outcomes return all context columns NULL.
**Lifecycle resolution:** request/finalize accept only the
reservation ID (plus request reason), lock the head and resolve its current preparation/fence/token internally; lifecycle needs
no read grant and restart does not depend on process memory. **Active envelope:** the exact active-only SD read returns the
wrapped envelope + frozen provider context required by AEAD unwrap, never a plaintext DEK and never a Revoked/terminal row.
**acknowledge cleanup (D)** takes NO `p_provider_cleanup_evidence_digest`; SQL
recomputes `ProviderCleanupEvidenceDigest`. **record_recovered (A/B trust)** validates wrapped shape + persisted suite,
recomputes MetadataDigest+ResolutionEvidence, no caller digest, no SQL byte-compare against an unknown original.

## 5. Roles / ACL topology
Capability roles (`NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS`, `ADMIN OPTION=false`):
`tagekyc_raw_export_custody_encryptor`, `tagekyc_raw_export_reconciler`, `tagekyc_raw_export_lifecycle`. Deployment LOGIN roles:
`tagekyc_raw_export_encryptor_login`, `tagekyc_raw_export_reconciler_login`, `tagekyc_raw_export_lifecycle_login` (`LOGIN
NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS`, one LOGIN→one capability role, `INHERIT=true`,`SET=false`,`ADMIN
OPTION=false`; no cross-membership). `USAGE ON SCHEMA tagekyc` to the three capability roles; no direct table privilege; the
internal helper has zero non-owner EXECUTE. **Effective-ACL proof (K):** the readiness/ArchTest verifier uses
`aclexplode(coalesce(pg_proc.proacl, acldefault('f', pg_proc.proowner)))` over `pg_catalog` (NOT
`information_schema.role_routine_grants`, which omits PUBLIC-derived access) and REJECTS: grantee OID 0 (PUBLIC); any
runtime/capability grantee outside the exact per-function allowlist; unexpected grantors; and the `proacl IS NULL` default-ACL
case whenever it implies unwanted PUBLIC EXECUTE (#20,#37). Down: revoke every exact privilege/default-privilege, DROP only the
three migration-owned capability roles, preserve deployment LOGIN roles and the deployment-owned `pgcrypto` + `tagekyc_extensions`
(§6c); no broad `DROP OWNED BY`.

## 6. Readiness (topology + configuration + CSPRNG)
### 6a. Topology configuration
Key **`TagEkyc:RawExport:AttemptKey:Topology`**, case-sensitive literals `ProcessLocalFixture` | `DurableKey`. Whitespace
trimmed; empty/missing/unknown → fail-closed `PROD_RAW_EXPORT_PROVIDER_TOPOLOGY_INVALID` (NO implicit default; never silently
`DurableKey`). Production + `ProcessLocalFixture` → rejected by the landed fixture-active code
`PROD_RAW_EXPORT_ATTEMPT_KEY_FIXTURE_ACTIVE`. `DurableKey` registers ONLY the durable validators; `ProcessLocalFixture` ONLY the
landed `AttemptKeyReadinessValidator`; sets never cross-fire. Parsed/owned by `DurableKeyTopologyOptions`; DI registers exactly
one set; the aggregate iterates only the registered set.
### 6b. Active DK-PROD codes + precedence (K — legacy fixture codes counted separately)
`CustodyRoleReadinessValidator` (**6**): `PROD_RAW_EXPORT_CUSTODY_ROLE_MISSING → _ROLE_ATTRIBUTE_INVALID →
_LOGIN_ATTRIBUTE_INVALID → _ROLE_GRANT_INVALID → _ROLE_CROSS_MEMBERSHIP → _ROLE_SET_ROLE_ENABLED`.
`DurableKeyProviderReadinessValidator` (**6**): `PROD_RAW_EXPORT_PROVIDER_TOPOLOGY_INVALID →
PROD_RAW_EXPORT_KEY_PROVIDER_DURABILITY_UNSUPPORTED → PROD_RAW_EXPORT_KEY_CSPRNG_UNAVAILABLE → PROD_RAW_EXPORT_KEK_NOT_QUALIFIED
→ PROD_RAW_EXPORT_HASH_HELPER_ACL_INVALID → PROD_RAW_EXPORT_KEY_RETRY_CONFIG_INVALID`. `KeyReservationReadinessValidator` (**9**):
`PROD_RAW_EXPORT_KEY_RESERVATION_STATE_INVALID → _KEY_PREPARATION_HISTORY_INVALID → _KEY_PREPARATION_LEASE_INVALID →
_KEY_OPERATION_RECOVERY_UNSUPPORTED → _KEY_PROVIDER_CLEANUP_UNSUPPORTED → _KEY_PROVIDER_CLEANUP_OUTCOME_UNKNOWN →
_KEY_PROVIDER_CLEANUP_OPERATOR_INTERVENTION → _KEY_PROVIDER_CORRUPT_UNRESOLVED → _KEY_ABANDON_PENDING`. **Active DK-PROD total =
6 + 6 + 9 = 21.** Preserved legacy/fixture (NOT counted in the 21): `PROD_RAW_EXPORT_ATTEMPT_KEY_PROFILE_MISSING`,
`_PROFILE_INVALID`, `_FIXTURE_ACTIVE` (**3**, ProcessLocalFixture topology only). A **pending** `AbandonRequested` fails via
`_KEY_ABANDON_PENDING`; a **completed** `ReservationAbandoned` and `Revoked` do NOT fail global readiness.
### 6c. CSPRNG contract (Option 1, ratified — L: production-owner literal is an activation gate, not a docs/build blocker)
Extension `pgcrypto` in controlled schema `tagekyc_extensions`; exact identity `tagekyc_extensions.gen_random_bytes(integer)
RETURNS bytea`. `prepare` calls exactly `tagekyc_extensions.gen_random_bytes(32)` — no unqualified `gen_random_bytes`,
non-shadowable (search_path=pg_catalog forces the qualification). Token rendering:
`rtrim(translate(encode(tagekyc_extensions.gen_random_bytes(32),'base64'),'+/','-_'),'=')` → exactly 43 ASCII base64url chars,
no padding. No caller token; application-side CSPRNG rejected for this slice. Readiness `PROD_RAW_EXPORT_KEY_CSPRNG_UNAVAILABLE`
on: extension absent, function absent, wrong schema, wrong signature, wrong owner, wrong ACL, unexpected PUBLIC/runtime/
capability EXECUTE, output length ≠ 32, token encoding invalid (catalog checks over `pg_extension`/`pg_proc`/`pg_namespace` +
the §5 effective-ACL proof). **The exact production extension-OWNER literal is deployment-specific and remains a
PRODUCTION-ACTIVATION gate (`tip_88c1_b2_durable_key_csprng_prereq.md`); it does NOT block docs reconciliation, proof-build, or
implementation against the approved dev/test topology.** Migration boundary (M): the migration performs a **non-mutating
prerequisite assertion** — it verifies the extension/function/schema/signature exist and does NOT install, relocate, alter
ownership of, or drop `pgcrypto`; Down preserves the extension + schema.

## 7. Recovery projection (reconciler-only)
`raw_export_read_current_attempt_key_recovery_context(uuid) RETURNS TABLE(...)` — current generation only, deterministic
latest-per-family, never generic history, SD, `search_path=pg_catalog`, one overload, EXECUTE only to
`tagekyc_raw_export_reconciler`. Families: overall latest; latest resolution (kind+digest); latest cleanup-required (reference);
latest cleanup-attempt (kind+digest); latest activation; current mapping state+evidence; both retry clocks + intervention flag;
latest-abandonment with DISTINCT actors: `abandon_request_preparation_event_id uuid, abandonment_provider_operation_token text,
abandonment_operator_reason_code text, requesting_actor_evidence text, finalizing_actor_evidence text,
abandonment_head_row_revision bigint, abandonment_event_at_utc timestamptz, abandonment_evidence_digest bytea`. `AbandonRequested`
-pending and terminal `ReservationAbandoned` fully reconstructable with no process memory.

## 8. Evidence / digests (all SQL-recomputed; 8 domains) + golden vectors
Domains: (1) `tip-88c1-attempt-key-context-v1`, (2) `tip-88c1-wrapped-dek-metadata-v1`,
(3) `tip-88c1-key-provider-resolution-evidence-v1`, (4) `tip-88c1-key-provider-cleanup-evidence-v1`,
(5) `tip-88c1-key-provider-absence-evidence-v1`, (6) `tip-88c1-key-cleanup-observation-evidence-v1`,
(7) `tip-88c1-key-revocation-evidence-v1`, (8) `tip-88c1-key-abandonment-evidence-v1`. **No new domain in v0.4 (H).**
`ResolvedNoResult` stores digest (3) = digest (5). `ProviderUnavailableObserved` recomputes and stores digest (3) (H — aligns
T9–T11). Ordered preimages (opt = tagged K1): (1) `{ReservationId[N],AttemptId[N],EncryptionAttemptFingerprint[hex],KeyProviderId,
KekId,KekVersion[dec],KekFingerprint,WrappingSuiteId,WrappingSuiteVersion[dec]}`; (2) `{ctx[hex],WrappingSuiteId,
WrappingSuiteVersion[dec],WrappedDekNonce[hex],WrappedDekCiphertext[hex],WrappedDekTag[hex]}`; (3) `{ctx[hex],PreparationId[N],
PreparationFence[dec],ResolutionKind,ProviderOperationToken,opt(ProviderOperationReceipt),opt(ProviderCleanupReference),
opt(WrappedDekMetadataDigest[hex])}`; (4) `{ctx[hex],PreparationId[N],PreparationFence[dec],CleanupResultKind,
ProviderCleanupReference,ProviderCleanupReceipt}`; (5) `{ctx[hex],PreparationId[N],PreparationFence[dec],"AbsenceProven",
ProviderOperationToken,ProviderAbsenceProofReceipt}`; (6) `{ctx[hex],PreparationId[N],PreparationFence[dec],
ProviderCleanupReference,CleanupResultKind,opt(ProviderCleanupReceipt)}`; (7) `{ctx[hex],PreparationId[N],PreparationFence[dec],
RevocationReasonCode}`; (8) `{ctx[hex],PreparationId[N],PreparationFence[dec],ProviderOperationToken,
AbandonRequestPreparationEventId[N-guid],OperatorReasonCode,RequestingActorEvidence,FinalizingActorEvidence,
FinalHeadRowRevision[dec]}`.
Golden vectors (fence=1; fixtures synthetic non-patient; unchanged from v0.2):
| Digest | Value |
|---|---|
| context | `37108746b02b4a73070aafdb116ceeb15ba5b18399286b92fe38c40fc86c7793` |
| wrapped-metadata | `607a19029096528f90db3fef24c5ba2229bf3cf86a5972932b00622503889d30` |
| resolution | `b30d9e7b6ce91af6f42bf826bd05daacc0c17bcb1409051b3517de5cc3f2c2dd` |
| cleanup | `d3f47850ae3cfa8cf233e85633b609201313bfa8d301b5689311f34e16dc54bb` |
| absence | `cab1a516ae8a2bd75fd260c529f2b35dd1a4d430a2d4f4ed9a733ea6bfd714d9` |
| cleanup-observation (ref,kind CleanupFailed,receipt absent) | `72c5086615ad0b49b5c8952dd6ea3d3dbfbe5c9a670aaf30f2550d07479969ed` |
| revocation | `af78d56210a6e7ba6cea7b0c05df1d1544742ebad279e94162d03a47cc223204` |
| abandonment (UUID reqRef) | `5c220fc2973bd847e8888dbfd4161fb1a80a17b24681cb68d9c1ef016c6f9776` |
```
python - <<'PY'
import hashlib,unicodedata,struct
def lp(s):
    b=unicodedata.normalize('NFC',s).encode('utf-8'); return struct.pack('>I',len(b))+b
def c1(d,*f):
    buf=lp(d)
    for x in f: buf+=lp(x)
    return hashlib.sha256(buf).hexdigest()
ctx=c1("tip-88c1-attempt-key-context-v1","11111111111141118111111111111111","22222222222242228222222222222222","ab"*32,"fixture-kek-local","fixture-kek-1","1","fixture-kek-fpr-v1","AES-256-GCM","1")
nonce="0102030405060708090a0b0c"; ct="".join(f"{b:02x}" for b in range(0x20,0x40)); tag="".join(f"{b:02x}" for b in range(0x40,0x50))
meta=c1("tip-88c1-wrapped-dek-metadata-v1",ctx,"AES-256-GCM","1",nonce,ct,tag)
p="33333333333343338333333333333333"
res=c1("tip-88c1-key-provider-resolution-evidence-v1",ctx,p,"1","WrappedResultRecovered","A"*43,"1","receipt-fixture-0001","0","1",meta)
cln=c1("tip-88c1-key-provider-cleanup-evidence-v1",ctx,p,"1","Cleaned","cleanup-ref-fixture-0001","cleanup-rcpt-fixture-0001")
absd=c1("tip-88c1-key-provider-absence-evidence-v1",ctx,p,"1","AbsenceProven","A"*43,"absence-rcpt-fixture-0001")
obs=c1("tip-88c1-key-cleanup-observation-evidence-v1",ctx,p,"1","cleanup-ref-fixture-0001","CleanupFailed","0")
rev=c1("tip-88c1-key-revocation-evidence-v1",ctx,p,"1","operator-revoke-fixture-0001")
ab=c1("tip-88c1-key-abandonment-evidence-v1",ctx,p,"1","A"*43,"44444444444444448444444444444444","operator-abandon-fixture-0001","operator-req-fixture-0001","operator-fin-fixture-0001","9")
for v in (ctx,meta,res,cln,absd,obs,rev,ab): print(v)
PY
```
**Event-field authority (H — two layers, replacing "every mandatory event field is digest-bound"):** every mandatory event
field is exactly one of — **(1) cryptographically digest-bound** (bound inside one of the 8 domains above: e.g. resolution
kind/token/receipt/cleanup-ref in (3); cleanup ref+kind+receipt in (4)/(6); revocation reason in (7); abandonment token/
reqRef/reasons/actors/revision in (8); wrapped ct/nonce/tag/suite in (2)); or **(2) append-only-protected** (immutable
append-only history row + guard, not inside any digest): e.g. `DirectActivated.ProviderOperationReceipt` (mandatory but NOT in
digest (2), which binds only ct/nonce/tag/suite) and `EventSequence`/`EventAtUtc`. No new domain is created for DirectActivated
(H); its receipt is authority-class (2).

## 9. Two-phase abandonment + lineage binding
Head `AbandonReq`/`ResvAbandoned` per §M1.1; no head abandonment-evidence columns. Per-EventKind NON-NULL (§M1.3):
`AbandonRequested` → `PreparationId,PreparationFence,ProviderOperationToken,OperatorReasonCode,RequestingActorEvidence,
HeadRowRevision` (`FinalizingActorEvidence,AbandonRequestPreparationEventId,AbandonmentEvidenceDigest,RevocationReasonCode`
NULL). `ProviderOperationAbandoned` → `PreparationId,PreparationFence,ProviderOperationToken,OperatorReasonCode,
RequestingActorEvidence,FinalizingActorEvidence,AbandonRequestPreparationEventId,AbandonmentEvidenceDigest,HeadRowRevision`
(`RevocationReasonCode` NULL). `finalize` locks the referenced `AbandonRequested` history row and verifies same
`AttemptKeyReservationId`/`PreparationId`/`PreparationFence`/`ProviderOperationToken` and `EventKind='AbandonRequested'`; stores
that exact UUID; the abandonment digest binds it in lowercase N-guid. Cross-reservation/cross-generation reference → self-FK +
guard reject AND recomputed digest diverges (#22k).

## M1. Sparse matrices (destination-state satisfiability — generated contract)
### M1.1 Pair-specific head shape
The normative shape map is `SHAPES` in `tip_88c1_b2_durable_key_prod_state_model.py`; a head-only matrix is prohibited because
`AR/IS`, `AR/RO`, `AR/CR`, `AR/CU` and `AR/AP` intentionally have different cleanup-clock requirements. Local head CHECKs encode
only predicates expressible from head columns; the deferred pair triggers in §2c enforce the cross-table pair.

Pinned rules checked by the model:
- `PL/IS`: preparation/lease/token present; resolution deadline/next are EITHER because provider lookup may be unavailable
  before lease expiry. `PL/RO` clears both resolution clocks. Cleanup clocks remain NULL.
- `PE/IS`: resolution deadline present; next-resolution is EITHER because T6 creates PE before the first provider observation.
- `POU/IS`: resolution deadline and next-resolution present.
- `PC/IS`: both resolution clocks EITHER (direct corruption may precede expiry; exhaustion follows it); no successor.
- `PCR/CR`: cleanup deadline present, cleanup-next EITHER; resolution clocks cleared on entry.
- `RF/{CU,AP}`: preparation/lease/token and all clocks cleared.
- `AC/RO` and `RV/RO`: wrapped head and PreparedAtUtc present; all retry/cleanup clocks cleared.
- `AR/{IS,RO}`: preparation/lease/token present; all clocks cleared. `AR/CR`: cleanup deadline present and cleanup-next EITHER.
  `AR/{CU,AP}`: all clocks cleared.
- `RA/{CU,AP}`: preparation id/token preserved for terminal lineage, lease and every retry/cleanup clock cleared.
- `ResolutionAttemptCount` and `CleanupAttemptCount` are value-shaped (`ZERO|POSITIVE|EITHER_COUNT`), never nullable. T1/T2 reset
  both to zero; resolution observations increment the former; cleanup observations increment the latter.
- `CleanupOperatorInterventionRequired` is value-shaped (`FALSE|TRUE|EITHER_BOOL`), never nullable. T35 sets true; T1/T2 and
  cleanup acknowledgment set false. No model `CLEAR` writes SQL NULL to this boolean.

Every state-changing edge in the model declares SET/CLEAR fields. `python ...state_model.py` rejects a SET into a destination
requiring NULL, a CLEAR into a destination requiring NON-NULL, an unknown field, duplicate edge ID, unreachable predecessor or
shape/reachability mismatch. SQL sparse predicates and SD-function deltas must be transcribed from this output, never inferred
from the operational summary.
### M1.2 Head immutability
Immutable after insert: `AttemptKeyReservationId,AttemptId,EncryptionAttemptFingerprint,KeyProviderId,KekId,KekVersion,
KekFingerprint,AttemptKeyContextFingerprint,WrappingSuiteId,WrappingSuiteVersion,CreatedAtUtc`. Server-generated (never
caller-set): `CurrentPreparationFence,RowRevision,all *AtUtc,AttemptKeyContextFingerprint,CurrentProviderOperationToken`.
Wrapped cols ∅→NON-NULL only at activation, immutable after. Per-function deltas = §1 Head-Δ.
### M1.3 History per-EventKind NON-NULL (others NULL)
`Opened`:`PreparationId,PreparationFence,ProviderOperationToken,WrappingSuiteId,WrappingSuiteVersion`. `Expired`:`PreparationId,
PreparationFence`. `DirectActivated`:`PreparationId,PreparationFence,ProviderOperationReceipt,WrappedDekMetadataDigest,
WrappingSuiteId,WrappingSuiteVersion`. `RecoveredActivated`:`PreparationId,PreparationFence,ProviderOperationReceipt,
WrappedDekMetadataDigest,WrappingSuiteId,WrappingSuiteVersion,ResolutionKind,ProviderResolutionEvidenceDigest`.
`ResolvedNoResult|ResolvedOutcomeUnknown|ResolvedCorrupt`:`PreparationId,PreparationFence,ResolutionKind,
ProviderResolutionEvidenceDigest`. `ResolvedCleanupRequired`:`PreparationId,PreparationFence,ResolutionKind,
ProviderResolutionEvidenceDigest,ProviderCleanupReference`. **`ProviderUnavailableObserved`:`PreparationId,PreparationFence,
ResolutionKind,ProviderResolutionEvidenceDigest` (H — digest now required).** `CleanupAttemptObserved`:`PreparationId,
PreparationFence,ProviderCleanupReference,CleanupResultKind,CleanupObservationEvidenceDigest`.
`CleanupAcknowledged`:`PreparationId,PreparationFence,CleanupResultKind,ProviderCleanupReference,ProviderCleanupReceipt,
ProviderCleanupEvidenceDigest`. `AbandonRequested`/`ProviderOperationAbandoned`: §9. `Revoked`:`PreparationId,PreparationFence,
RevocationReasonCode,RevocationEvidenceDigest`. Per-row CHECK keyed on `EventKind`.
### M1.4 Mapping state matrix (identity immutable; D two lineages)
`Issued`:RNN `IssuedAtUtc`; wrapped/cleanup/absence RN. `ResultObserved`:RNN wrapped family+`ResultObservedAtUtc`; cleanup/
absence RN. `AbsenceProven`:RNN `ProviderAbsenceProofReceipt`; wrapped/cleanup RN. `CleanupRequired`:RNN `ProviderCleanupReference`;
wrapped family **EITHER** (RN if Issued-lineage, PRESERVED if ResultObserved-lineage); absence RN. `CleanedUp`:RNN
`ProviderCleanupReference,ProviderCleanupReceipt`; wrapped family **EITHER** (same lineage rule, byte-identical if present);
absence RN. #53 (Issued-lineage → wrapped RN through CU), #54 (ResultObserved-lineage → wrapped byte-identical through CU).
### M1.5 Fixed current retry/lease profile (DI `DurableKeyCustodyOptions` is conformance input only)

The current DurableKey topology has exactly one operational timing profile:

| Field | Fixed current value |
|---|---:|
| `PreparationLeaseDuration` | 15 minutes |
| `ResolutionInitialRetryDelay` | 30 seconds |
| `ResolutionRetryMultiplier` | 2.0 |
| `ResolutionMaxBackoff` | 30 minutes |
| `ResolutionDeadline` | 24 hours |
| `ResolutionMaxAttemptCount` | 0 |
| `CleanupInitialRetryDelay` | 1 minute |
| `CleanupRetryMultiplier` | 2.0 |
| `CleanupMaxBackoff` | 1 hour |
| `CleanupDeadline` | 7 days |
| `BoundedAeadOperationDuration` | 30 seconds |

An absent configuration key resolves to its fixed value. A present value must parse and equal its fixed value exactly; malformed
or non-fixed values — including values inside the formerly documented validation ranges — fail readiness with
`PROD_RAW_EXPORT_KEY_RETRY_CONFIG_INVALID`. Configuration never influences operational timing, existing generations or SQL due/
deadline values; no `IOptionsMonitor`, hot reload, configuration table, SQL timing parameter, caller/environment GUC, overload or
runtime profile selection is permitted. SQL uses the fixed lease/retry/deadline/count literals and C# AEAD uses the fixed
30-second bound even while readiness is red. Backoff is `min(initial×multiplier^(attempt−1),maxBackoff)` and cannot disagree with
persisted SQL due/deadline values.

`ResolutionMaxAttemptCount = 0` means deadline-only current governance: T33 remains active; count-based terminalization is
`FUTURE_GATE` under `DK-PROD-DURABLE-CONFIG-SNAPSHOT-V1`. `ResolutionAttemptCount` remains persisted and increments only on real
provider observations (never `mark-expired`) for evidence, observability and retry/backoff calculation. Deadlines initialize once
per generation. The former ranges are reserved validation bounds for a future immutable configurable-profile topology only; they
do not authorize current-profile variation.

## M2. Write guards (G — exact relation names + route proofs)
Trigger `trg_raw_export_attempt_key_guard` (32) → `raw_export_attempt_key_guard()`. `TG_TABLE_SCHEMA='tagekyc'`; route by the
EXACT relation names `TG_TABLE_NAME ∈ {'raw_export_attempt_key_reservations','raw_export_attempt_key_preparation_events',
'raw_export_key_provider_operations'}` (one method: name-based). Requires `current_user='tagekyc_raw_export_deployer'` AND
transaction-local GUC `tagekyc.raw_export_attempt_key_write_context='active'`; missing/empty/nested → `P0001
RAW_EXPORT_KEY_WRITE_CONTEXT_INVALID`; prior GUC captured via `current_setting(...,true)`, restored on success+exception.
Allowed: head INSERT+per-function UPDATE-delta; history INSERT-only; mapping INSERT+per-§4-UPDATE-delta. UPDATE/DELETE on
history, DELETE on mapping, mapping UPDATE altering identity or under stale prep/fence/token, and any direct DML without the GUC
are rejected. Proofs: one positive route per table (#55a/#55b/#55c) + one unknown-table fail-closed (#56, a spoofed
`TG_TABLE_NAME` outside the set raises `P0001`); positive control + spoofed-GUC-fails-on-current_user (#34,#35,#36).

The write guard controls authorized deltas; it does not replace cross-table integrity. The two deferred constraint triggers in
§2c run after the SD function has produced its final head+mapping pair. Tests #58a/#58b remove one trigger at a time, perform an
owner-authorized one-sided mutation, and require real COMMIT to fail when both triggers are present and to succeed (test RED)
under the scratch mutation. Restoration proves both trigger definitions byte-identical.

## M6. Production test manifest (J — honest categorized census; only mutation-proof rows carry a scratch mutation)
Categories: **MUT** (mutation-proof: scratch mutation + RED assertion + byte-identical restoration), **CAT** (catalog
invariant), **MIG** (migration up/down), **POS** (positive readiness/behavior). FQNs live in
`Tip88C1B2DurableKeyProdTests` / `Tip88C1B2DurableKeyProdArchTests`.
- Transition/state MUT (each disables the §1 branch named in Table row, asserts the exact RED outcome/state): #1,#2,#3,#4,#5,
  #6,#7,**#8(POU→POU, C1)**,#9a,#9b,#9c,**#12(direct-corrupt, B — distinct from active #25 deadline exhaustion)**,#10,#11,#13,#14,#15,#16,
  #17,#18,#19,#20,#21,#22a,#22b,#22c,#22d,#22e,#22f,#22g,#22h,#22i,#22j,**#22k(cross-reservation lineage)**,#23,#24,#25,#27,
  #28,#29,#30,#31,#32.
- Finding-specific MUT: **#17b** `CleanupAck_NoCallerDigest_RecomputedAuthorityBites` (change fn to trust a supplied/tampered
  digest → evidence readback ≠ golden → RED; NOT a compile error); **#51** `Cleanup_CallerCannotForgeSuccess` (ack with a
  `ProviderCleanupReference` not matching the persisted CleanupRequired reference → `EvidenceMissing`/`StateConflict`; a
  non-reconciler caller → 42501; this proves capability exclusion + reference binding, NOT provenance against the trusted
  reconciler); **#53** `Cleanup_IssuedLineage_WrappedFamilyStaysNull`; **#54**
  `Cleanup_ResultObservedLineage_WrappedFamilyByteIdentical`.
- Guard/integrity MUT: #34,#35,#36,**#56** `WriteGuard_UnknownTable_FailsClosed`; **#58a**
  `PairConstraint_HeadTrigger_BitesAtCommit`; **#58b** `PairConstraint_MappingTrigger_BitesAtCommit`.
- FK MUT: #33 `LandedAttempt_FK_23503`.
- CAT (no scratch mutation required — J): **#17c** `CleanupAck_CallerDigestParam_AbsentInCatalog` (signature/overload);
  **#20b** `Recovered_SingleCanonicalSurface`; #37 `Roles_Attributes_NoCrossMembership` (effective-ACL); #49
  `Identifiers_AllUnder63_CatalogRoundTrip`; **#55a/#55b/#55c** `WriteGuard_RoutesReservations/Events/Mapping` (positive route
  proofs); **#57** `EventSingleton_IsPerPreparationGeneration`.
- POS readiness: #38 `Readiness_Topology_FailClosed_NoSilentDurable`; #39 `Readiness_QualifiedDurable_NotHitByFixtureCodes`;
  #40 `Csprng_MissingExtension`; #41 `Csprng_WrongSchema`; #42 `Csprng_WrongOwnerOrAcl`; #43 `Csprng_ShadowFunction`; #44
  `Csprng_OutputLengthNot32`; #45 `Csprng_TokenLengthNot43`; #46 `Csprng_InvalidBase64UrlChar`; #47 `Csprng_CallerTokenPath_
  Rejected`; #48 `RetryConfig_FixedProfileConformance` (count-neutral POS+MUT: exact fixed profile and absent keys pass; malformed,
  every in-range-but-non-fixed value and each individual field mismatch fail with `PROD_RAW_EXPORT_KEY_RETRY_CONFIG_INVALID`;
  operational values remain fixed while readiness is red; accepting one non-fixed in-range value in scratch turns #48 RED and
  is restored byte-identically).
- MIG: #50 `Migration_DownReapply_NoOrphan_ExtensionPreserved`.
- Execution closure: **#59** `ResultObserved_LeaseExpiry_Restart_Activates`; **#60**
  `CapabilityRoles_PrepareWrapRecordActivateRestartReadUnwrap_NoTableGrant`; **#61**
  `Lifecycle_Restart_ResolvesCurrentGenerationWithoutReadGrant`; **#62**
  `FreshGeneration_ResetsBothAttemptCountsAndInterventionFalse`.

### M6f. Future-gated test manifest — `DK-PROD-DURABLE-CONFIG-SNAPSHOT-V1`

`FUTURE_GATE #26` is reserved for `MaxAttemptExceeded_CurrentImmutableProfile` and is not an active test method while
`ResolutionMaxAttemptCount = 0`. It may not be exercised through test-only configuration, SQL parameters, GUCs, environment
overrides, mutation-only non-production values or an undeclared profile. Test #27 and all later identifiers remain unchanged.

**Exact active test-method census (recomputed from the labels above):** transition/state **43** (#1–#8 = 8; #9a–#9c = 3;
#10–#21 = 12; #22a–#22k = 11; active #23–#32 excluding `FUTURE_GATE #26` = 9) + finding MUT 4 (#17b,#51,#53,#54) + guard/integrity MUT 6
(#34,#35,#36,#56,#58a,#58b) + FK MUT 1 (#33) + CAT 8 (#17c,#20b,#37,#49,#55a,#55b,#55c,#57) + POS 11 (#38–#48) + MIG 1
(#50) + execution-closure 4 (#59–#62) = active test methods **78** (fixture tests are NOT counted; they belong to
DK-FIXTURE-PROOF).

## M7. Text bounds + handle redaction (F — restored, v0.4 field list)
For every `KeyProviderId, KekId, KekFingerprint, WrappingSuiteId, ProviderOperationToken, ProviderOperationReceipt,
ProviderCleanupReference, ProviderCleanupReceipt, ProviderResourceReference, ProviderAbsenceProofReceipt, RevocationReasonCode,
OperatorReasonCode, RequestingActorEvidence, FinalizingActorEvidence`: NFC-normalized + trimmed; non-empty; ≤512 UTF-8 bytes
(`ProviderOperationToken` additionally = exactly 43-char base64url, §6c, server-generated, never caller-supplied);
control-characters (`U+0000..U+001F`,`U+007F`) rejected by a DB CHECK (`value !~ '[\x00-\x1f\x7f]'`) and a C# validator.
`ProviderOperationToken` + provider handles are internal opaque types with a redacted `ToString()` (fixed tag, never the
value); never plaintext-logged, never a metric label, never in a readiness payload, never in a public API. `RequestingActorEvidence`/
`FinalizingActorEvidence` are trusted-derived (`raw_export_current_actor()`), not caller free text (§9). Token lifetime = the
current preparation generation.

## M8. Production-only file allowlist (Part G — any file outside is STOP/RRI)
Documentation tooling: `docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_b2_durable_key_prod_state_model.py`
(normative model/checker; no runtime import).
Contracts: `src/TagEkyc.Contracts/RawExport/AttemptKeyReservationContracts.cs`,
`src/TagEkyc.Contracts/RawExport/AttemptAeadOperationContracts.cs`. Infrastructure:
`src/TagEkyc.Infrastructure/RawExport/KekOperationContracts.cs`,
`src/TagEkyc.Infrastructure/RawExport/KekProvisioningRecoveryOperation.cs`,
`src/TagEkyc.Infrastructure/RawExport/PostgresAttemptKeyReservationProvider.cs`,
`src/TagEkyc.Infrastructure/RawExport/PostgresKeyProviderOperationMap.cs`,
`src/TagEkyc.Infrastructure/RawExport/AttemptKeyRecoveryContextReader.cs`,
`src/TagEkyc.Infrastructure/RawExport/AttemptAeadOperationService.cs`,
`src/TagEkyc.Infrastructure/RawExport/AttemptDekLease.cs`,
`src/TagEkyc.Infrastructure/RawExport/DurableKeyCustodyServiceCollectionExtensions.cs`,
`src/TagEkyc.Infrastructure/RawExport/DurableKeyCustodyOptions.cs`,
`src/TagEkyc.Infrastructure/RawExport/DurableKeyTopologyOptions.cs`,
`src/TagEkyc.Infrastructure/RawExport/CustodyRoleReadinessValidator.cs`,
`src/TagEkyc.Infrastructure/RawExport/DurableKeyProviderReadinessValidator.cs`,
`src/TagEkyc.Infrastructure/RawExport/KeyReservationReadinessValidator.cs`,
`src/TagEkyc.Infrastructure/RawExport/CsprngReadinessValidator.cs`,
`src/TagEkyc.Infrastructure/Persistence/Entities/RawExportAttemptKeyReservationRow.cs`,
`src/TagEkyc.Infrastructure/Persistence/Entities/RawExportAttemptKeyPreparationEventRow.cs`,
`src/TagEkyc.Infrastructure/Persistence/Entities/RawExportKeyProviderOperationRow.cs`,
`src/TagEkyc.Infrastructure/Persistence/Configurations/RawExportAttemptKeyReservationConfig.cs`,
`src/TagEkyc.Infrastructure/Persistence/Configurations/RawExportAttemptKeyPreparationEventConfig.cs`,
`src/TagEkyc.Infrastructure/Persistence/Configurations/RawExportKeyProviderOperationConfig.cs`,
`src/TagEkyc.Infrastructure/Persistence/TagEkycDbContext.cs`,
`src/TagEkyc.Infrastructure/Persistence/Migrations/{UTCyyyyMMddHHmmss}_Tip88C1B2DurableKeyProd.cs`,
`src/TagEkyc.Infrastructure/Persistence/Migrations/{UTCyyyyMMddHHmmss}_Tip88C1B2DurableKeyProd.Designer.cs`,
`src/TagEkyc.Infrastructure/Persistence/Migrations/TagEkycDbContextModelSnapshot.cs`,
`src/TagEkyc.Infrastructure/TagEkyc.Infrastructure.csproj`. API: `src/TagEkyc.Api/Program.cs`,
`src/TagEkyc.Api/ReadinessEndpoint.cs`, `src/TagEkyc.Api/appsettings.json`. Tests + docs:
`tests/TagEkyc.IntegrationTests/Tip88C1B2DurableKeyProdTests.cs`, `tests/TagEkyc.ArchTests/Tip88C1B2DurableKeyProdArchTests.cs`,
`docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_b2_durable_key_prod_as_built.md`,
`docs/phase1_scope_and_debt_registry_v0_1.md`. One E3 constant (additive-snapshot-proven):
`tests/TagEkyc.IntegrationTests/Tip88B1E3ResolverReadBoundaryTests.cs`. **EXCLUDED (DK-FIXTURE-PROOF):** fixture journal
entity/config, fixture provider adapter, fixture-only tests, DK-FIXTURE-PROOF as-built, object-store/R2 files. **Count: 36
authorized files (35 source/test/as-built/debt files incl. 1 E3 constant, plus 1 documentation checker).**

## M9. Up/Down order + census
**Up:** (1) NON-MUTATING prerequisite assertion of `tagekyc_extensions.gen_random_bytes(integer)` (verify only — does NOT
install/relocate/alter-owner/drop pgcrypto, M); (2) capability roles + memberships; (3) add the landed-attempt AK; (4) create
the head table; (5) create the composite FK on the head table, either inline during `CREATE TABLE` or explicitly afterward;
(6) create the history and mapping tables; (7) create the remaining indexes/local CHECKs/self-FKs (including mapping lineage
and reservation/fence uniqueness); (8) create the write-guard function + three per-table triggers; (9) create the callable SD
functions and recovered-activation helper, then the pair-enforcement helper + two deferred constraint triggers; (10) apply
grants + default-ACL revokes; (11) add readiness wiring; (12) update ModelSnapshot. **Down (dependency-safe, no CASCADE):**
readiness wiring → exact grants/default-ACL → two deferred pair trigger instances → pair-enforcement function → three
write-guard trigger instances → write-guard function → callable SD functions → recovered-activation helper → explicitly
managed indexes/local CHECKs/self-FKs as applicable → mapping table → history table → composite FK while the head table still
exists → head table → landed-attempt AK → memberships → capability roles → restore the pre-DK-PROD ModelSnapshot. Down PRESERVES deployment LOGIN
roles, `pgcrypto`, `tagekyc_extensions`; leaves no orphan overload/grant/default-ACL/trigger/index/FK; restores the landed
attempt key set + the pre-DURABLE-KEY ModelSnapshot; clean reapply (#50).
**Recomputed census (fixture NOT counted):** conceptual head **11** / persisted disposition **10**; mapping conceptual **6** /
persisted **5**; reachable pairs **18** (Cartesian 66, rejected 48); tables **3**; callable functions **15** + owner-only helpers
**2**; roles **6**; trigger instances **5** (three write guards + two deferred pair constraints); unique/FK indexes **7** (AK 45,
composite-FK 48, event-sequence 40, event-singleton 41, provider-token 44, mapping reservation/fence 47, abandon-request self-FK
35); active DK-PROD readiness codes **21** (+3 legacy fixture, separate); evidence domains **8**; golden vectors **8**; active
test methods **78**; authorized files **36**; all identifiers ≤63 (longest
internal helper 62).

## J. Compiler-like audits
### J1 HeadState × MappingState compatibility (exactly the 18 §1a-derived legal pairs; L=legal, X=rejected-by-CHECK)
| H＼M | none | Issued | ResultObserved | CleanupRequired | CleanedUp | AbsenceProven |
|---|---|---|---|---|---|---|
| none | L | X | X | X | X | X |
| PL | X | L | L | X | X | X |
| PE | X | L | X | X | X | X |
| POU | X | L | X | X | X | X |
| PC | X | L | X | X | X | X |
| PCR | X | X | X | L | X | X |
| RF | X | X | X | X | L | L |
| AC | X | X | L | X | X | X |
| RV | X | X | L | X | X | X |
| AR | X | L | L | L | L | L |
| RA | X | X | X | X | L | L |
Legal cells = 18 (matches §1a and the executable model). Every X is prevented by local sparse CHECKs, the two deferred
cross-table constraint triggers and per-function predecessor gates. Removed unreachable cells vs a naive matrix:
`(PE,RO),(POU,RO),(PC,RO)` (resolution invariant), `(PC,CR),(PC,CU),(PC,AP)`,
`(RV,CU),(RV,AP)`, `(RF,IS),(RF,RO),(RF,CR)` — none has a transition path.
### J2 SQL surface census — 15 callable (§4) + 2 owner-only helpers; each with signature/return/owner/security/search_path/
grantees/revoked/canonical-or-internal/transitions/Down in §4 + §1; no reconciler recovered bypass; no separately-granted
Issued fn (prepare inlines it); no direct journal DML (no journal here); no capability-role access to the internal helper.
### J3 parameter authority — server-generated: `ProviderOperationId,ProviderOperationToken,*AtUtc,RowRevision,
CurrentPreparationFence,AttemptKeyContextFingerprint`. Trusted-actor-context: `RequestingActorEvidence,FinalizingActorEvidence`.
Frozen-DB: selectors read under lock in prepare. Provider-adapter result: wrapped ct/nonce/tag/suite/`ProviderResourceReference`/
receipts, absence/cleanup receipts, resolution kind, cleanup result kind. SQL-recomputed: all 8 digests. Caller-business-input
only: `p_attempt_*`,`p_source_artifact_id`,`p_preparation_*`,`p_provider_operation_token` (echo of the server token, verified
under lock),`p_operator_reason_code`,`p_revocation_reason_code`. Cleanup result kind/receipt are trusted reconciler assertions
obtained from the provider adapter but are SQL parameters at the DB boundary; SQL proves capability + exact-reference binding,
not provider provenance against that trusted principal. No digest/actor/selector/suite/timestamp is caller-authoritative.
### J4 evidence — §8 (domain/preimage/encoding/authority=SQL/storage/projection/golden/mutation). Two-layer authority in §8.
### J5 ACL/readiness — §5 (effective-ACL via aclexplode) + §6 (per-code validator/precedence/positive+mutation test).
### J6 crash/recovery (no process memory):
| Boundary | Durable record | Restart actor | Read surface | Reconstructed | Next op | No 2nd DEK/resource because |
|---|---|---|---|---|---|---|
| after prepare | (PL,IS)+Opened | encryptor/reconciler | recovery ctx | PL/IS,token | record-wrapped/resolve | token idempotent; IS present |
| provider success before ResultObserved | (PL,IS); provider durable | reconciler | Lookup(token) | Found | record_recovered→AC | CreateOrGet returns same bytes |
| ResultObserved before direct activation | (PL,RO) | encryptor | recovery ctx | RO | activate_direct | wrapped durable in mapping |
| repeated Unknown | (POU,IS)+N×ResolvedOutcomeUnknown | reconciler | recovery ctx | POU,count | resolve again | no new token/gen |
| cleanup discovered during POU | (PCR,CR)+ResolvedCleanupRequired | reconciler | recovery ctx | PCR/CR,ref | cleanup obs/ack | deadline set once |
| cleanup success before ack | (PCR,CR); provider cleaned | reconciler | recovery ctx+Lookup | CR+receipt | ack_cleanup | receipt durable |
| abandonment requested before cleanup | (AR,·)+AbandonRequested | reconciler/lifecycle | recovery ctx | AR pending | cleanup/absence branch | mapping unchanged |
| cleanup/absence before finalize | (AR,{CU,AP}) | lifecycle | recovery ctx | AR+CU/AP | finalize | gate satisfied |
| finalize commit | (RA,·)+ProviderOperationAbandoned | any | recovery ctx | RA terminal | none | terminal, digest-bound |
### J7 test-vacuity — every MUT names the exact §1 branch it disables + the exact RED outcome/state; POS/CAT/MIG rows need no
fake mutation (J); #9a/#9b/#9c are 3 methods; #17b bites the evidence authority (not a compile error), #17c is the separate
signature test.
### J8 Up/Down — §M9.

## Finding-to-anchor closure
| Finding | Anchor | Executable contract | Test | Status |
|---|---|---|---|---|
| A recovery/cleanup port | §3 | `IKekProvisioningRecoveryOperation` (Resolve+Cleanup); result taxonomies mapped to T7–T19/T23–T27; anti-forgery reference-match + effective-ACL | #51 | CLOSED |
| B direct corruption | §1 T12 | `{(PL,IS),(PE,IS),(POU,IS)}+CorruptOrUnverifiable→(PC,IS)`, ResolvedCorrupt, no successor, distinct from exhaustion | #12 | CLOSED |
| C authoritative graph / sparse | executable model, §1, §M1.1 | 46 active individually named mutating edges; `FUTURE_GATE` preserves T34a/T34b separately; pair-specific SET/CLEAR contract; operational grouped prose is non-normative | checker | CLOSED |
| D wrapped lineage through cleanup | §2c, §M1.4 | two lineages; wrapped EITHER (RN Issued / PRESERVE ResultObserved), never erased | #53,#54 | CLOSED |
| E compatibility regeneration | model, §1a, §J1 | derivation → **18** reachable pairs; deferred triggers/J1/census identical | checker,#58a,#58b | CLOSED |
| F self-contained defs | §M7, §2d | §M7 restored (v0.4 fields); full singleton index + generation proof | #57 | CLOSED |
| G write-guard routing | §M2 | exact relation names; per-table route + unknown-table fail-closed | #55a–#55c,#56 | CLOSED |
| H evidence claims | §8, §M1.3 | ProviderUnavailableObserved digest required; two-layer field authority; no new domain | #9a–#9c | CLOSED |
| I outcome vs exception | §1, §4 | ShapeInvalid/SuiteMismatch = typed RETURN only (not P0001), consistent everywhere | #4,#20 | CLOSED |
| J test census | §M6 | categorized manifest; MUT-only mutations; **78** active recomputed | checker | CLOSED |
| K readiness/ACL | §5,§6b | active DK-PROD **21** (legacy 3 separate); aclexplode effective-ACL proof | #37 | CLOSED |
| L CSPRNG not blocking | header,§6c | owner literal = activation gate only; contract not BLOCKED | #40–#47 | CLOSED |
| M migration wording | §6c,§M9 | non-mutating prerequisite assertion; no install/relocate/own/drop; Down preserves | #50 | CLOSED |
| N manifest arity | §4 | every manifest row exactly 5 columns; inspect/recovery/active-envelope rows exact | checker | CLOSED |
| P executable capability dataflow | §4, prepare paragraph | complete prepare context; active-envelope read; lifecycle self-resolution | #60,#61 | CLOSED |
| Q generation reset/value shapes | model, §M1.1 | counters reset per generation; boolean FALSE/TRUE/EITHER_BOOL, never NULL | #62,checker | CLOSED |
| R dependency-safe Down | §M9 | trigger instances precede trigger functions; no CASCADE; real Down/reapply | #50 | CLOSED |
| S post-RO lease | T5, §4 | PL/RO activation ignores expired preparation lease and survives restart | #59 | CLOSED |
| T transcription checker | model | parsed J1/M6/M8/SQL arity + six negative self-tests | checker --self-test | CLOSED |
| prior v0.2 + v0.4 closure findings | A–T above | each maps to one executable anchor/proof | — | CLOSED (pending independent review) |

## O. Stale sibling references (pending synchronization after DK-PROD freeze — NOT edited here)
DK-FIXTURE-PROOF still references DK-PROD "row 13" and "test #57" and a "§5 metadata-digest domain" anchor that are now
renumbered (recovered activation is §1 T20; the crash-window integration test is DK-FIXTURE-PROOF-local; the metadata domain
is DK-PROD §8). These are **pending synchronization after DK-PROD v0.4 freeze** — they are NOT yet valid and were NOT edited in
this turn (only DK-PROD is reconciled). Fix them when DK-FIXTURE-PROOF is unblocked against the frozen DK-PROD hash.

## Open / STOP-RRI
- **CSPRNG production extension-owner literal** — deployment-specific; a PRODUCTION-ACTIVATION gate only
  (`tip_88c1_b2_durable_key_csprng_prereq.md`), NOT a docs/proof-build/dev-test blocker (L). No other STOP/RRI.
- This standalone contract is READY FOR ONE INDEPENDENT CLOSURE REVIEW; proof-build authorization is NOT requested.
