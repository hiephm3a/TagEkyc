namespace TagEkyc.Domain;

public static class RequiredCheckPolicy
{
    public static readonly IReadOnlySet<RequiredCheckType> SignFlowS1RequiredChecks =
        new HashSet<RequiredCheckType>
        {
            RequiredCheckType.CaptureQuality,
            RequiredCheckType.DocumentNfc,
            RequiredCheckType.FaceMatch,
            RequiredCheckType.Liveness,
        };

    public static bool IsSatisfiedBy(IEnumerable<RequiredCheckType> requestedChecks, IReadOnlySet<RequiredCheckType> requiredChecks)
    {
        var requested = requestedChecks.ToHashSet();
        return requiredChecks.All(requested.Contains);
    }
}
