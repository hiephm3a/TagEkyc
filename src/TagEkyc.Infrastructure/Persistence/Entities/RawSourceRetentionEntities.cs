namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawSourceConsentReferenceRow
{
    public Guid ConsentReferenceId { get; set; }
    public Guid ClientApplicationId { get; set; }
    public string SubjectRef { get; set; } = null!;
    public string ExternalConsentArtifactRef { get; set; } = null!;
    public long CurrentRevision { get; set; }
}

public sealed class RawSourceConsentReferenceEventRow
{
    public Guid ConsentReferenceId { get; set; }
    public long Revision { get; set; }
    public string EventType { get; set; } = null!;
    public string SourceVersion { get; set; } = null!;
    public string ConsentTextVersion { get; set; } = null!;
    public string ConsentTextContentHash { get; set; } = null!;
    public DateTimeOffset ValidFromUtc { get; set; }
    public DateTimeOffset ValidUntilUtc { get; set; }
    public Guid RecordedByPrincipalId { get; set; }
    public DateTimeOffset RecordedAtUtc { get; set; }
    public string OperationDomain { get; set; } = null!;
    public Guid IdempotencyKey { get; set; }
    public byte[] RequestFingerprint { get; set; } = null!;
    public string? DecisionRef { get; set; }
}

public sealed class RawSourceConsentBindingRow
{
    public Guid ConsentBindingId { get; set; }
    public Guid ConsentReferenceId { get; set; }
    public long ConsentReferenceRevision { get; set; }
    public Guid PrincipalId { get; set; }
    public Guid ClientApplicationId { get; set; }
    public Guid VerificationSessionId { get; set; }
    public string SubjectRef { get; set; } = null!;
    public Guid IdempotencyKey { get; set; }
    public byte[] RequestFingerprint { get; set; } = null!;
    public DateTimeOffset RecordedAtUtc { get; set; }
}

public sealed class RawSourceRetentionPermitRow
{
    public Guid RetentionAuthorityId { get; set; }
    public long Revision { get; set; }
    public Guid ConsentBindingId { get; set; }
    public Guid PrincipalId { get; set; }
    public Guid ClientApplicationId { get; set; }
    public Guid VerificationSessionId { get; set; }
    public Guid PolicyId { get; set; }
    public int PolicyVersion { get; set; }
    public string Purpose { get; set; } = null!;
    public DateTimeOffset IssuedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public string ControllerIdentity { get; set; } = null!;
    public string StableDataScopeId { get; set; } = null!;
    public string RetentionPolicyId { get; set; } = null!;
    public int RetentionPolicyVersion { get; set; }
    public string RetentionClass { get; set; } = null!;
    public string RetentionStartEvent { get; set; } = null!;
    public string RevocationPolicyId { get; set; } = null!;
    public string PurgePolicyId { get; set; } = null!;
    public string LegalHoldPolicyId { get; set; } = null!;
    public int MaximumRetentionSeconds { get; set; }
    public Guid IssueOperationId { get; set; }
    public byte[] RequestFingerprint { get; set; } = null!;
}

public sealed class RawSourceRetentionPermitClassRow
{
    public Guid RetentionAuthorityId { get; set; }
    public long Revision { get; set; }
    public string RawClass { get; set; } = null!;
}
