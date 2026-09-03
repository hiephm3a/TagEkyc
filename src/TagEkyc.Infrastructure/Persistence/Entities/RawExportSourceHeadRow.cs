namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportSourceHeadRow
{
    public Guid SourceArtifactId { get; set; }
    public string CustodyState { get; set; } = string.Empty;
    public Guid CurrentEncryptionAttemptId { get; set; }
    public long ReservationRevision { get; set; }
    public long Fence { get; set; }
}
