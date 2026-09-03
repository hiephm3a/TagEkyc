using TagEkyc.Application.VerificationSessions;
using TagEkyc.Domain;

namespace TagEkyc.Application.LocalDev;

public sealed record LocalDevApiKeyRecord(
    Guid ApiKeyId,
    Guid ClientApplicationId,
    string ApiKeyValue,
    string KeyPrefix,
    IReadOnlySet<string> Scopes,
    ApiKeyStatus Status,
    DateTimeOffset? ExpiresAt,
    AuthenticatedCallerCategory CallerCategory,
    IReadOnlySet<Guid>? AllowedClientApplicationIds = null,
    IReadOnlySet<string>? AllowedCaptureAgentIds = null,
    Guid PrincipalId = default);

public sealed class LocalDevRuntimePolicySource : ILocalDevClientPolicyProvider
{
    public static readonly Guid BusinessClientId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid OtherBusinessClientId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    public static readonly Guid DisabledClientId = Guid.Parse("10000000-0000-0000-0000-000000000003");

    private static readonly IReadOnlySet<string> BusinessScopes = new HashSet<string>
    {
        "business.session.create",
        "business.session.read",
        "session.complete",
        "session.cancel",
    };

    private static readonly IReadOnlySet<string> CaptureAgentScopes = new HashSet<string>
    {
        "capture.artifact.append",
    };

    private static readonly IReadOnlySet<string> TrustedAdapterScopes = new HashSet<string>
    {
        "trusted.evidence.append",
    };

    private readonly IReadOnlyList<LocalDevClientPolicy> policies =
    [
        CreateBusinessPolicy(BusinessClientId, ClientApplicationStatus.Active),
        CreateBusinessPolicy(OtherBusinessClientId, ClientApplicationStatus.Active),
        CreateBusinessPolicy(DisabledClientId, ClientApplicationStatus.Disabled),
    ];

    public IReadOnlyList<LocalDevClientPolicy> Policies => policies;

    public Task<LocalDevClientPolicy?> GetPolicyAsync(
        Guid clientApplicationId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(policies.SingleOrDefault(policy => policy.ClientApplicationId == clientApplicationId));

    private static LocalDevClientPolicy CreateBusinessPolicy(Guid clientApplicationId, ClientApplicationStatus status) =>
        new(
            clientApplicationId,
            status,
            PolicySnapshotId.LocalDevS1,
            new HashSet<VerificationProfile>
            {
                VerificationProfile.StandardEkycProfile,
                VerificationProfile.ChallengeBoundEkycProfile,
            },
            new HashSet<string>
            {
                "PATIENT_REGISTRATION",
                "SIGNING_AUTH",
            },
            new HashSet<RequiredCheckType>
            {
                RequiredCheckType.CaptureQuality,
                RequiredCheckType.DocumentOcr,
                RequiredCheckType.DocumentNfc,
                RequiredCheckType.FaceMatch,
                RequiredCheckType.Liveness,
            },
            new HashSet<string>(
                BusinessScopes
                    .Concat(CaptureAgentScopes)
                    .Concat(TrustedAdapterScopes)),
            AllowsChallengeBoundProfile: true,
            MaxSessionTtl: TimeSpan.FromHours(24),
            AllowedOptionalEvidenceChecks: new HashSet<RequiredCheckType>(),
            AllowedSupportingArtifactTypes: new HashSet<CaptureArtifactType>
            {
                CaptureArtifactType.DocumentFrontImage,
                CaptureArtifactType.DocumentBackImage,
                CaptureArtifactType.DeviceCaptureMetadata,
            });
}
