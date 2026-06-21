namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class VerificationDecisionRow
{
    public Guid Id { get; set; }
    public Guid VerificationSessionId { get; set; }
    public string Result { get; set; } = string.Empty;
    public string AssuranceLevel { get; set; } = string.Empty;
    public decimal? RiskScore { get; set; }
    public string FailedChecksJson { get; set; } = "[]";
    public string CompletedChecksJson { get; set; } = "[]";
    public string DecisionReasonCodesJson { get; set; } = "[]";
    public string RetryReasonCodesJson { get; set; } = "[]";
    public DateTimeOffset CreatedAt { get; set; }
}
