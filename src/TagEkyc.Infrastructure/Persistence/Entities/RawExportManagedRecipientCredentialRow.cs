namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportManagedRecipientCredentialRow
{
    public Guid ApiKeyId { get; set; }
    public Guid RecipientClientApplicationId { get; set; }
    public Guid PrincipalId { get; set; }
    public int CredentialVersion { get; set; }
    public string State { get; set; } = string.Empty;
    public long Revision { get; set; }
    public DateTimeOffset IssuedAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public string? RevocationReason { get; set; }
    public Guid? ReplacedByApiKeyId { get; set; }
}
