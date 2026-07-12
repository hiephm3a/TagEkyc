namespace TagEkyc.Application;

public enum AuthenticatedCallerCategory
{
    BusinessConsumer = 0,
    CaptureAgent = 1,
    TrustedAdapter = 2,
    OperatorAdmin = 3,
}

public sealed record AuthenticatedClientContext(
    Guid ApiKeyId,
    Guid ClientApplicationId,
    string KeyPrefix,
    AuthenticatedCallerCategory CallerCategory,
    IReadOnlySet<string> Scopes,
    IReadOnlySet<Guid>? AllowedClientApplicationIds = null,
    IReadOnlySet<string>? AllowedCaptureAgentIds = null,
    Guid PrincipalId = default);
