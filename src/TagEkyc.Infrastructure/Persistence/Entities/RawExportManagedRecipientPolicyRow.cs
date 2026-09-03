namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportManagedRecipientPolicyRow
{
    public Guid RecipientClientApplicationId { get; set; }
    public string ActivationProfile { get; set; } = string.Empty;
    public byte[] ActivationScopesDigest { get; set; } = [];
    public string State { get; set; } = string.Empty;
    public long Revision { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
