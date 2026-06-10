namespace TagEkyc.Domain;

public sealed record ApiKeyMetadata(
    Guid Id,
    Guid ClientApplicationId,
    string KeyPrefix,
    IReadOnlySet<string> Scopes,
    ApiKeyStatus Status,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? RevokedAt);
