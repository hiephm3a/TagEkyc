# TIP-88C1-B2 DURABLE-OBJECT — Readiness precedence correction

Version: 0.1
Status: DRAFT_NARROW_CORRECTION
Date: 2026-08-05
Repository baseline: `0e301445ab12c3890e7bec72ef88ca562c4e39a8`
Superseded surface only: TIP-88C1-B2 DURABLE-OBJECT v0.5 §8.2 rows #7/#8
Dispatch v0.5 SHA-256: `EFCA33CB6A17EE2FD4E07D22AB173162919A58B60F07BFC0222AB567E0126918`

## Classification and authority

This is an append-only, narrow contract-correction candidate. It does not edit
or replace dispatch v0.5. It is not ratified and cannot authorize a commit,
deployment, production activation, Raw BIO, R2, delivery or GOV/ART evidence.

The Homeowner's temporary authority bridge permits an uncommitted validator
change and proof only so this candidate can receive an independent closure
review. The correction becomes authoritative only through a later, separate
Homeowner record with status `RATIFIED_NARROW_CORRECTION` that binds this
document's reviewed SHA-256.

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

## Exact correction

Only the two adjacent posture checks exchange order:

```csharp
if (!posture.ObjectLockAbsent)
    throw new ProvisionalObjectCustodyReadinessException(Codes[7]);
if (!posture.VersioningNeverEnabled)
    throw new ProvisionalObjectCustodyReadinessException(Codes[6]);
```

Bucket availability remains before both checks. Lifecycle and public-access
checks remain after them. No predicate, readiness code, code array, provider
call, posture field or any other precedence changes.

Validator SHA-256 before correction:
`C1083CF6C7405F09F4CC23D30EB4C7392FF7552A26A581B2E01969ADCCADAA96`

Validator SHA-256 after correction:
`0094851231E043EC64DE401C6ACBCB5625004ECD517E837AF17B3FF645905A42`

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
distinguishable on the reference provider.

## Mutation evidence

| Mutation | Required RED observed |
| --- | --- |
| Restore old versioning-before-lock order | Object-Lock scenario expected `LOCK_PROHIBITED`, observed `VERSIONING_PROHIBITED` |
| Remove Object-Lock rejection | Object-Lock scenario expected `LOCK_PROHIBITED`, observed `VERSIONING_PROHIBITED` |
| Remove versioning rejection | Versioning-only scenario expected `VERSIONING_PROHIBITED`, observed `GOV_ART_INACTIVE` |

Each mutation was applied separately and the validator was restored to SHA-256
`0094851231E043EC64DE401C6ACBCB5625004ECD517E837AF17B3FF645905A42`.

## Related provider interpretation repair

The same real-provider proof showed that MinIO returns exact versioning status
`Off` for a bucket that has never enabled versioning. The adapter previously
accepted only null/empty status and falsely emitted `VERSIONING_PROHIBITED`.
The minimal executable repair accepts only null/empty or exact `Off`; `Enabled`
and `Suspended` remain prohibited.

Provider SHA-256 before repair:
`83AFB62C2A95C469C6CE63E59A355A28D8214CB8659621E00579160D6DED5235`

Provider SHA-256 after repair:
`A2A0F3EC08CD1AECA4508979328CCFF4768523A8A73E15E67D61264CA4C52116`

## Review boundary

Independent review must bind this exact draft, dispatch v0.5, the validator,
O17, the real MinIO evidence and the DURABLE-OBJECT as-built. Do not change this
reviewed draft in place to mark it ratified; create a successor Homeowner record.
