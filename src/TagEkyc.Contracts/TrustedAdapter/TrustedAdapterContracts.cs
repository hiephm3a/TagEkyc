using TagEkyc.Contracts.Common;

namespace TagEkyc.Contracts.TrustedAdapter;

public enum EvidenceResultTypeDto
{
    CaptureQuality = 0,
    DocumentOcr = 1,
    NfcValidation = 2,
    FaceMatch = 3,
    Liveness = 4,
    FingerprintMatch = 5,
    FraudRisk = 6,
}

public sealed record EvidenceResultSubmissionRequestDto(
    EvidenceResultTypeDto ResultType,
    IReadOnlyList<string> InputCaptureArtifactIds,
    VerificationResultDto Result,
    decimal? Confidence,
    IReadOnlyList<string> ReasonCodes,
    string? RetryReasonCode,
    string? SanitizedSummaryRef,
    string? PayloadHash,
    SignaturePlaceholderStatusDto PayloadSignatureStatus,
    string EngineName,
    string EngineVersion,
    string? RequestId,
    string? CorrelationId);

public sealed record EvidenceResultSubmissionResponseDto(
    string EvidenceResultId,
    bool Accepted,
    string SessionState,
    string? NextAction);

public sealed record DocumentResultSubmissionRequestDto(
    string DocumentType,
    string IssuingCountry,
    string? DocumentNumberHash,
    string? FullNameHash,
    string? DateOfBirthHash,
    decimal? OcrConfidence,
    VerificationResultDto Result,
    string? ArtifactHash,
    string EngineName,
    string EngineVersion,
    string? RequestId,
    string? CorrelationId);

public sealed record CaptureQualityResultSubmissionRequestDto(
    IReadOnlyList<string> InputCaptureArtifactIds,
    RequiredCheckTypeDto CheckType,
    VerificationResultDto Result,
    IReadOnlyList<string> ReasonCodes,
    string? RetryInstructionCode,
    string EngineName,
    string EngineVersion,
    string? RequestId,
    string? CorrelationId);
