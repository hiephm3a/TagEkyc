namespace TagEkyc.Domain;

public sealed record AuditEvent(
    Guid Id,
    Guid ClientApplicationId,
    Guid? VerificationSessionId,
    string ActorType,
    string? ActorId,
    string EventType,
    HashRef EventPayloadHash,
    string? EventPayloadRef,
    string RequestId,
    string CorrelationId,
    DateTimeOffset OccurredAt);
