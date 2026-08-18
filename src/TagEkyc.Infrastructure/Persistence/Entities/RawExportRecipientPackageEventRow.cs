namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportRecipientPackageEventRow
{
    public Guid C2PreparationId { get; set; }
    public long EventRevision { get; set; }
    public string EventKind { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public byte[]? EvidenceDigest { get; set; }
    public DateTimeOffset RecordedAtUtc { get; set; }
}
