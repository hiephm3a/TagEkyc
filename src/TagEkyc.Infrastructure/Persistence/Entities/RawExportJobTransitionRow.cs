namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportJobTransitionRow
{
    public Guid TransitionId { get; set; }
    public Guid JobId { get; set; }
    public long ResultingRevision { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? FromState { get; set; }
    public string ToState { get; set; } = string.Empty;
    public Guid? AttemptId { get; set; }
    public long FencingToken { get; set; }
    public Guid? ResultingLeaseOwnerId { get; set; }
    public DateTimeOffset? ResultingLeaseExpiresAt { get; set; }
    public string? FailureCode { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}
