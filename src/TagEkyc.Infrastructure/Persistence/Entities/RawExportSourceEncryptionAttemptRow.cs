namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportSourceEncryptionAttemptRow
{
    public Guid AttemptId { get; set; }
    public Guid SourceArtifactId { get; set; }
    public long EncryptionAttemptRevision { get; set; }
    public long Fence { get; set; }
    public Guid ProvisionalObjectIdentity { get; set; }
    public Guid AttemptKeyReservationId { get; set; }
    public string KeyProviderId { get; set; } = string.Empty;
    public string KekId { get; set; } = string.Empty;
    public int KekVersion { get; set; }
    public string KekFingerprint { get; set; } = string.Empty;
    public string EncryptionSuiteId { get; set; } = string.Empty;
    public int EncryptionFramingVersion { get; set; }
    public string NonceStrategyId { get; set; } = string.Empty;
    public string NonceDerivationSeedReferenceOrWrappedSeed { get; set; } = string.Empty;
    public byte[] NonceDerivationSeedCommitment { get; set; } = [];
    public int ChunkSize { get; set; }
    public byte[] FramingParametersDigest { get; set; } = [];
    public byte[] EncryptionAttemptFingerprint { get; set; } = [];
    public DateTimeOffset OwnershipLeaseExpiresAtUtc { get; set; }
    public string? R2TerminationDisposition { get; set; }
    public DateTimeOffset? R2TerminatedAtUtc { get; set; }
    public int? StagedCiphertextFingerprintSchemaVersion { get; set; }
    public byte[]? StagedCiphertextFingerprint { get; set; }
    public Guid? StagedObjectCustodyId { get; set; }
    public long? StagedObjectStateRevision { get; set; }
    public long? StagedFromReservationRevision { get; set; }
    public long? VerifiedPlaintextLength { get; set; }
    public long? StagedCiphertextLength { get; set; }
    public byte[]? StagedCiphertextDigest { get; set; }
    public byte[]? StagedProviderReceiptDigest { get; set; }
    public byte[]? StagedVerificationEvidenceDigest { get; set; }
    public DateTimeOffset? StagedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public int SchemaVersion { get; set; }
}
