using TagEkyc.Contracts.CaptureAgent;
using TagEkyc.Contracts.Common;
using TagEkyc.Contracts.TrustedAdapter;

namespace TagEkyc.Contracts.CaptureRuntime;

public sealed record CaptureCapabilityRequest(string Action, Guid? CurrentCapabilityId = null, long? ExpectedRevision = null);
public sealed record CaptureCapabilityResponse(Guid CaptureCapabilityId, string? Secret, DateTimeOffset ExpiresAtUtc, string State, long Revision);
public sealed record CaptureRuntimeBindRequest(Guid CaptureCapabilityId, string CaptureCapabilitySecret, Guid BindOperationId);
public sealed record CaptureRuntimeBindingResponse(Guid BindingId, DateTimeOffset ExecutionExpiresAtUtc, long RuntimeRevision, long InstallationRevision, long CredentialRevision, long CapabilityRevision);
public sealed record CaptureRuntimeReconcileRequest(Guid CaptureCapabilityId, Guid BindOperationId);

public sealed record CaptureRuntimeConfigurationResponse(
    Guid CaptureAgentId, Guid ConfigurationId, long ConfigurationRevision,
    DateTimeOffset EffectiveAtUtc, DateTimeOffset ExpiresAtUtc, bool RawExportEnabled,
    int PlaintextBudgetSeconds, int RawExportSourceClaimSafetyMarginMilliseconds,
    int CaptureAgentConfigurationPollingIntervalSeconds,
    int RawExportSourceMaximumChipDg2PortraitBytes,
    int RawExportSourceMaximumLiveSelfieImageBytes,
    long RawExportCaptureMaximumAggregatePlaintextBytesPerHost,
    int RawExportCustodyMaximumPlaintextWindowBytesPerStream,
    long RawExportCustodyMaximumAggregatePlaintextWindowBytesPerDeployment,
    int RawExportIngressMaximumPreAdmissionBufferedBytes);

public sealed record CaptureRuntimeCaptureArtifactRequest(Guid BindingId, CaptureRuntimeCaptureArtifactPayload Payload);
public sealed record CaptureRuntimeCaptureArtifactPayload(
    CaptureArtifactTypeDto ArtifactType, CaptureSourceDto CaptureSource,
    string? ArtifactHash, string? MetadataHash, string? RequestId, string? CorrelationId);

public sealed record CaptureRuntimeEvidenceResultRequest(Guid BindingId, CaptureRuntimeEvidenceResultPayload Payload);
public sealed record CaptureRuntimeEvidenceResultPayload(
    EvidenceResultTypeDto ResultType, IReadOnlyList<string> InputCaptureArtifactIds,
    VerificationResultDto Result, decimal? Confidence, IReadOnlyList<string> ReasonCodes,
    string? RetryReasonCode, string? SanitizedSummaryRef, string? PayloadHash,
    SignaturePlaceholderStatusDto PayloadSignatureStatus, string EngineName,
    string EngineVersion, string? RequestId, string? CorrelationId,
    CaptureRuntimeNfcEvidenceDecisionBasis? NfcEvidenceDecisionBasis = null,
    CaptureRuntimeFaceMatchEvidenceDecisionBasis? FaceMatchEvidenceDecisionBasis = null,
    CaptureRuntimeLivenessEvidenceDecisionBasis? LivenessEvidenceDecisionBasis = null);

public sealed record CaptureRuntimeCaptureBinding(
    string? ChallengeHash,
    string? SessionId,
    DateTimeOffset? CapturedAt,
    string? ArtifactHash);

public sealed record CaptureRuntimeNfcEvidenceDecisionBasis(
    IReadOnlyList<string> Flags,
    CaptureRuntimeCaptureBinding? CaptureBinding,
    VerificationResultDto? ServerDecisionResult,
    VerificationResultDto? AdapterRequestedResult,
    string? EngineName,
    string? EngineVersion,
    IReadOnlyList<NfcInputArtifactRefDto> InputArtifacts,
    string? SanitizedSummaryLabel,
    VerificationExtensionDescriptorDto? Extension = null);

public sealed record CaptureRuntimeFaceMatchEvidenceDecisionBasis(
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
    CaptureRuntimeCaptureBinding? LiveCaptureBinding,
    VerificationResultDto? ServerDecisionResult,
    VerificationResultDto? AdapterRequestedResult,
    string? EngineName,
    string? EngineVersion,
    string? SanitizedSummaryLabel,
    VerificationExtensionDescriptorDto? Extension = null);

public sealed record CaptureRuntimeLivenessEvidenceDecisionBasis(
    string? LiveMediaArtifactId,
    string? LiveMediaArtifactHash,
    decimal? LivenessScore,
    string? AdapterRequestedVerdict,
    string? Method,
    string? LivenessGrade,
    decimal? ThresholdApplied,
    CaptureRuntimeCaptureBinding? LiveCaptureBinding,
    bool? ServerDerivedIsLive,
    VerificationResultDto? ServerDecisionResult,
    VerificationResultDto? AdapterRequestedResult,
    string? SanitizedSummaryLabel,
    VerificationExtensionDescriptorDto? Extension = null);
