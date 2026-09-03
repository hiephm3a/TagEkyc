namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportJobAttemptRow
{
    public Guid AttemptId { get; set; }
    public Guid JobId { get; set; }
    public int AttemptOrdinal { get; set; }
    public string Phase { get; set; } = string.Empty;
    public Guid LeaseOwnerId { get; set; }
    public long FencingToken { get; set; }
    public DateTimeOffset AcquiredAt { get; set; }
    public DateTimeOffset InitialLeaseExpiresAt { get; set; }
}
