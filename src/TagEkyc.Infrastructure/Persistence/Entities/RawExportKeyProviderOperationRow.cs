namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportKeyProviderOperationRow
{
    public Guid ProviderOperationId { get; set; }
    public string KeyProviderId { get; set; } = string.Empty;
    public string ProviderOperationToken { get; set; } = string.Empty;
    public Guid AttemptKeyReservationId { get; set; }
    public Guid PreparationId { get; set; }
    public long PreparationFence { get; set; }
    public byte[] AttemptKeyContextFingerprint { get; set; } = [];
    public string ProviderOperationState { get; set; } = string.Empty;
    public byte[]? WrappedDekCiphertext { get; set; }
    public byte[]? WrappedDekNonce { get; set; }
    public byte[]? WrappedDekTag { get; set; }
    public byte[]? WrappedDekMetadataDigest { get; set; }
    public string? WrappingSuiteId { get; set; }
    public int? WrappingSuiteVersion { get; set; }
    public string? ProviderResourceReference { get; set; }
    public string? ProviderOperationReceipt { get; set; }
    public DateTimeOffset? ResultObservedAtUtc { get; set; }
    public string? ProviderCleanupReference { get; set; }
    public string? ProviderCleanupReceipt { get; set; }
    public string? ProviderAbsenceProofReceipt { get; set; }
    public DateTimeOffset IssuedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
