using TagEkyc.Application.Ports;
using TagEkyc.Contracts.CaptureAgent;
using TagEkyc.Contracts.Common;
using TagEkyc.Contracts.CaptureRuntime;
using TagEkyc.Contracts.TrustedAdapter;
using TagEkyc.Domain;

namespace TagEkyc.Application.VerificationSessions;

public abstract record VerifiedAppendPrincipal
{
    private VerifiedAppendPrincipal() { }
    public sealed record Client(AuthenticatedClientContext Actor, string? CaptureAgentId, string? DeviceId) : VerifiedAppendPrincipal;
    public sealed record Runtime(Guid CaptureAgentId, Guid DeviceInstallationId, Guid CredentialId,
        long Generation, Guid RolePolicyId, long RolePolicyRevision, Guid BindingId) : VerifiedAppendPrincipal;
    public string? CaptureAgentIdentity => this switch { Client c => c.CaptureAgentId, Runtime r => r.CaptureAgentId.ToString("N"), _ => throw new InvalidOperationException() };
    public string? DeviceIdentity => this switch { Client c => c.DeviceId, Runtime r => r.DeviceInstallationId.ToString("N"), _ => throw new InvalidOperationException() };
}

public interface IAuthorityNeutralVerificationEvidencePlanner
{
    Task<SessionOperationResult<AppendCaptureArtifactWrite>> PlanCaptureArtifactAsync(
        VerifiedAppendPrincipal principal, VerificationSession session, LocalDevClientPolicy policy,
        NeutralCaptureArtifactPayload payload, string idempotencyKey, DateTimeOffset now,
        CancellationToken cancellationToken = default);
    Task<SessionOperationResult<AppendEvidenceResultWrite>> PlanEvidenceResultAsync(
        VerifiedAppendPrincipal principal, VerificationSession session, LocalDevClientPolicy policy,
        IReadOnlyList<CaptureArtifact> sessionArtifacts, NeutralEvidenceResultPayload payload,
        string idempotencyKey, DateTimeOffset now, CancellationToken cancellationToken = default);
}

public interface IAppendBusinessTransaction
{
    Task<SessionOperationResult<T>> ExecuteAsync<T>(
        Func<CancellationToken, Task<SessionOperationResult<T>>> operation, CancellationToken cancellationToken);
}

public interface IAuthorityNeutralVerificationEvidenceWriter
{
    Task<SessionOperationResult<CaptureArtifactSubmissionResponseDto>> ApplyCaptureArtifactAsync(
        VerifiedAppendPrincipal principal, AppendCaptureArtifactWrite write,
        CancellationToken cancellationToken);
    Task<SessionOperationResult<EvidenceResultSubmissionResponseDto>> ApplyEvidenceResultAsync(
        VerifiedAppendPrincipal principal, AppendEvidenceResultWrite write,
        CancellationToken cancellationToken);
}

public sealed record VerifiedRuntimeAppendAuthority(Guid VerificationSessionId,
    Guid RolePolicyId, long RolePolicyRevision, long RuntimeRevision,
    long InstallationRevision, long CredentialRevision, Guid CapabilityId,
    long CapabilityRevision, Guid BindingId);

public interface ICaptureRuntimeAppendAuthority
{
    Task<SessionOperationResult<VerifiedRuntimeAppendAuthority>> ValidateCaptureAsync(
        AuthenticatedCaptureRuntimeContext actor, Guid bindingId, DateTimeOffset now,
        CancellationToken cancellationToken);
    Task<SessionOperationResult<VerifiedRuntimeAppendAuthority>> ValidateEvidenceAsync(
        AuthenticatedCaptureRuntimeContext actor, Guid bindingId, DateTimeOffset now,
        CancellationToken cancellationToken);
}

public sealed record NeutralCaptureArtifactPayload(
    CaptureArtifactTypeDto ArtifactType, CaptureSourceDto CaptureSource,
    string? ArtifactHash, string? MetadataHash, string? RequestId, string? CorrelationId);

public sealed record NeutralEvidenceResultPayload(
    EvidenceResultTypeDto ResultType, IReadOnlyList<string> InputCaptureArtifactIds,
    VerificationResultDto Result, decimal? Confidence, IReadOnlyList<string> ReasonCodes,
    string? RetryReasonCode, string? SanitizedSummaryRef, string? PayloadHash,
    SignaturePlaceholderStatusDto PayloadSignatureStatus, string EngineName,
    string EngineVersion, string? RequestId, string? CorrelationId,
    NeutralNfcEvidenceDecisionBasis? NfcEvidenceDecisionBasis = null,
    NeutralFaceMatchEvidenceDecisionBasis? FaceMatchEvidenceDecisionBasis = null,
    NeutralLivenessEvidenceDecisionBasis? LivenessEvidenceDecisionBasis = null);

public sealed record NeutralCaptureBinding(
    string? CaptureAgentId, string? DeviceId,
    string? ChallengeHash,
    string? SessionId,
    DateTimeOffset? CapturedAt,
    string? ArtifactHash);

public sealed record NeutralNfcEvidenceDecisionBasis(
    IReadOnlyList<string> Flags,
    NeutralCaptureBinding? CaptureBinding,
    VerificationResultDto? ServerDecisionResult,
    VerificationResultDto? AdapterRequestedResult,
    string? EngineName,
    string? EngineVersion,
    IReadOnlyList<NfcInputArtifactRefDto> InputArtifacts,
    string? SanitizedSummaryLabel,
    VerificationExtensionDescriptorDto? Extension = null);

public sealed record NeutralFaceMatchEvidenceDecisionBasis(
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
    NeutralCaptureBinding? LiveCaptureBinding,
    VerificationResultDto? ServerDecisionResult,
    VerificationResultDto? AdapterRequestedResult,
    string? EngineName,
    string? EngineVersion,
    string? SanitizedSummaryLabel,
    VerificationExtensionDescriptorDto? Extension = null);

public sealed record NeutralLivenessEvidenceDecisionBasis(
    string? LiveMediaArtifactId,
    string? LiveMediaArtifactHash,
    decimal? LivenessScore,
    string? AdapterRequestedVerdict,
    string? Method,
    string? LivenessGrade,
    decimal? ThresholdApplied,
    NeutralCaptureBinding? LiveCaptureBinding,
    bool? ServerDerivedIsLive,
    VerificationResultDto? ServerDecisionResult,
    VerificationResultDto? AdapterRequestedResult,
    string? SanitizedSummaryLabel,
    VerificationExtensionDescriptorDto? Extension = null);

public static class AuthorityNeutralPayloadAdapters
{
    public static NeutralCaptureArtifactPayload MapRuntimeCapturePayload(CaptureRuntimeCaptureArtifactPayload value, VerifiedAppendPrincipal.Runtime principal) =>
        new(value.ArtifactType, value.CaptureSource, value.ArtifactHash, value.MetadataHash, value.RequestId, value.CorrelationId);
    public static NeutralEvidenceResultPayload MapRuntimeEvidencePayload(CaptureRuntimeEvidenceResultPayload value, VerifiedAppendPrincipal.Runtime principal) =>
        new(value.ResultType, value.InputCaptureArtifactIds, value.Result, value.Confidence, value.ReasonCodes, value.RetryReasonCode, value.SanitizedSummaryRef, value.PayloadHash, value.PayloadSignatureStatus, value.EngineName, value.EngineVersion, value.RequestId, value.CorrelationId, Map(value.NfcEvidenceDecisionBasis, principal), Map(value.FaceMatchEvidenceDecisionBasis, principal), Map(value.LivenessEvidenceDecisionBasis, principal));
    public static NeutralCaptureArtifactPayload MapClientCapturePayload(CaptureArtifactSubmissionRequestDto value) => new(value.ArtifactType, value.CaptureSource, value.ArtifactHash, value.MetadataHash, value.RequestId, value.CorrelationId);
    public static NeutralEvidenceResultPayload MapClientEvidencePayload(EvidenceResultSubmissionRequestDto value) => new(value.ResultType, value.InputCaptureArtifactIds, value.Result, value.Confidence, value.ReasonCodes, value.RetryReasonCode, value.SanitizedSummaryRef, value.PayloadHash, value.PayloadSignatureStatus, value.EngineName, value.EngineVersion, value.RequestId, value.CorrelationId, Map(value.NfcEvidenceDecisionBasis), Map(value.FaceMatchEvidenceDecisionBasis), Map(value.LivenessEvidenceDecisionBasis));
    private static NeutralNfcEvidenceDecisionBasis? Map(CaptureRuntimeNfcEvidenceDecisionBasis? value, VerifiedAppendPrincipal.Runtime principal) => value is null ? null : new(value.Flags, Map(value.CaptureBinding, principal), value.ServerDecisionResult, value.AdapterRequestedResult, value.EngineName, value.EngineVersion, value.InputArtifacts, value.SanitizedSummaryLabel, value.Extension);
    private static NeutralNfcEvidenceDecisionBasis? Map(NfcEvidenceDecisionBasisDto? value) => value is null ? null : new(value.Flags, Map(value.CaptureBinding), value.ServerDecisionResult, value.AdapterRequestedResult, value.EngineName, value.EngineVersion, value.InputArtifacts, value.SanitizedSummaryLabel, value.Extension);
    private static NeutralFaceMatchEvidenceDecisionBasis? Map(CaptureRuntimeFaceMatchEvidenceDecisionBasis? value, VerifiedAppendPrincipal.Runtime principal) => value is null ? null : new(value.LiveFaceArtifactId, value.LiveFaceArtifactHash, value.MatchScore, value.ThresholdApplied, value.IsMatch, value.ReferenceFaceSource, value.ReferenceEvidenceResultId, value.ReferenceEvidenceType, value.ReferenceArtifactId, value.ReferenceArtifactHash, value.ReferencePayloadHash, Map(value.LiveCaptureBinding, principal), value.ServerDecisionResult, value.AdapterRequestedResult, value.EngineName, value.EngineVersion, value.SanitizedSummaryLabel, value.Extension);
    private static NeutralFaceMatchEvidenceDecisionBasis? Map(FaceMatchEvidenceDecisionBasisDto? value) => value is null ? null : new(value.LiveFaceArtifactId, value.LiveFaceArtifactHash, value.MatchScore, value.ThresholdApplied, value.IsMatch, value.ReferenceFaceSource, value.ReferenceEvidenceResultId, value.ReferenceEvidenceType, value.ReferenceArtifactId, value.ReferenceArtifactHash, value.ReferencePayloadHash, Map(value.LiveCaptureBinding), value.ServerDecisionResult, value.AdapterRequestedResult, value.EngineName, value.EngineVersion, value.SanitizedSummaryLabel, value.Extension);
    private static NeutralLivenessEvidenceDecisionBasis? Map(CaptureRuntimeLivenessEvidenceDecisionBasis? value, VerifiedAppendPrincipal.Runtime principal) => value is null ? null : new(value.LiveMediaArtifactId, value.LiveMediaArtifactHash, value.LivenessScore, value.AdapterRequestedVerdict, value.Method, value.LivenessGrade, value.ThresholdApplied, Map(value.LiveCaptureBinding, principal), value.ServerDerivedIsLive, value.ServerDecisionResult, value.AdapterRequestedResult, value.SanitizedSummaryLabel, value.Extension);
    private static NeutralLivenessEvidenceDecisionBasis? Map(LivenessEvidenceDecisionBasisDto? value) => value is null ? null : new(value.LiveMediaArtifactId, value.LiveMediaArtifactHash, value.LivenessScore, value.AdapterRequestedVerdict, value.Method, value.LivenessGrade, value.ThresholdApplied, Map(value.LiveCaptureBinding), value.ServerDerivedIsLive, value.ServerDecisionResult, value.AdapterRequestedResult, value.SanitizedSummaryLabel, value.Extension);
    private static NeutralCaptureBinding? Map(CaptureRuntimeCaptureBinding? value, VerifiedAppendPrincipal.Runtime principal) => value is null ? null : new(principal.CaptureAgentIdentity, principal.DeviceIdentity, value.ChallengeHash, value.SessionId, value.CapturedAt, value.ArtifactHash);
    private static NeutralCaptureBinding? Map(NfcCaptureBindingDto? value) => value is null ? null : new(value.CaptureAgentId, value.DeviceId, value.ChallengeHash, value.SessionId, value.CapturedAt, value.ArtifactHash);
    private static NeutralCaptureBinding? Map(FaceMatchCaptureBindingDto? value) => value is null ? null : new(value.CaptureAgentId, value.DeviceId, value.ChallengeHash, value.SessionId, value.CapturedAt, value.ArtifactHash);
    private static NeutralCaptureBinding? Map(LivenessCaptureBindingDto? value) => value is null ? null : new(value.CaptureAgentId, value.DeviceId, value.ChallengeHash, value.SessionId, value.CapturedAt, value.ArtifactHash);
}
