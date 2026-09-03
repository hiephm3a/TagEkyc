namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportSourcePublicationRow
{
    public Guid SourcePublicationId { get; set; }
    public Guid SourceArtifactId { get; set; }
    public Guid AttemptId { get; set; }
    public Guid ObjectCustodyId { get; set; }
    public Guid AttemptKeyReservationId { get; set; }
    public int StagedCiphertextFingerprintSchemaVersion { get; set; }
    public byte[] StagedCiphertextFingerprint { get; set; } = [];
    public int CommittedAuthoritySnapshotSchemaVersion { get; set; }
    public Guid CommittedAuthoritySnapshotId { get; set; }
    public long CommittedAuthorityRevision { get; set; }
    public Guid CommittedConsentPolicyId { get; set; }
    public int CommittedConsentPolicyVersion { get; set; }
    public byte[] CommitEvidenceDigest { get; set; } = [];
    public DateTimeOffset CommittedAtUtc { get; set; }
    public string PublicationState { get; set; } = string.Empty;
    public Guid? OpaqueCommittedLocatorId { get; set; }
    public int? PublishedAuthoritySnapshotSchemaVersion { get; set; }
    public Guid? PublishedAuthoritySnapshotId { get; set; }
    public long? PublishedAuthorityRevision { get; set; }
    public Guid? PublishedConsentPolicyId { get; set; }
    public int? PublishedConsentPolicyVersion { get; set; }
    public byte[]? AvailableEvidenceDigest { get; set; }
    public DateTimeOffset? AvailableAtUtc { get; set; }
    public string CleanupDisposition { get; set; } = string.Empty;
    public byte[]? CleanupEvidenceDigest { get; set; }
    public DateTimeOffset? FinalizedAtUtc { get; set; }
    public long PublicationRevision { get; set; }
    public int SchemaVersion { get; set; }
}
