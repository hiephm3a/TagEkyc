namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportRecipientManagementOperationRow
{
    public Guid OperationId { get; set; }
    public Guid ManagerApiKeyId { get; set; }
    public Guid ManagerPrincipalId { get; set; }
    public byte[] IdempotencyKeyDigest { get; set; } = [];
    public byte[] EqualityFingerprint { get; set; } = [];
    public byte[] PayloadDigest { get; set; } = [];
    public DateTimeOffset? AdmissionAtUtc { get; set; }
    public string OperationKind { get; set; } = string.Empty;
    public Guid RecipientClientApplicationId { get; set; }
    public string? Outcome { get; set; }
    public long? ResultIdentityRevision { get; set; }
    public Guid? ResultApiKeyId { get; set; }
    public int? ResultCredentialVersion { get; set; }
    public string? ResultKeyId { get; set; }
    public int? ResultKeyVersion { get; set; }
    public long? ResultRevision { get; set; }
    public int? AuthorizedDeliveryCount { get; set; }
    public int? StreamingDeliveryCount { get; set; }
    public int? InterruptedDeliveryCount { get; set; }
    public string? ResultSnapshot { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
}
