namespace TagEkyc.Contracts.RawExport;

public enum RawExportAssemblyTopology
{
    Disabled,
    FixtureProof,
    Invalid,
}

public enum C2AssemblyPrepareOutcome
{
    Prepared,
    ExistingMatch,
    Conflict,
    Unavailable,
    OutcomeUnknown,
}

public enum C2AssemblyInspectionOutcome
{
    Missing,
    Preparing,
    Prepared,
    Finalized,
    Aborted,
    Conflict,
    Unavailable,
    OutcomeUnknown,
}

public enum C2AssemblyFinalizeOutcome
{
    Finalized,
    ExistingMatch,
    Conflict,
    Unavailable,
    OutcomeUnknown,
}

public enum C2AssemblyAbortOutcome
{
    Aborted,
    ExistingMatch,
    Conflict,
    Unavailable,
    OutcomeUnknown,
}

public sealed record RawExportAssemblyItemDescriptor(
    int Ordinal,
    string RawClass,
    Guid SourceArtifactId,
    Guid CaptureArtifactId,
    int CaptureRevision,
    string MediaType,
    long PlaintextLength,
    int ContentCommitmentSchemaVersion,
    string ContentCommitmentKeyId,
    int ContentCommitmentKeyVersion,
    byte[] ContentCommitment);

public sealed record RawExportAssemblyHeader(
    Guid AssemblyId,
    Guid ClientApplicationId,
    DateTimeOffset CreatedAtUtc,
    string ExportMode,
    Guid JobId,
    int ManifestVersion,
    Guid PermitId,
    Guid PolicyId,
    int PolicyVersion,
    string PurposeCode,
    Guid RecipientClientApplicationId,
    byte[] SubjectRefToken,
    Guid VerificationSessionId,
    IReadOnlyList<RawExportAssemblyItemDescriptor> Items);

public sealed record C2AssemblyPreparationRequest(
    Guid C2PreparationId,
    Guid AssemblyId,
    byte[] AssemblyFingerprint,
    byte[] ManifestDigest,
    byte[] AssemblyDigest,
    byte[] AssemblyAuthenticationValue,
    long CompleteAssemblyLength);

public sealed record C2AssemblyPrepareResult(
    C2AssemblyPrepareOutcome Outcome,
    byte[]? ProviderReceiptDigest);

public sealed record C2AssemblyInspection(
    C2AssemblyInspectionOutcome Outcome,
    byte[]? AssemblyFingerprint,
    byte[]? ProviderReceiptDigest);

public sealed record C2AssemblyFinalizeResult(C2AssemblyFinalizeOutcome Outcome);

public sealed record C2AssemblyAbortResult(C2AssemblyAbortOutcome Outcome);

public interface IC2AssemblyPreparationProvider
{
    Task<C2AssemblyPrepareResult> PrepareAsync(
        C2AssemblyPreparationRequest request,
        Func<Stream, CancellationToken, Task> boundedAssemblyWriter,
        CancellationToken cancellationToken);

    Task<C2AssemblyInspection> GetPreparationAsync(
        Guid c2PreparationId,
        CancellationToken cancellationToken);

    Task<C2AssemblyFinalizeResult> FinalizeAsync(
        Guid c2PreparationId,
        byte[] assemblyFingerprint,
        CancellationToken cancellationToken);

    Task<C2AssemblyAbortResult> AbortAsync(
        Guid c2PreparationId,
        byte[] abortAuthorizationDigest,
        CancellationToken cancellationToken);
}

public interface IRawExportAssemblyAuthenticationProvider
{
    string KeyId { get; }

    int KeyVersion { get; }

    Task<byte[]> AuthenticateManifestAsync(
        ReadOnlyMemory<byte> authenticationPayload,
        CancellationToken cancellationToken);
}

public enum RawExportAssemblyExecutionOutcome
{
    Sealed,
    ExistingMatch,
    NotFoundOrNotAllowed,
    AuthorityInvalid,
    SourceUnavailable,
    BindingConflict,
    AssemblyConflict,
    PreparationConflict,
    LeaseLost,
    Expired,
    StateConflict,
    ProviderUnavailable,
    ProviderOutcomeUnknown,
    VerificationIndeterminate,
}

public sealed record RawExportAssemblyExecutionRequest(
    Guid JobId,
    Guid AttemptId,
    long ExpectedJobRevision,
    long ExpectedFence,
    Guid ActorPrincipalId);

public sealed record RawExportAssemblyExecutionResult(
    RawExportAssemblyExecutionOutcome Outcome,
    Guid? AssemblyId,
    Guid? C2PreparationId,
    long? JobRevision,
    long? PreparationRevision);
