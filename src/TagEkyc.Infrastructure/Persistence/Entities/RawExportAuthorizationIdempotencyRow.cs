namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportAuthorizationIdempotencyRow
{
    public Guid PrincipalId { get; set; }
    public Guid ClientApplicationId { get; set; }
    public Guid RequestedVerificationSessionId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public byte[] FingerprintHash { get; set; } = [];
    public Guid ExportDecisionId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
