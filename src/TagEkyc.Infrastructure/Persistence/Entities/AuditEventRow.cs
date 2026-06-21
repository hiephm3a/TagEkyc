namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class AuditEventRow
{
    public Guid Id { get; set; }
    public Guid ClientApplicationId { get; set; }
    public Guid? VerificationSessionId { get; set; }
    public string ActorType { get; set; } = string.Empty;
    public string? ActorId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EventPayloadHash { get; set; } = string.Empty;
    public string? EventPayloadRef { get; set; }
    public string RequestId { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
}
