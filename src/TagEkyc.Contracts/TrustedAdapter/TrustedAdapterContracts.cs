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
    string? CorrelationId,
    NfcEvidenceDecisionBasisDto? NfcEvidenceDecisionBasis = null,
    FaceMatchEvidenceDecisionBasisDto? FaceMatchEvidenceDecisionBasis = null,
    LivenessEvidenceDecisionBasisDto? LivenessEvidenceDecisionBasis = null);

public enum VerificationExtensionCategoryDto
{
    IdentityEvidence = 0,
    PossessionFactor = 1,
    QualityGate = 2,
}

public sealed record VerificationExtensionDescriptorDto(
    string Id,
    string RequiredCheckType,
    VerificationExtensionCategoryDto Category,
    string EmitsEvidenceType);

public sealed record NfcCaptureBindingDto(
    string? ChallengeHash,
    string? SessionId,
    string? CaptureAgentId,
    string? DeviceId,
    DateTimeOffset? CapturedAt,
    string? ArtifactHash);

public sealed record NfcInputArtifactRefDto(
    string ArtifactId,
    string? ArtifactHash);

public sealed record NfcEvidenceDecisionBasisDto(
    IReadOnlyList<string> Flags,
    NfcCaptureBindingDto? CaptureBinding,
    VerificationResultDto? ServerDecisionResult,
    VerificationResultDto? AdapterRequestedResult,
    string? EngineName,
    string? EngineVersion,
    IReadOnlyList<NfcInputArtifactRefDto> InputArtifacts,
    string? SanitizedSummaryLabel,
    VerificationExtensionDescriptorDto? Extension = null);

public enum FaceMatchReferenceFaceSourceDto
{
    ChipDg2FromTrustedNfc = 0,
    DocumentImage = 1,
    SelfieImage = 2,
}

public sealed record FaceMatchCaptureBindingDto(
    string? ChallengeHash,
    string? SessionId,
    string? CaptureAgentId,
    string? DeviceId,
    DateTimeOffset? CapturedAt,
    string? ArtifactHash);

public sealed record FaceMatchEvidenceDecisionBasisDto(
    string? LiveFaceArtifactId,
    string? LiveFaceArtifactHash,
    decimal? MatchScore,
    decimal? ThresholdApplied,
    bool? IsMatch,
    FaceMatchReferenceFaceSourceDto? ReferenceFaceSource,
    string? ReferenceEvidenceResultId,
    string? ReferenceEvidenceType,
    string? ReferenceArtifactId,
    string? ReferenceArtifactHash,
    string? ReferencePayloadHash,
    FaceMatchCaptureBindingDto? LiveCaptureBinding,
    VerificationResultDto? ServerDecisionResult,
    VerificationResultDto? AdapterRequestedResult,
    string? EngineName,
    string? EngineVersion,
    string? SanitizedSummaryLabel,
    VerificationExtensionDescriptorDto? Extension = null);

public sealed record LivenessCaptureBindingDto(
    string? ChallengeHash,
    string? SessionId,
    string? CaptureAgentId,
    string? DeviceId,
    DateTimeOffset? CapturedAt,
    string? ArtifactHash);

public sealed record LivenessEvidenceDecisionBasisDto(
    string? LiveMediaArtifactId,
    string? LiveMediaArtifactHash,
    decimal? LivenessScore,
    string? AdapterRequestedVerdict,
    string? Method,
    string? LivenessGrade,
    decimal? ThresholdApplied,
    LivenessCaptureBindingDto? LiveCaptureBinding,
    bool? ServerDerivedIsLive,
    VerificationResultDto? ServerDecisionResult,
    VerificationResultDto? AdapterRequestedResult,
    string? SanitizedSummaryLabel,
    VerificationExtensionDescriptorDto? Extension = null);

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
