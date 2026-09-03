namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportJobOperationalHeadRow
{
    public Guid JobId { get; set; }
    public string CurrentState { get; set; } = string.Empty;
    public long Revision { get; set; }
    public Guid? CurrentAttemptId { get; set; }
    public Guid? LeaseOwnerId { get; set; }
    public DateTimeOffset? LeaseExpiresAt { get; set; }
    public long FencingToken { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
