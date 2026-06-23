using TagEkyc.Domain;

namespace TagEkyc.Application.VerificationSessions;

public sealed record LocalDevClientPolicy(
    Guid ClientApplicationId,
    ClientApplicationStatus Status,
    PolicySnapshotId PolicySnapshotId,
    IReadOnlySet<VerificationProfile> AllowedProfiles,
    IReadOnlySet<string> AllowedPurposes,
    IReadOnlySet<RequiredCheckType> AllowedRequiredChecks,
    IReadOnlySet<string> AllowedCallerScopes,
    bool AllowsChallengeBoundProfile,
    TimeSpan? MaxSessionTtl,
    IReadOnlySet<RequiredCheckType>? AllowedOptionalEvidenceChecks = null,
    IReadOnlySet<CaptureArtifactType>? AllowedSupportingArtifactTypes = null);

public interface ILocalDevClientPolicyProvider
{
    Task<LocalDevClientPolicy?> GetPolicyAsync(
        Guid clientApplicationId,
        CancellationToken cancellationToken = default);
}
