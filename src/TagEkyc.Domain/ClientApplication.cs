namespace TagEkyc.Domain;

public sealed record ClientApplication(
    Guid Id,
    string Name,
    ClientApplicationStatus Status,
    IReadOnlySet<string> AllowedPurposes,
    IReadOnlySet<RequiredCheckType> AllowedChecks,
    Uri? WebhookBaseUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset? DisabledAt);
