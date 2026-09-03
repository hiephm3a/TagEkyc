namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportSourceReservationRow
{
    public Guid SourceArtifactId { get; set; }
    public Guid IngressClaimId { get; set; }
    public int AuthoritySnapshotSchemaVersion { get; set; }
    public Guid AuthoritySnapshotId { get; set; }
    public int SubjectRefTokenSchemaVersion { get; set; }
    public string SubjectRefTokenKeyId { get; set; } = string.Empty;
    public int SubjectRefTokenKeyVersion { get; set; }
    public byte[] SubjectRefToken { get; set; } = [];
    public string StorageProfileId { get; set; } = string.Empty;
    public string SourceEncryptionProfileId { get; set; } = string.Empty;
    public int SourceEncryptionProfileVersion { get; set; }
    public DateTimeOffset AbsoluteSourceExpiresAtUtc { get; set; }
    public byte[] AdmissionFingerprint { get; set; } = [];
    public DateTimeOffset EffectivePlaintextRetentionExpiresAtUtc { get; set; }
    public DateTimeOffset ReservationExpiresAtUtc { get; set; }
    public byte[] SourceReservationFingerprint { get; set; } = [];
    public int ContentCommitmentSchemaVersion { get; set; }
    public string ContentCommitmentKeyId { get; set; } = string.Empty;
    public int ContentCommitmentKeyVersion { get; set; }
    public byte[] ContentCommitment { get; set; } = [];
    public long ClaimedPlaintextLength { get; set; }
    public string MediaType { get; set; } = string.Empty;
    public DateTimeOffset CapturedAtUtc { get; set; }
    public DateTimeOffset PlaintextRetentionStartedAtUtc { get; set; }
    public DateTimeOffset PlaintextRetentionExpiresAtUtc { get; set; }
    public int PlaintextRetentionBudgetSeconds { get; set; }
    public string ControllerIdentity { get; set; } = string.Empty;
    public string StableDataScopeId { get; set; } = string.Empty;
    public Guid ConsentPolicyId { get; set; }
    public int ConsentPolicyVersion { get; set; }
    public int SchemaVersion { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
