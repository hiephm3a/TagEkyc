namespace TagEkyc.Infrastructure.RawExport;

internal sealed record RawExportR3StageCommand(
    Guid ActorPrincipalId,
    Guid AttemptId,
    Guid ObjectCustodyId,
    long ExpectedReservationRevision,
    long ExpectedEncryptionAttemptRevision,
    long ExpectedFence,
    long ExpectedObjectStateRevision);

internal enum RawExportR3StageDisposition
{
    Staged,
    ExistingMatch,
    NotFound,
    StateConflict,
    SourceRetentionNotAuthorized,
}

internal sealed record RawExportR3StageResult(
    RawExportR3StageDisposition Disposition,
    Guid? SourceArtifactId,
    Guid? AttemptId,
    Guid? ObjectCustodyId,
    int? StagedCiphertextFingerprintSchemaVersion,
    byte[]? StagedCiphertextFingerprint,
    long? ReservationRevision,
    long? Fence,
    DateTimeOffset? StagedAtUtc)
{
    public override string ToString() =>
        $"RawExportR3StageResult:{Disposition}:Attempt={AttemptId?.ToString("N") ?? "none"}:<redacted>";
}

internal sealed record RawExportR3FingerprintInput(
    byte[] EncryptionAttemptFingerprint,
    Guid ObjectCustodyId,
    byte[] ObjectBindingDigest,
    long VerifiedPlaintextLength,
    byte[] ContentCommitment,
    long StagedCiphertextLength,
    byte[] StagedCiphertextDigest,
    byte[] StagedProviderReceiptDigest,
    byte[] StagedVerificationEvidenceDigest);
