namespace TagEkyc.Infrastructure.RawExport;

internal enum RawExportR2WriterDisposition
{
    PreCustodyRejected,
    PreCustodyRetryable,
    PreCustodyTerminalKey,
    PreCustodyOperatorRequired,
    PendingVerification,
    ReconciliationRequired,
    NotArmed,
    CustodyStateConflict,
}

internal enum RawExportR2VerificationDisposition
{
    Verified,
    VerificationIndeterminateRetry,
    VerificationFailedRequiresCleanup,
}

internal sealed record RawExportR2EncryptionRequest(
    Guid ActorPrincipalId,
    Guid AttemptKeyReservationId,
    Guid AttemptId,
    Guid SourceArtifactId,
    long ExpectedEncryptionAttemptRevision,
    long ExpectedFence,
    Stream PlaintextSource);

internal sealed record RawExportR2WriterResult(
    Guid AttemptId,
    Guid? ObjectCustodyId,
    string? ObjectState,
    long? StateRevision,
    RawExportR2WriterDisposition Disposition)
{
    public override string ToString() =>
        $"RawExportR2WriterResult:{Disposition}:Attempt={AttemptId:N}:<redacted>";
}

internal sealed record RawExportR2VerificationRequest(
    Guid ActorPrincipalId,
    Guid AttemptId,
    long ExpectedEncryptionAttemptRevision,
    long ExpectedFence,
    Guid ObjectCustodyId);

internal sealed record RawExportR2VerifierResult(
    Guid AttemptId,
    Guid ObjectCustodyId,
    long? VerifiedPlaintextLength,
    byte[]? ContentCommitment,
    long? CiphertextLength,
    byte[]? CiphertextDigest,
    byte[]? VerificationEvidenceDigest,
    RawExportR2VerificationDisposition Disposition)
{
    internal static RawExportR2VerifierResult Failure(
        Guid attemptId,
        Guid objectCustodyId,
        RawExportR2VerificationDisposition disposition) =>
        new(attemptId, objectCustodyId, null, null, null, null, null, disposition);

    public override string ToString() =>
        $"RawExportR2VerifierResult:{Disposition}:Attempt={AttemptId:N}:Object={ObjectCustodyId:N}:<redacted>";
}

internal sealed record RawExportR2EncryptionContext(
    Guid AttemptId,
    Guid SourceArtifactId,
    long EncryptionAttemptRevision,
    long Fence,
    Guid AttemptKeyReservationId,
    Guid ProvisionalObjectIdentity,
    byte[] EncryptionAttemptFingerprint,
    string KeyProviderId,
    string KekId,
    int KekVersion,
    string KekFingerprint,
    string EncryptionSuiteId,
    int EncryptionFramingVersion,
    int ChunkSize,
    string NonceStrategyId,
    string NonceDerivationSeedReferenceOrWrappedSeed,
    byte[] NonceDerivationSeedCommitment,
    byte[] FramingParametersDigest,
    byte[] WrappedDekMetadataDigest,
    Guid VerificationSessionId,
    Guid CaptureArtifactId,
    int CaptureRevision,
    string RawClass,
    string StableDataScopeId,
    string ControllerIdentity,
    long ClaimedPlaintextLength,
    string MediaType,
    int ContentCommitmentSchemaVersion,
    string ContentCommitmentKeyId,
    int ContentCommitmentKeyVersion,
    byte[] ContentCommitment,
    DateTimeOffset OwnershipLeaseExpiresAtUtc,
    DateTimeOffset EffectivePlaintextRetentionExpiresAtUtc,
    DateTimeOffset ReservationExpiresAtUtc);

internal sealed record RawExportR2VerificationContext(
    Guid AttemptId,
    Guid SourceArtifactId,
    long EncryptionAttemptRevision,
    long Fence,
    Guid AttemptKeyReservationId,
    Guid ProvisionalObjectIdentity,
    byte[] EncryptionAttemptFingerprint,
    string KeyProviderId,
    string KekId,
    int KekVersion,
    string KekFingerprint,
    string EncryptionSuiteId,
    int EncryptionFramingVersion,
    int ChunkSize,
    string NonceStrategyId,
    byte[] FramingParametersDigest,
    byte[] WrappedDekMetadataDigest,
    Guid VerificationSessionId,
    Guid CaptureArtifactId,
    int CaptureRevision,
    string RawClass,
    string StableDataScopeId,
    string ControllerIdentity,
    long ClaimedPlaintextLength,
    string MediaType,
    int ContentCommitmentSchemaVersion,
    string ContentCommitmentKeyId,
    int ContentCommitmentKeyVersion,
    byte[] ContentCommitment);

internal sealed record RawExportR2ObjectContext(
    Guid ObjectCustodyId,
    Guid AttemptId,
    Guid AttemptKeyReservationId,
    Guid SourceArtifactId,
    Guid ProvisionalObjectIdentity,
    long EncryptionAttemptRevision,
    long AttemptFence,
    byte[] EncryptionAttemptFingerprint,
    string ObjectKey,
    byte[] ObjectBindingDigest,
    string State,
    long StateRevision,
    Guid? PutOperationId,
    long? CiphertextLength,
    byte[]? CiphertextDigest,
    byte[]? ProviderReceiptDigest,
    byte[]? VerificationEvidenceDigest);

internal sealed record RawExportR2KeyInspection(
    string PreparationDisposition,
    Guid AttemptId,
    Guid? CurrentPreparationId,
    long CurrentPreparationFence,
    byte[]? WrappedDekMetadataDigest);

internal sealed class RawExportR2DeterministicInvalidException(string code)
    : IOException(code)
{
    internal string Code { get; } = code;
}
