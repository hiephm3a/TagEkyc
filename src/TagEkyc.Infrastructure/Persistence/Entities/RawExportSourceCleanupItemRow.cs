namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportSourceCleanupItemRow
{
    public Guid CleanupItemId { get; set; }
    public Guid SourcePublicationId { get; set; }
    public string ResourceKind { get; set; } = string.Empty;
    public Guid ResourceId { get; set; }
    public Guid ResourceAttemptId { get; set; }
    public long PlannedResourceRevision { get; set; }
    public string CleanupState { get; set; } = string.Empty;
    public string? CompletionDisposition { get; set; }
    public byte[]? CleanupEvidenceDigest { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public long RowRevision { get; set; }
    public int SchemaVersion { get; set; }
}
