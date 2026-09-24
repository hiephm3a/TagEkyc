namespace TagEkyc.Infrastructure.RawExport;

// Metadata only. This snapshot does not authorize a stage or reconstruct a body.
// Each existing stage revalidates its own authority and CAS in its transaction.
internal sealed record RawSourceRetentionContinuation(
    Guid SourceArtifactId, Guid CustodyPrincipalId, Guid ClientApplicationId,
    Guid VerificationSessionId, Guid RuntimeBindingId, Guid RetentionAuthorityId,
    long RetentionAuthorityRevision, string CustodyState, long ReservationRevision, long Fence,
    Guid AttemptId, long EncryptionAttemptRevision, Guid AttemptKeyReservationId,
    Guid? ObjectCustodyId, string? ObjectState, long? ObjectStateRevision,
    Guid? SourcePublicationId, long? PublicationRevision, string? PublicationState, string? CleanupDisposition,
    string? R2TerminalIntentCode, string? R2TerminalIntentDisposition, DateTimeOffset? R2TerminalIntentAtUtc,
    string? R2TerminationDisposition, DateTimeOffset? R2TerminatedAtUtc, string? R2TerminalOutcomeCode);

internal interface IRawSourceRetentionContinuationRepository
{
    Task<RawSourceRetentionContinuation?> ReadAsync(Guid sourceArtifactId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> ScanAsync(Guid? afterSourceArtifactId, int limit, CancellationToken cancellationToken);
}
