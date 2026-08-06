namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportProvisionalObjectRow
{
    public Guid ObjectCustodyId { get; set; }
    public Guid AttemptId { get; set; }
    public Guid AttemptKeyReservationId { get; set; }
    public Guid SourceArtifactId { get; set; }
    public Guid ProvisionalObjectIdentity { get; set; }
    public long EncryptionAttemptRevision { get; set; }
    public long AttemptFence { get; set; }
    public byte[] EncryptionAttemptFingerprint { get; set; } = [];
    public string ObjectKey { get; set; } = string.Empty;
    public byte[] ObjectBindingDigest { get; set; } = [];
    public string State { get; set; } = string.Empty;
    public long StateRevision { get; set; }
    public Guid? PutOperationId { get; set; }
    public DateTimeOffset? PutArmedAtUtc { get; set; }
    public string? PutOutcomeKind { get; set; }
    public DateTimeOffset? OutcomeObservedAtUtc { get; set; }
    public long? CiphertextLength { get; set; }
    public byte[]? CiphertextDigest { get; set; }
    public byte[]? ProviderReceiptDigest { get; set; }
    public byte[]? VerificationEvidenceDigest { get; set; }
    public DateTimeOffset? VerifiedAtUtc { get; set; }
    public string? CleanupReasonCode { get; set; }
    public byte[]? CleanupEvidenceDigest { get; set; }
    public DateTimeOffset? CleanupRequestedAtUtc { get; set; }
    public byte[]? DeletionEvidenceDigest { get; set; }
    public string? DeletionEvidenceKind { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }
    public string? QuarantineReasonCode { get; set; }
    public byte[]? QuarantineEvidenceDigest { get; set; }
    public DateTimeOffset? QuarantinedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public int SchemaVersion { get; set; }
}
