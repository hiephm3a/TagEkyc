namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportManagedRecipientIdentityRow
{
    public Guid RecipientClientApplicationId { get; set; }
    public Guid PrincipalId { get; set; }
    public string State { get; set; } = string.Empty;
    public long Revision { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
