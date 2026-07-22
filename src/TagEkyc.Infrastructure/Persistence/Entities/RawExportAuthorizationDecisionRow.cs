namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportAuthorizationDecisionRow
{
    public Guid ExportDecisionId { get; set; }
    public Guid PrincipalId { get; set; }
    public Guid ClientApplicationId { get; set; }
    public Guid ApiKeyId { get; set; }
    public Guid RequestedVerificationSessionId { get; set; }
    public Guid PolicyId { get; set; }
    public int PolicyVersion { get; set; }
    public byte[] FingerprintHash { get; set; } = [];
    public string RawClassSelectionMode { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
    public string? PrimaryCause { get; set; }
    public Guid? ResolvedVerificationSessionId { get; set; }
    public Guid? SessionOwnerClientApplicationId { get; set; }
    public string? SessionSubjectRef { get; set; }
    public string? SessionState { get; set; }
    public int? BoundRuleSetVersion { get; set; }
    public int? CurrentRuleSetVersion { get; set; }
    public string? EligibilityPrimaryCause { get; set; }
    public DateTimeOffset? EligibilityEvaluatedAtUtc { get; set; }
    public Guid? GrantPrincipalId { get; set; }
    public Guid? GrantPolicyId { get; set; }
    public int? GrantPolicyVersion { get; set; }
    public int? GrantRevision { get; set; }
    public Guid? LifecyclePolicyId { get; set; }
    public int? LifecyclePolicyVersion { get; set; }
    public int? LifecycleRevision { get; set; }
    public string? PurposeCode { get; set; }
    public Guid? RecipientClientApplicationId { get; set; }
    public string? SubjectConsentCause { get; set; }
    public byte[]? ConsentScopeHash { get; set; }
    public Guid? SubjectConsentRecordId { get; set; }
    public int? ConsentRevision { get; set; }
    public DateTimeOffset? ConsentValidFromUtc { get; set; }
    public DateTimeOffset? ConsentValidUntilUtc { get; set; }
    public DateTimeOffset? ConsentEvaluatedAtUtc { get; set; }
    public int? PolicyPermitTtlSeconds { get; set; }
    public DateTimeOffset? DecisionExpiresAtUtc { get; set; }
    public DateTimeOffset DecidedAtUtc { get; set; }
}
