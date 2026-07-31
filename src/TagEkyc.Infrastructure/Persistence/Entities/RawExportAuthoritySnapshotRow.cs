namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportAuthoritySnapshotRow
{
    public Guid AuthoritySnapshotEventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public long Revision { get; set; }
    public long? TargetRevision { get; set; }
    public DateTimeOffset? ValidFromUtc { get; set; }
    public DateTimeOffset? ValidUntilUtc { get; set; }
    public DateTimeOffset RecordedAtUtc { get; set; }
    public Guid? CapturedByPrincipalId { get; set; }
    public Guid? WithdrawnByPrincipalId { get; set; }
    public Guid? RevokedByPrincipalId { get; set; }
    public Guid ClientApplicationId { get; set; }
    public Guid VerificationSessionId { get; set; }
    public Guid CaptureAcceptanceId { get; set; }
    public string RawClass { get; set; } = string.Empty;
    public int? AuthoritySnapshotSchemaVersion { get; set; }
    public Guid? AuthoritySnapshotId { get; set; }
    public Guid? AuthorityArtifactId { get; set; }
    public int? AuthorityArtifactVersion { get; set; }
    public string? ControllerIdentity { get; set; }
    public string? ApprovedPurpose { get; set; }
    public string? StableDataScopeId { get; set; }
    public string? RetentionPolicyId { get; set; }
    public int? RetentionPolicyVersion { get; set; }
    public string? RetentionClass { get; set; }
    public string? RetentionStartEvent { get; set; }
    public DateTimeOffset? AbsoluteSourceExpiresAtUtc { get; set; }
    public string? ReuseDisposition { get; set; }
    public string? ExtensionDisposition { get; set; }
    public string? RevocationPolicyId { get; set; }
    public string? PurgePolicyId { get; set; }
    public string? LegalHoldPolicyId { get; set; }
    public DateTimeOffset? EvaluatedAtUtc { get; set; }
}
