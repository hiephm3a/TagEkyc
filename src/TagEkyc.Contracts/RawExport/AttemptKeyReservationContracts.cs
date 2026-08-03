namespace TagEkyc.Contracts.RawExport;

public enum AttemptKeyProvisioningOutcome
{
    Activated,
    ExistingMatch,
    InProgress,
    Conflict,
    HeadNotReserved,
    Terminated,
    ProviderUnavailable,
    ProviderOutcomeUnknown,
    ProviderCorruptOrUnverifiable,
    StateConflict,
}

public sealed record AttemptKeyProvisioningRequest(
    Guid AttemptKeyReservationId,
    Guid AttemptId,
    Guid SourceArtifactId);

public sealed record AttemptKeyProvisioningResult(
    AttemptKeyProvisioningOutcome Outcome,
    Guid AttemptKeyReservationId,
    byte[]? WrappedDekMetadataDigest);

public interface IAttemptKeyReservationProvisioningOperation
{
    Task<AttemptKeyProvisioningResult> ProvisionAsync(
        AttemptKeyProvisioningRequest request,
        CancellationToken cancellationToken);
}
