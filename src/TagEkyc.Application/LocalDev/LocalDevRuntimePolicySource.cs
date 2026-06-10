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
    AuthenticatedCallerCategory CallerCategory);

public sealed class LocalDevRuntimePolicySource : ILocalDevClientPolicyProvider
{
    public static readonly Guid BusinessClientId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid OtherBusinessClientId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    public static readonly Guid DisabledClientId = Guid.Parse("10000000-0000-0000-0000-000000000003");

    private static readonly IReadOnlySet<string> BusinessScopes = new HashSet<string>
    {
        "business.session.create",
        "business.session.read",
    };

    private readonly IReadOnlyList<LocalDevClientPolicy> policies =
    [
        CreateBusinessPolicy(BusinessClientId, ClientApplicationStatus.Active),
        CreateBusinessPolicy(OtherBusinessClientId, ClientApplicationStatus.Active),
        CreateBusinessPolicy(DisabledClientId, ClientApplicationStatus.Disabled),
    ];

    private readonly IReadOnlyList<LocalDevApiKeyRecord> apiKeys =
    [
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000001"),
            BusinessClientId,
            "localdev-business-key",
            "ldev_biz",
            BusinessScopes,
            ApiKeyStatus.Active,
            DateTimeOffset.UtcNow.AddYears(10),
            AuthenticatedCallerCategory.BusinessConsumer),
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000002"),
            OtherBusinessClientId,
            "localdev-other-business-key",
            "ldev_other",
            BusinessScopes,
            ApiKeyStatus.Active,
            DateTimeOffset.UtcNow.AddYears(10),
            AuthenticatedCallerCategory.BusinessConsumer),
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000003"),
            BusinessClientId,
            "localdev-readonly-key",
            "ldev_read",
            new HashSet<string> { "business.session.read" },
            ApiKeyStatus.Active,
            DateTimeOffset.UtcNow.AddYears(10),
            AuthenticatedCallerCategory.BusinessConsumer),
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000004"),
            BusinessClientId,
            "localdev-revoked-key",
            "ldev_rev",
            BusinessScopes,
            ApiKeyStatus.Revoked,
            DateTimeOffset.UtcNow.AddYears(10),
            AuthenticatedCallerCategory.BusinessConsumer),
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000005"),
            BusinessClientId,
            "localdev-expired-key",
            "ldev_exp",
            BusinessScopes,
            ApiKeyStatus.Expired,
            DateTimeOffset.UtcNow.AddDays(-1),
            AuthenticatedCallerCategory.BusinessConsumer),
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000006"),
            DisabledClientId,
            "localdev-disabled-client-key",
            "ldev_dis",
            BusinessScopes,
            ApiKeyStatus.Active,
            DateTimeOffset.UtcNow.AddYears(10),
            AuthenticatedCallerCategory.BusinessConsumer),
    ];

    public IReadOnlyList<LocalDevApiKeyRecord> ApiKeys => apiKeys;
    public IReadOnlyList<LocalDevClientPolicy> Policies => policies;

    public Task<LocalDevClientPolicy?> GetPolicyAsync(
        Guid clientApplicationId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(policies.SingleOrDefault(policy => policy.ClientApplicationId == clientApplicationId));

    private static LocalDevClientPolicy CreateBusinessPolicy(Guid clientApplicationId, ClientApplicationStatus status) =>
        new(
            clientApplicationId,
            status,
            new HashSet<VerificationProfile>
            {
                VerificationProfile.StandardEkycProfile,
                VerificationProfile.TransactionBoundEkycProfile,
            },
            new HashSet<string>
            {
                "PATIENT_REGISTRATION",
                "SIGNING_AUTH",
            },
            new HashSet<RequiredCheckType>
            {
                RequiredCheckType.CaptureQuality,
                RequiredCheckType.DocumentNfc,
                RequiredCheckType.FaceMatch,
                RequiredCheckType.Liveness,
            },
            BusinessScopes,
            AllowsTransactionBoundProfile: true,
            MaxSessionTtl: TimeSpan.FromHours(24));
}

