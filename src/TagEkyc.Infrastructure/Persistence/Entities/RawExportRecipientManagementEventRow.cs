namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportRecipientManagementEventRow
{
    public Guid ManagementEventId { get; set; }
    public Guid OperationId { get; set; }
    public Guid ManagerApiKeyId { get; set; }
    public Guid ManagerPrincipalId { get; set; }
    public Guid RecipientClientApplicationId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string TargetIdentity { get; set; } = string.Empty;
    public long PriorRevision { get; set; }
    public long NewRevision { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int? AuthorizedDeliveryCount { get; set; }
    public int? StreamingDeliveryCount { get; set; }
    public int? InterruptedDeliveryCount { get; set; }
    public byte[] PayloadDigest { get; set; } = [];
    public byte[]? PriorScopesDigest { get; set; }
    public byte[]? NewScopesDigest { get; set; }
    public byte[] EvidenceDigest { get; set; } = [];
    public DateTimeOffset OccurredAtUtc { get; set; }
}
