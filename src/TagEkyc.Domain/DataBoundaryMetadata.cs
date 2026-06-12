namespace TagEkyc.Domain;

public sealed record DataBoundaryMetadata
{
    public static DataBoundaryMetadata LocalDevDefault(IReadOnlySet<RequiredCheckType> requiredChecks) =>
        CreateDefault(PolicySnapshotId.LocalDevS1, requiredChecks);

    public static DataBoundaryMetadata CreateDefault(
        PolicySnapshotId policySnapshotId,
        IReadOnlySet<RequiredCheckType> requiredChecks) =>
        new(
            policySnapshotId,
            new DecisionReproducibilityBoundary(
                policySnapshotId,
                new RequiredCheckSetPolicyIdentity(policySnapshotId, requiredChecks)),
            RetentionClass.LocalDevEphemeral,
            DeletionEligibility.NotEvaluated,
            LegalHoldStatus.None,
            PurgeBlockReason.None,
            accessAuditRequired: false,
            actor: null);

    public DataBoundaryMetadata(
        PolicySnapshotId policySnapshotId,
        DecisionReproducibilityBoundary decisionBoundary,
        RetentionClass retentionClass,
        DeletionEligibility deletionEligibility,
        LegalHoldStatus legalHoldStatus,
        PurgeBlockReason purgeBlockReason,
        bool accessAuditRequired,
        ActorReference? actor)
    {
        PolicySnapshotId = policySnapshotId ?? throw new ArgumentNullException(nameof(policySnapshotId));
        DecisionBoundary = decisionBoundary ?? throw new ArgumentNullException(nameof(decisionBoundary));

        if (DecisionBoundary.PolicySnapshotId != PolicySnapshotId)
        {
            throw new ArgumentException("Decision boundary must use the same policy snapshot id.", nameof(decisionBoundary));
        }

        RetentionClass = retentionClass;
        DeletionEligibility = deletionEligibility;
        LegalHoldStatus = legalHoldStatus;
        PurgeBlockReason = purgeBlockReason;
        AccessAuditRequired = accessAuditRequired;
        Actor = actor;
    }

    public PolicySnapshotId PolicySnapshotId { get; }
    public DecisionReproducibilityBoundary DecisionBoundary { get; }
    public RetentionClass RetentionClass { get; }
    public DeletionEligibility DeletionEligibility { get; }
    public LegalHoldStatus LegalHoldStatus { get; }
    public PurgeBlockReason PurgeBlockReason { get; }
    public bool AccessAuditRequired { get; }
    public ActorReference? Actor { get; }
}
