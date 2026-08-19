namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportRecipientPackageDeliveryEventRow
{
    public Guid DeliveryEventId { get; set; }
    public Guid DeliveryId { get; set; }
    public Guid RecipientClientApplicationId { get; set; }
    public long Revision { get; set; }
    public string EventType { get; set; } = string.Empty;
    public int? DeliveryAttemptNumber { get; set; }
    public long? DeliveryFence { get; set; }
    public Guid AuthenticatedApiKeyId { get; set; }
    public Guid AuthenticatedPrincipalId { get; set; }
    public byte[] CorrelationDigest { get; set; } = [];
    public byte[] EvidenceDigest { get; set; } = [];
    public DateTimeOffset OccurredAtUtc { get; set; }
}
