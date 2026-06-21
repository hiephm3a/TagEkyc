namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class VerificationSessionRow
{
    public Guid Id { get; set; }
    public Guid ClientApplicationId { get; set; }
    public string SubjectRef { get; set; } = string.Empty;
    public string Profile { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string RequiredChecksJson { get; set; } = "[]";
    public string? ExternalSessionId { get; set; }
    public string? ExternalTransactionId { get; set; }
    public string? BindingNonceHash { get; set; }
    public string RequestId { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public string AssuranceLevel { get; set; } = string.Empty;
    public Guid? FinalDecisionId { get; set; }
    public Guid? EvidencePackageId { get; set; }
    public string? EvidencePackageHash { get; set; }
    public string? ManifestHash { get; set; }
    public string PolicySnapshotId { get; set; } = string.Empty;
    public string RetentionClass { get; set; } = string.Empty;
    public string DeletionEligibility { get; set; } = string.Empty;
    public string LegalHoldStatus { get; set; } = string.Empty;
    public string PurgeBlockReason { get; set; } = string.Empty;
    public bool AccessAuditRequired { get; set; }
    public string? ActorId { get; set; }
    public string? ActorCategory { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
