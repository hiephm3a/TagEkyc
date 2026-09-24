namespace TagEkyc.Contracts.RawExport;

// API-internal metadata result. It is not an Agent credential or a public R27 DTO.
public sealed record RawIngressBrokerHandoff(
    Guid SourceArtifactId, Guid AttemptKeyReservationId, Guid AttemptId,
    long ExpectedEncryptionAttemptRevision, long ExpectedFence,
    Guid CustodyActorPrincipalId, Guid ClientApplicationId, Guid BindingId,
    Guid RetentionAuthorityId, long RetentionAuthorityRevision,
    DateTimeOffset ExecutionExpiresAtUtc);

public abstract record RawIngressBrokerResult
{
    private RawIngressBrokerResult() { }
    public sealed record Handoff(RawIngressBrokerHandoff Value) : RawIngressBrokerResult;
    public sealed record Final(CaptureAgentFinalResult Value,
        RawIngressBrokerFinalOrigin Origin = RawIngressBrokerFinalOrigin.PreAdmission) : RawIngressBrokerResult;
}

// In-process provenance of the owning SQL branch, not an extra wire field.
public enum RawIngressBrokerFinalOrigin
{
    PreAdmission, PublishedReplay, PreservedCiphertext, PersistedTerminal
}
