namespace TagEkyc.Domain;

public sealed record RequiredCheckSetPolicyIdentity
{
    public RequiredCheckSetPolicyIdentity(
        PolicySnapshotId policySnapshotId,
        IEnumerable<RequiredCheckType> requiredChecks)
    {
        PolicySnapshotId = policySnapshotId ?? throw new ArgumentNullException(nameof(policySnapshotId));

        var checkSet = requiredChecks.ToHashSet();
        if (checkSet.Count == 0)
        {
            throw new ArgumentException("Required check set policy identity requires at least one check.", nameof(requiredChecks));
        }

        RequiredChecks = checkSet;
    }

    public PolicySnapshotId PolicySnapshotId { get; }
    public IReadOnlySet<RequiredCheckType> RequiredChecks { get; }
}
