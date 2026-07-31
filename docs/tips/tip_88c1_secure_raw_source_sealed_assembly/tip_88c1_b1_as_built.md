# TIP-88C1-B1 — As-Built: Ingress-Claim Idempotency Skeleton (key-free)

Maps the landed C1-B1 code to the planning brief. Records the key-free claim
shell, the alias/token ledger, the C1-only hash codec, and exactly what was
deferred to C1-B2. Design lives in `tip_88c1_planning_brief.md` §D4.1 / §5.6.

- **Commit:** `94161f4` (branch `tip-88a-raw-export-policy-catalog-build`, unpushed).
- **Brief anchor:** §D4.1 (exact ingress claim), §5.6 (LP codec).
- **Migration:** `20260730214000_Tip88C1B1IngressClaimIdempotencySkeleton.cs`.

## Landed surface

Two tables (schema `tagekyc`):

**`raw_export_source_ingress_claims`** — the canonical `ClaimEvaluating` shell.
- PK `IngressClaimId`; `uq_..._exact_artifact` = (ClientApplicationId, ProducerId, VerificationSessionId, CaptureArtifactId, CaptureRevision, RawClass); FKs (RESTRICT) → acceptance / `capture_artifacts` / `verification_sessions`.
- `ck_..._state` **hard-pins** `ClaimState = 'ClaimEvaluating' AND CaptureRevision >= 1 AND CommitmentKeySelectorVersion >= 1 AND octet_length(IngressIdentityFingerprint) = 32`.
- Stores `CommitmentKeySelectorId` varchar(128) + `CommitmentKeySelectorVersion` int — a **key selector, not key material**; and `AuthoritySnapshotId` varchar(128) **opaque, no FK, no snapshot table** (provenance not yet materialized — see C1-B2 note).
- Has **no** `SourceArtifactId`, `ContentCommitment`, `AdmissionFingerprint`, reservation/attempt/object/key columns.

**`raw_export_source_ingress_claim_aliases`** — four-field lookup + consumption ledger.
- PK `IngressClaimAliasId`; `uq_..._alias_key` = (ClientApplicationId, ProducerId, CaptureAgentInstanceId, IngressIdempotencyKey); FK `IngressClaimId` → claim (nullable).
- `ck_..._state` allows `AliasState IN ('Evaluating','Bound','ConflictTombstone')` (Bound/ConflictTombstone reserved for C1-B2), disposition set, `CurrentClaimEvaluationRevision/Fence >= 1`, `CurrentTokenSchemaVersion = 1`, variant IN (`NewClaimEvaluationToken`,`ExistingClaimComparisonToken`), audience `tagekyc.raw-export-source-ingress-claim-comparison`, 32-byte fingerprints/token digest, token expiry ordering.

Two `SECURITY DEFINER` functions (owner deployer, `search_path = pg_catalog`):
- `begin_raw_export_source_ingress_claim(23 args) → TABLE(outcome_code, claim_evaluation_token, token_variant, token_expires_at_utc, claim_evaluation_id, claim_evaluation_revision, claim_evaluation_fence, retry_not_before_utc)`.
- `validate_raw_export_claim_evaluation_token(10 args) → boolean` (UUIDv4 + base64url-43 grammar guard, constant-time digest compare).
- Guard `enforce_raw_export_source_ingress_write()` — deployer + write-context GUC (`claim`/`alias`) → else `RAW_EXPORT_SOURCE_INGRESS_DIRECT_DML_UNSUPPORTED`.

## Hash codec

`C1HashCanonical` (`src/TagEkyc.Infrastructure/RawExport/C1HashCanonical.cs`) —
length-prefixed (u32 big-endian), domain-separated, SHA-256. A **new C1-only
profile, distinct** from the landed S1 Evidence-Integrity `HashCanonical` JCS
codec (does not reinterpret it). `begin` computes the **unkeyed**
`ProducerClaimEnvelopeFingerprint` and `IngressIdentityFingerprint`. Golden
vectors are pinned in the C1-B1 tests with SQL/C#/Node agreement.

## Why it is KEY-FREE

`begin` needs no key: it freezes only the commitment key **selector** (id +
version) and computes **unkeyed** fingerprints. No `ContentCommitment`, HMAC,
`complete`, DEK, or KEK is present — verified by grep at landing (empty).

## Gate opened

**`C1-B2-INGRESS-COMPLETE-GATE`** — deferred to a separately authorized C1-B2:
claim-comparison broker, keyed `ContentCommitment`/HMAC, `complete_...`,
`AdmissionFingerprint` recompute, `SourceArtifactId` allocation,
`uq_raw_export_source_ingress_source`, R1 crypto/DEK/KEK/object context,
`Bound`/`ConflictTombstone` resolution, and key-provider integration.

## Note carried into C1-B2 scoping

`AuthoritySnapshotId` is an opaque varchar with no landed provenance;
`ControllerIdentity` + `StableDataScopeId` (bound into the §5.6.1
`ContentCommitment` preimage) have **zero landed source**. C1-B2 must supply a
provenance for them (fixture authority-snapshot, or caller-assert + gate)
before it can compute an authoritative commitment.

## Verification at landing

Per-project test isolation green; codec golden vectors + token constant-time
compare + direct-DML guard mutation-proven. ModelSnapshot →
`8EC86A3565AAFEFA4CBEAA6F256BBDB4D38AF60617389184D27BE33036BE7243`.

## Supersession note — C1-B2-BETA

The B1 `begin_raw_export_source_ingress_claim` body was superseded by the
C1-B2-BETA migration. B1 compared the full `IngressIdentityFingerprint`,
which includes `IngressIdempotencyKey`, making the documented Existing token
branch unreachable for an alternate alias. BETA preserves the canonical claim
and fingerprint, permits only the idempotency key to differ, and compares every
other immutable identity field explicitly. See
`tip_88c1_b2_beta_as_built.md` for lifecycle and validation evidence.
