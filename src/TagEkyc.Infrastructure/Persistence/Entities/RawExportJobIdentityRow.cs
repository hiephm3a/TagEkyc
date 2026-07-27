namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportJobIdentityRow
{
    public Guid JobId { get; set; }
    public Guid PermitId { get; set; }
    public Guid AuthorizationDecisionId { get; set; }
    public Guid PrincipalId { get; set; }
    public Guid ClientApplicationId { get; set; }
    public Guid CreatedByApiKeyId { get; set; }
    public Guid VerificationSessionId { get; set; }
    public string SubjectRef { get; set; } = string.Empty;
    public Guid PolicyId { get; set; }
    public int PolicyVersion { get; set; }
    public string PurposeCode { get; set; } = string.Empty;
    public Guid RecipientClientApplicationId { get; set; }
    public string ExportMode { get; set; } = string.Empty;
    public DateTimeOffset PermitExpiresAt { get; set; }
    public DateTimeOffset JobExpiresAt { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public byte[] IdempotencyFingerprintHash { get; set; } = [];
    public int SchemaVersion { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
