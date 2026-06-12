namespace TagEkyc.Domain;

public sealed record DecisionReproducibilityBoundary
{
    public DecisionReproducibilityBoundary(
        PolicySnapshotId policySnapshotId,
        RequiredCheckSetPolicyIdentity requiredCheckSetPolicy)
    {
        PolicySnapshotId = policySnapshotId ?? throw new ArgumentNullException(nameof(policySnapshotId));
        RequiredCheckSetPolicy = requiredCheckSetPolicy ?? throw new ArgumentNullException(nameof(requiredCheckSetPolicy));

        if (RequiredCheckSetPolicy.PolicySnapshotId != PolicySnapshotId)
        {
            throw new ArgumentException("Required check set policy must use the same policy snapshot id.", nameof(requiredCheckSetPolicy));
        }
    }

    public PolicySnapshotId PolicySnapshotId { get; }
    public RequiredCheckSetPolicyIdentity RequiredCheckSetPolicy { get; }
}
