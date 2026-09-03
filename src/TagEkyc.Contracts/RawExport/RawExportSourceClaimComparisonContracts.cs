namespace TagEkyc.Contracts.RawExport;

public sealed record RawExportClaimEvaluationToken(
    Guid EvaluationId,
    long Revision,
    long Fence,
    string Variant,
    DateTimeOffset ExpiresAtUtc,
    string Value);

public sealed record RawExportSourceClaimComparisonCommand(
    Guid ActorPrincipalId,
    Guid ClientApplicationId,
    string ProducerId,
    string CaptureAgentInstanceId,
    Guid IngressIdempotencyKey,
    RawExportClaimEvaluationToken Token,
    long ClaimedPlaintextLength,
    string MediaType,
    DateTimeOffset CapturedAtUtc,
    DateTimeOffset PlaintextRetentionStartedAtUtc,
    DateTimeOffset PlaintextRetentionExpiresAtUtc,
    int PlaintextRetentionBudgetSeconds,
    ReadOnlyMemory<byte> ClaimedPlaintextDigest);

public enum RawExportSourceClaimComparisonOutcome
{
    NewReservation,
    ExistingMatch,
    FingerprintConflict,
    HistoricCommitmentKeyUnavailable,
    SourceRetentionNotAuthorized,
    ClaimTokenInvalid,
    PlaintextRetentionInvalid,
}

public sealed record RawExportSourceClaimComparisonResult(
    RawExportSourceClaimComparisonOutcome Outcome,
    Guid? SourceArtifactId);

public interface IRawExportSourceClaimComparisonBroker
{
    Task<RawExportSourceClaimComparisonResult> CompleteNewCandidateAsync(
        RawExportSourceClaimComparisonCommand command,
        CancellationToken cancellationToken);

    Task<RawExportSourceClaimComparisonResult> CompleteExistingCandidateAsync(
        RawExportSourceClaimComparisonCommand command,
        CancellationToken cancellationToken);
}
