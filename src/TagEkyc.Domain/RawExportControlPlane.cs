namespace TagEkyc.Domain;

public enum RawExportGrantEventType
{
    Granted = 0,
    Revoked = 1,
}

public enum RawExportAuthorityType
{
    GrantAdmin = 0,
    RecorderAuthorityAdmin = 1,
    ActivationAuthority = 2,
    FulfillmentRecorder = 3,
}

public enum RawExportAuthorityScopeType
{
    Policy = 0,
    Global = 1,
}

public enum RawExportAuthorityEventType
{
    Granted = 0,
    Revoked = 1,
}

public enum RawExportFulfillmentEventType
{
    Accepted = 0,
    Withdrawn = 1,
}

public enum RawExportLifecycleEventType
{
    Activated = 0,
    Suspended = 1,
    Revoked = 2,
}

public enum RawExportEligibilityState
{
    Active = 0,
    Inactive = 1,
}

public enum RawExportEligibilityCause
{
    Abandoned = 0,
    NotCatalogApproved = 1,
    Revoked = 2,
    Suspended = 3,
    NotActivated = 4,
    NoGrant = 5,
    StaleRuleSet = 6,
    MissingOrInvalidFulfillment = 7,
}

public sealed record RawExportGrantRef(
    Guid PrincipalId,
    Guid PolicyId,
    int PolicyVersion,
    int Revision);

public sealed record RawExportLifecycleRef(
    Guid PolicyId,
    int PolicyVersion,
    int Revision);

public sealed record RawExportFulfillmentRef(
    RawExportRequirementType RequirementType,
    Guid FulfillmentEventId,
    int Revision,
    string ArtifactRef,
    string ArtifactVersion);

public sealed record RawExportEligibilitySnapshot(
    RawExportEligibilityState State,
    RawExportEligibilityCause? PrimaryCause,
    IReadOnlyList<RawExportEligibilityCause> Causes,
    DateTimeOffset EvaluatedAtUtc,
    int BoundRuleSetVersion,
    int CurrentRuleSetVersion,
    RawExportGrantRef? GrantRef,
    RawExportLifecycleRef? LifecycleRef,
    IReadOnlyList<RawExportFulfillmentRef> FulfillmentRefs);

public static class RawExportControlPlaneConstants
{
    public static readonly Guid DeploymentPrincipalId = Guid.Parse("00000000-0000-5000-8000-000000088b10");

    public const string RuleSetPublishLockKey = "tip88b1:raw_export_requirement_rule_set_publish";
}
