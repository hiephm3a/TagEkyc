namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportAttemptKeyPreparationEventRow
{
    public Guid PreparationEventId { get; set; }
    public Guid AttemptKeyReservationId { get; set; }
    public Guid PreparationId { get; set; }
    public long PreparationFence { get; set; }
    public long EventSequence { get; set; }
    public string EventKind { get; set; } = string.Empty;
    public string? ResolutionKind { get; set; }
    public string? CleanupResultKind { get; set; }
    public string? ProviderOperationToken { get; set; }
    public string? ProviderOperationReceipt { get; set; }
    public string? ProviderCleanupReference { get; set; }
    public string? ProviderCleanupReceipt { get; set; }
    public byte[]? ProviderResolutionEvidenceDigest { get; set; }
    public byte[]? ProviderCleanupEvidenceDigest { get; set; }
    public byte[]? CleanupObservationEvidenceDigest { get; set; }
    public byte[]? WrappedDekMetadataDigest { get; set; }
    public string? WrappingSuiteId { get; set; }
    public int? WrappingSuiteVersion { get; set; }
    public string? RevocationReasonCode { get; set; }
    public byte[]? RevocationEvidenceDigest { get; set; }
    public string? OperatorReasonCode { get; set; }
    public string? RequestingActorEvidence { get; set; }
    public string? FinalizingActorEvidence { get; set; }
    public Guid? AbandonRequestPreparationEventId { get; set; }
    public byte[]? AbandonmentEvidenceDigest { get; set; }
    public long? HeadRowRevision { get; set; }
    public DateTimeOffset EventAtUtc { get; set; }
}
