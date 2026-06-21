namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class EvidenceResultRow
{
    public Guid Id { get; set; }
    public Guid VerificationSessionId { get; set; }
    public Guid? VerificationCheckId { get; set; }
    public string ResultType { get; set; } = string.Empty;
    public string InputCaptureArtifactIdsJson { get; set; } = "[]";
    public string Result { get; set; } = string.Empty;
    public decimal? Confidence { get; set; }
    public string ReasonCodesJson { get; set; } = "[]";
    public string? RetryReasonCode { get; set; }
    public string? SanitizedSummaryRef { get; set; }
    public string? PayloadHash { get; set; }
    public string PayloadSignatureStatus { get; set; } = string.Empty;
    public string EngineName { get; set; } = string.Empty;
    public string EngineVersion { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
