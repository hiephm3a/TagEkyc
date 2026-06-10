namespace TagEkyc.Domain;

public sealed record EvidenceResult(
    Guid Id,
    Guid VerificationSessionId,
    Guid? VerificationCheckId,
    EvidenceResultType ResultType,
    IReadOnlyList<Guid> InputCaptureArtifactIds,
    VerificationResult Result,
    decimal? Confidence,
    IReadOnlyList<string> ReasonCodes,
    string? RetryReasonCode,
    string? SanitizedSummaryRef,
    HashRef? PayloadHash,
    SignaturePlaceholderStatus PayloadSignatureStatus,
    string EngineName,
    string EngineVersion,
    string RequestId,
    string CorrelationId,
    DateTimeOffset CreatedAt);
