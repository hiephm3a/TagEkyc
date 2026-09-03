namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportProvisionalObjectEventRow
{
    public Guid ObjectCustodyEventId { get; set; }
    public Guid ObjectCustodyId { get; set; }
    public long EventSequence { get; set; }
    public string? FromState { get; set; }
    public string ToState { get; set; } = string.Empty;
    public string ActorKind { get; set; } = string.Empty;
    public long StateRevision { get; set; }
    public byte[] EvidenceDigest { get; set; } = [];
    public DateTimeOffset EventAtUtc { get; set; }
    public int SchemaVersion { get; set; }
}
