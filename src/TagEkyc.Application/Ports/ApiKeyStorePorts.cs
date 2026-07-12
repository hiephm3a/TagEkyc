using TagEkyc.Domain;

namespace TagEkyc.Application.Ports;

public interface IApiKeyStore
{
    Task<ResolvedApiKey?> FindByPresentedKeyAsync(string presentedApiKey, CancellationToken cancellationToken = default);
}

public sealed record ResolvedApiKey(
    Guid ApiKeyId,
    Guid ClientApplicationId,
    string KeyPrefix,
    IReadOnlySet<string> Scopes,
    ApiKeyStatus Status,
    DateTimeOffset? ExpiresAt,
    AuthenticatedCallerCategory CallerCategory,
    IReadOnlySet<Guid>? AllowedClientApplicationIds = null,
    IReadOnlySet<string>? AllowedCaptureAgentIds = null,
    Guid PrincipalId = default);
