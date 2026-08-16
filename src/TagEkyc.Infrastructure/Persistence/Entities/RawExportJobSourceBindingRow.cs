namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportJobSourceBindingRow
{
    public Guid JobSourceBindingId { get; set; }
    public Guid JobId { get; set; }
    public int Ordinal { get; set; }
    public string RawClass { get; set; } = string.Empty;
    public Guid VerificationSessionId { get; set; }
    public Guid SessionCaptureSelectionId { get; set; }
    public Guid CaptureAcceptanceId { get; set; }
    public Guid CaptureArtifactId { get; set; }
    public int CaptureRevision { get; set; }
    public Guid SourceArtifactId { get; set; }
    public Guid SourcePublicationId { get; set; }
    public long SourcePublicationRevision { get; set; }
    public Guid EncryptionAttemptId { get; set; }
    public long EncryptionAttemptRevision { get; set; }
    public long EncryptionAttemptFence { get; set; }
    public Guid AttemptKeyReservationId { get; set; }
    public Guid ObjectCustodyId { get; set; }
    public long ObjectStateRevision { get; set; }
    public int SubjectRefTokenSchemaVersion { get; set; }
    public string SubjectRefTokenKeyId { get; set; } = string.Empty;
    public int SubjectRefTokenKeyVersion { get; set; }
    public byte[] SubjectRefToken { get; set; } = [];
    public int ContentCommitmentSchemaVersion { get; set; }
    public string ContentCommitmentKeyId { get; set; } = string.Empty;
    public int ContentCommitmentKeyVersion { get; set; }
    public byte[] ContentCommitment { get; set; } = [];
    public long PlaintextLength { get; set; }
    public string MediaType { get; set; } = string.Empty;
    public int AuthoritySnapshotSchemaVersion { get; set; }
    public Guid AuthoritySnapshotId { get; set; }
    public long AuthorityRevision { get; set; }
    public Guid ConsentPolicyId { get; set; }
    public int ConsentPolicyVersion { get; set; }
    public DateTimeOffset AbsoluteSourceExpiresAtUtc { get; set; }
    public DateTimeOffset EffectivePlaintextRetentionExpiresAtUtc { get; set; }
    public string StableDataScopeId { get; set; } = string.Empty;
    public string ControllerIdentity { get; set; } = string.Empty;
    public byte[] BindingFingerprint { get; set; } = [];
    public DateTimeOffset CreatedAtUtc { get; set; }
    public int SchemaVersion { get; set; }
}
