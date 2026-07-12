using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Domain;

namespace TagEkyc.Application.LocalDev;

public sealed class LocalDevApiKeyStore : IApiKeyStore
{
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

    private readonly IReadOnlyList<LocalDevApiKeyRecord> apiKeys =
    [
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000001"),
            LocalDevRuntimePolicySource.BusinessClientId,
            "localdev-business-key",
            "ldev_biz",
            BusinessScopes,
            ApiKeyStatus.Active,
            DateTimeOffset.UtcNow.AddYears(10),
            AuthenticatedCallerCategory.BusinessConsumer,
            PrincipalId: LocalDevRuntimePolicySource.BusinessClientId),
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000002"),
            LocalDevRuntimePolicySource.OtherBusinessClientId,
            "localdev-other-business-key",
            "ldev_other",
            BusinessScopes,
            ApiKeyStatus.Active,
            DateTimeOffset.UtcNow.AddYears(10),
            AuthenticatedCallerCategory.BusinessConsumer,
            PrincipalId: LocalDevRuntimePolicySource.OtherBusinessClientId),
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000003"),
            LocalDevRuntimePolicySource.BusinessClientId,
            "localdev-readonly-key",
            "ldev_read",
            new HashSet<string> { "business.session.read" },
            ApiKeyStatus.Active,
            DateTimeOffset.UtcNow.AddYears(10),
            AuthenticatedCallerCategory.BusinessConsumer,
            PrincipalId: LocalDevRuntimePolicySource.BusinessClientId),
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000009"),
            LocalDevRuntimePolicySource.BusinessClientId,
            "localdev-complete-only-key",
            "ldev_complete",
            new HashSet<string> { "business.session.create", "session.complete", "session.cancel" },
            ApiKeyStatus.Active,
            DateTimeOffset.UtcNow.AddYears(10),
            AuthenticatedCallerCategory.BusinessConsumer,
            PrincipalId: LocalDevRuntimePolicySource.BusinessClientId),
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000004"),
            LocalDevRuntimePolicySource.BusinessClientId,
            "localdev-revoked-key",
            "ldev_rev",
            BusinessScopes,
            ApiKeyStatus.Revoked,
            DateTimeOffset.UtcNow.AddYears(10),
            AuthenticatedCallerCategory.BusinessConsumer,
            PrincipalId: LocalDevRuntimePolicySource.BusinessClientId),
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000005"),
            LocalDevRuntimePolicySource.BusinessClientId,
            "localdev-expired-key",
            "ldev_exp",
            BusinessScopes,
            ApiKeyStatus.Expired,
            DateTimeOffset.UtcNow.AddDays(-1),
            AuthenticatedCallerCategory.BusinessConsumer,
            PrincipalId: LocalDevRuntimePolicySource.BusinessClientId),
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000006"),
            LocalDevRuntimePolicySource.DisabledClientId,
            "localdev-disabled-client-key",
            "ldev_dis",
            BusinessScopes,
            ApiKeyStatus.Active,
            DateTimeOffset.UtcNow.AddYears(10),
            AuthenticatedCallerCategory.BusinessConsumer,
            PrincipalId: LocalDevRuntimePolicySource.DisabledClientId),
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000007"),
            LocalDevRuntimePolicySource.BusinessClientId,
            "localdev-capture-agent-key",
            "ldev_capture",
            CaptureAgentScopes,
            ApiKeyStatus.Active,
            DateTimeOffset.UtcNow.AddYears(10),
            AuthenticatedCallerCategory.CaptureAgent,
            new HashSet<Guid> { LocalDevRuntimePolicySource.BusinessClientId },
            new HashSet<string> { "ldev_capture" },
            PrincipalId: Guid.Parse("30000000-0000-0000-0000-000000000007")),
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000008"),
            LocalDevRuntimePolicySource.BusinessClientId,
            "localdev-trusted-adapter-key",
            "ldev_adapter",
            TrustedAdapterScopes,
            ApiKeyStatus.Active,
            DateTimeOffset.UtcNow.AddYears(10),
            AuthenticatedCallerCategory.TrustedAdapter,
            new HashSet<Guid> { LocalDevRuntimePolicySource.BusinessClientId },
            PrincipalId: Guid.Parse("30000000-0000-0000-0000-000000000008")),
    ];

    public IReadOnlyList<LocalDevApiKeyRecord> ApiKeys => apiKeys;

    public Task<ResolvedApiKey?> FindByPresentedKeyAsync(
        string presentedApiKey,
        CancellationToken cancellationToken = default)
    {
        var apiKey = apiKeys.SingleOrDefault(candidate => candidate.ApiKeyValue == presentedApiKey);
        return Task.FromResult(apiKey is null ? null : ToResolved(apiKey));
    }

    private static ResolvedApiKey ToResolved(LocalDevApiKeyRecord apiKey) =>
        new(
            apiKey.ApiKeyId,
            apiKey.ClientApplicationId,
            apiKey.KeyPrefix,
            apiKey.Scopes,
            apiKey.Status,
            apiKey.ExpiresAt,
            apiKey.CallerCategory,
            apiKey.AllowedClientApplicationIds,
            apiKey.AllowedCaptureAgentIds,
            apiKey.PrincipalId);
}
