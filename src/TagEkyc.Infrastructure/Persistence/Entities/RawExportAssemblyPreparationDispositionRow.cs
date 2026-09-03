namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportAssemblyPreparationDispositionRow
{
    public Guid C2PreparationId { get; set; }
    public Guid AssemblyId { get; set; }
    public Guid JobId { get; set; }
    public Guid AttemptId { get; set; }
    public long FencingToken { get; set; }
    public byte[] AssemblyFingerprint { get; set; } = [];
    public byte[] PreparationFingerprint { get; set; } = [];
    public string Disposition { get; set; } = string.Empty;
    public byte[]? ProviderReceiptDigest { get; set; }
    public byte[]? AbortAuthorizationDigest { get; set; }
    public long RowRevision { get; set; }
    public DateTimeOffset PreparingAtUtc { get; set; }
    public DateTimeOffset? PendingAtUtc { get; set; }
    public DateTimeOffset? SealCommittedAtUtc { get; set; }
    public DateTimeOffset? FinalizedAtUtc { get; set; }
    public DateTimeOffset? AbortAuthorizedAtUtc { get; set; }
    public DateTimeOffset? AbortedAtUtc { get; set; }
    public int SchemaVersion { get; set; }
}
