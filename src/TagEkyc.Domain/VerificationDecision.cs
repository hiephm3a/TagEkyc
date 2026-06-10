namespace TagEkyc.Domain;

public sealed record VerificationDecision(
    Guid Id,
    Guid VerificationSessionId,
    VerificationResult Result,
    AssuranceLevel AssuranceLevel,
    decimal? RiskScore,
    IReadOnlyList<RequiredCheckType> FailedChecks,
    IReadOnlyList<RequiredCheckType> CompletedChecks,
    IReadOnlyList<string> DecisionReasonCodes,
    IReadOnlyList<string> RetryReasonCodes,
    DateTimeOffset CreatedAt);
