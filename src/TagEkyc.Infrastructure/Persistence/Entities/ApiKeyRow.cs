namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class ApiKeyRow
{
    public Guid ApiKeyId { get; set; }

    public Guid ClientApplicationId { get; set; }

    public Guid PrincipalId { get; set; }

    public string CredentialRef { get; set; } = string.Empty;

    public string CredentialType { get; set; } = string.Empty;

    public string CredentialStatus { get; set; } = string.Empty;

    public string KeyPrefix { get; set; } = string.Empty;

    public byte[] KeyHash { get; set; } = [];

    public string ScopesJson { get; set; } = "[]";

    public DateTimeOffset? ExpiresAt { get; set; }

    public string CallerCategory { get; set; } = string.Empty;

    public string? AllowedClientApplicationIdsJson { get; set; }

    public string? AllowedCaptureAgentIdsJson { get; set; }

    public string? OAuthClientId { get; set; }

    public string? MtlsSubjectDn { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
