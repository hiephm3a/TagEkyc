namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportOpenBaoKekOperationJournalRow
{
    public Guid ProviderOperationId { get; set; }
    public byte[] ProviderOperationTokenDigest { get; set; } = [];
    public byte[] AttemptKeyContextFingerprint { get; set; } = [];
    public Guid PreparationId { get; set; }
    public long PreparationFence { get; set; }
    public string KeyProviderId { get; set; } = string.Empty;
    public string KekId { get; set; } = string.Empty;
    public int KekVersion { get; set; }
    public string KekFingerprint { get; set; } = string.Empty;
    public string JournalState { get; set; } = string.Empty;
    public string MaterialRepresentationId { get; set; } = string.Empty;
    public int MaterialRepresentationVersion { get; set; }
    public string WrappingSchemeId { get; set; } = string.Empty;
    public int WrappingSchemeVersion { get; set; }
    public byte[]? OpaqueWrappedDekPayload { get; set; }
    public string? ProviderResourceReference { get; set; }
    public string? ProviderOperationReceipt { get; set; }
    public string? ProviderAbsenceProofReceipt { get; set; }
    public string? ProviderCleanupReference { get; set; }
    public string? ProviderCleanupReceipt { get; set; }
    public long RowRevision { get; set; }
    public DateTimeOffset IssuedAtUtc { get; set; }
    public DateTimeOffset? ResultObservedAtUtc { get; set; }
    public DateTimeOffset? CleanedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
