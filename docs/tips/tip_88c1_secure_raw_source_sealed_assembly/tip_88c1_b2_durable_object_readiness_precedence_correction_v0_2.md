# TIP-88C1-B2 DURABLE-OBJECT — Readiness precedence correction

Version: 0.2
Status: DRAFT_NARROW_CORRECTION
Date: 2026-08-05
Repository baseline: `0e301445ab12c3890e7bec72ef88ca562c4e39a8`
Superseded surface only: TIP-88C1-B2 DURABLE-OBJECT v0.5 §8.2 rows #7/#8
Dispatch v0.5 SHA-256: `EFCA33CB6A17EE2FD4E07D22AB173162919A58B60F07BFC0222AB567E0126918`
Supersedes unratified draft v0.1 SHA-256: `B3C6D0433E5DAB9E89444ADC3AF285E771B50A437FA2501CB9C5C4DDC356DC5B`

## Classification and authority

This is an append-only, narrow contract-correction candidate. It does not edit
or replace dispatch v0.5. It is not ratified and cannot authorize a commit,
deployment, production activation, Raw BIO, R2, delivery or GOV/ART evidence.

The Homeowner's temporary authority bridge permits an uncommitted validator
change and proof only so this candidate can receive an independent closure
review. The correction becomes authoritative only through a later, separate
Homeowner record with status `RATIFIED_NARROW_CORRECTION` that binds this
document's reviewed SHA-256.

Draft v0.1 incorrectly included an independently authorized provider adapter
repair. Version 0.2 removes that scope conflation. The provider repair remains
classified and evidenced only as an Option-B executable defect in the
DURABLE-OBJECT as-built; this correction neither binds nor authorizes it.

## Observed contradiction

Against the pinned real MinIO provider, a bucket created with Object Lock also
has versioning enabled because S3-compatible Object Lock requires versioning.
The v0.5 order checked versioning before Object Lock and therefore produced:

```text
Object Lock bucket
Expected: PROD_RAW_EXPORT_OBJECT_LOCK_PROHIBITED
Observed: PROD_RAW_EXPORT_OBJECT_VERSIONING_PROHIBITED
```

This made the Object-Lock-specific readiness branch unreachable against the
reference provider. Replacing the provider with a fake would hide the defect
and is not accepted evidence.

## Exact superseded and corrected contract

Superseded v0.5 §8.2 rows #7/#8:

```text
#7 VersioningNeverEnabled == false → PROD_RAW_EXPORT_OBJECT_VERSIONING_PROHIBITED
#8 ObjectLockAbsent == false        → PROD_RAW_EXPORT_OBJECT_LOCK_PROHIBITED
```

Corrected order:

```text
#7 ObjectLockAbsent == false        → PROD_RAW_EXPORT_OBJECT_LOCK_PROHIBITED
#8 VersioningNeverEnabled == false → PROD_RAW_EXPORT_OBJECT_VERSIONING_PROHIBITED
```

The implementation is exactly:

```csharp
if (!posture.ObjectLockAbsent)
    throw new ProvisionalObjectCustodyReadinessException(Codes[7]);
if (!posture.VersioningNeverEnabled)
    throw new ProvisionalObjectCustodyReadinessException(Codes[6]);
```

Bucket availability remains before both checks. Lifecycle and public-access
checks remain after them. No predicate, readiness code or code-array entry
changes.

Validator SHA-256 before correction:
`C1083CF6C7405F09F4CC23D30EB4C7392FF7552A26A581B2E01969ADCCADAA96`

Validator SHA-256 after correction:
`0094851231E043EC64DE401C6ACBCB5625004ECD517E837AF17B3FF645905A42`

## Scope invariance

This correction changes only the relative priority of the two adjacent §8.2
checks. It does not change or authorize changes to:

* `Disabled | S3CompatibleDurable` topology;
* Writer, Reconciler, Lifecycle or PostureProbe capabilities;
* credential separation, provider API interpretation or provider code;
* bucket-unavailable, lifecycle, public-access, capability, GOV/ART,
  operation or R2 gates;
* state model, SQL, migration, ACL or object classes;
* single-part transport or the 128 MiB cap;
* any readiness error token.

Object Lock and versioning both remain prohibited and fail closed. The
correction changes only which exact prohibited condition is reported first
when the real provider necessarily presents both.

## Real-provider reachability evidence

The corrected O17 proof uses task-only buckets in
`minio/minio:RELEASE.2025-09-07T16-13-09Z`:

| Real provider posture | Exact readiness result |
| --- | --- |
| Missing bucket | `PROD_RAW_EXPORT_OBJECT_BUCKET_UNAVAILABLE` |
| Versioning enabled, Object Lock absent | `PROD_RAW_EXPORT_OBJECT_VERSIONING_PROHIBITED` |
| Object Lock enabled, versioning consequently enabled | `PROD_RAW_EXPORT_OBJECT_LOCK_PROHIBITED` |
| Object Lock absent, versioning `Off`, lifecycle rule present | `PROD_RAW_EXPORT_OBJECT_LIFECYCLE_PROHIBITED` |
| Object Lock absent, versioning `Off`, no lifecycle/public access | advances to `PROD_RAW_EXPORT_OBJECT_GOV_ART_INACTIVE` |

Both `LOCK_PROHIBITED` and `VERSIONING_PROHIBITED` are therefore reachable and
distinguishable on the reference provider. The `Off` interpretation used by
this proof is an independently authorized Option-B executable adapter repair
recorded in the as-built. It is an evidence prerequisite, not part of this
contract correction.

## Mutation evidence

| Mutation | Required RED observed |
| --- | --- |
| Restore old versioning-before-lock order | Object-Lock scenario expected `LOCK_PROHIBITED`, observed `VERSIONING_PROHIBITED` |
| Remove Object-Lock rejection | Object-Lock scenario expected `LOCK_PROHIBITED`, observed `VERSIONING_PROHIBITED` |
| Remove versioning rejection | Versioning-only scenario expected `VERSIONING_PROHIBITED`, observed `GOV_ART_INACTIVE` |

Each mutation was applied separately and the validator was restored to SHA-256
`0094851231E043EC64DE401C6ACBCB5625004ECD517E837AF17B3FF645905A42`.

## Review and ratification boundary

Independent review must bind this exact v0.2 draft, dispatch v0.5, the
validator, O17, the real MinIO evidence and the DURABLE-OBJECT as-built. The
review must not treat the independent provider `Off` repair as part of this
correction's authority.

Do not change this reviewed draft in place to mark it ratified. After clean
review, create a successor Homeowner record that binds:

```text
CorrectionPath
CorrectionSHA256
ReviewedVerdict
ReviewerIdentity
RatifiedAtUtc
```

IndependentReview: `PENDING`
HomeownerRatification: `NOT_RECORDED`
CommitAuthority: `NONE`
