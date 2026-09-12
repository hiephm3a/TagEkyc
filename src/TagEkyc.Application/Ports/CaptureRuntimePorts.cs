using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.CaptureAgent;
using TagEkyc.Contracts.CaptureRuntime;
using TagEkyc.Contracts.TrustedAdapter;

namespace TagEkyc.Application.Ports;

public enum CaptureRuntimeVerifierPepperDomain
{
    PlatformCredential,
    BootstrapDigest,
    CapabilityDigest
}

public interface ICaptureRuntimeVerifierPepperSource
{
    int CurrentVersion { get; }

    ValueTask<ICaptureRuntimeVerifierPepperLease?> TryResolveAsync(
        int version,
        CaptureRuntimeVerifierPepperDomain domain,
        CancellationToken cancellationToken = default);
}

public interface ICaptureRuntimeVerifierPepperLease : IDisposable
{
    int Version { get; }
    CaptureRuntimeVerifierPepperDomain Domain { get; }
    ReadOnlyMemory<byte> Key { get; }
}

public interface IPlatformOperatorCredentialAuthenticator
{
    Task<SessionOperationResult<AuthenticatedPlatformOperatorContext>> AuthenticateAsync(
        string? presentedKey, CancellationToken cancellationToken = default);
}

public interface ICaptureRuntimeRequestAuthenticator
{
    Task<SessionOperationResult<AuthenticatedCaptureRuntimeContext>> AuthenticateAsync(
        CaptureRuntimeSignedRequest request, CancellationToken cancellationToken = default);
}

public interface ICaptureRuntimeRotationCompletionAuthenticator
{
    Task<SessionOperationResult<CaptureRuntimeRotationAuthentication>> AuthenticateRotationCompletionAsync(
        CaptureRuntimeSignedRequest request, Guid rotationId, CaptureRuntimeRotationCompleteRequest body,
        CaptureRuntimeRotationFingerprintCandidates candidates, CancellationToken cancellationToken = default);
}

public enum CaptureRuntimeRotationBranch { Predecessor, Successor }

public sealed record CaptureRuntimeRotationFingerprintCandidates(byte[]? AsPredecessor, byte[]? AsSuccessor);

public sealed record CaptureRuntimeRotationAuthentication(
    AuthenticatedCaptureRuntimeContext Actor, CaptureRuntimeRotationBranch Branch, byte[] SelectedRequestFingerprint);

public interface ICaptureRuntimeRotationService
{
    Task<SessionOperationResult<CaptureRuntimeRotationCompletionResponse>> CompleteRotationAsync(
        CaptureRuntimeSignedRequest signedRequest, Guid rotationId, CaptureRuntimeRotationCompleteRequest request,
        Guid idempotencyKey, ReadOnlyMemory<byte> exactRequestBody, CancellationToken cancellationToken = default);
}

public interface ICaptureRuntimeRotationGateway
{
    Task<SessionOperationResult<CaptureRuntimeRotationCompletionResponse>> CompleteRotationAsync(
        CaptureRuntimeRotationCommand command, CancellationToken cancellationToken);
    Task<SessionOperationResult<CaptureRuntimeRotationCompletionResponse>> ReplayCompletedRotationAsync(
        CaptureRuntimeRotationCommand command, CancellationToken cancellationToken);
}

public sealed record CaptureRuntimeRotationCommand(
    Guid RotationId, Guid CredentialId, long PresentedGeneration, Guid CandidateKeyId,
    Guid IdempotencyKey, byte[] PublicVerifierSpki, byte[] PublicKeyThumbprint,
    byte[] SuccessorProof, byte[] RequestFingerprint, DateTimeOffset NowUtc);

public interface ICaptureRuntimeRawIngressAdmission
{
    ValueTask<CaptureRuntimeRawIngressAdmissionResult> AdmitAsync(
        CaptureRuntimeRawIngressAdmissionContext context, Stream body,
        CancellationToken cancellationToken);
}

public sealed record CaptureRuntimeRawIngressAdmissionContext(
    Guid CaptureAgentId, Guid DeviceInstallationId, Guid CredentialId,
    long CredentialGeneration, Guid RolePolicyId, long RolePolicyRevision,
    DateTimeOffset SignedAtUtc, byte[] Nonce, byte[] SignedEnvelopeFingerprint,
    long AgentConfigurationRevision, Guid VerificationSessionId,
    Guid CaptureArtifactId, int CaptureRevision, string RawClass,
    Guid IngressIdempotencyKey, string MediaType, long ClaimedPlaintextLength,
    string ClaimedPlaintextDigest, DateTimeOffset CapturedAtUtc,
    DateTimeOffset PlaintextRetentionStartedAtUtc,
    DateTimeOffset PlaintextRetentionExpiresAtUtc, long PlaintextRetentionBudgetSeconds);

public enum CaptureRuntimeRawIngressOutcome
{
    Available, AlreadyAvailable, EvaluationInProgress, BindingInvalid,
    CapacityUnavailable, TransportProtocolInvalid
}

public sealed record CaptureRuntimeRawIngressAdmissionResult(
    CaptureRuntimeRawIngressOutcome Outcome, Guid? SourceArtifactId,
    string? CurrentSourceState, string? CurrentDisposition,
    DateTimeOffset? RetryNotBeforeUtc);

public interface ICaptureRuntimeEnrollmentGateway
{
    Task<SessionOperationResult<int?>> ResolveBootstrapVerifierVersionAsync(
        Guid bootstrapIssuanceId, CancellationToken cancellationToken);
    Task<SessionOperationResult<CaptureRuntimeEnrollmentResponse>> RedeemBootstrapAsync(
        CaptureRuntimeEnrollmentCommand command,
        CancellationToken cancellationToken);
}

public sealed record CaptureRuntimeEnrollmentCommand(
    Guid BootstrapIssuanceId, Guid RedeemOperationId, byte[] RequestFingerprint,
    Guid CandidateKeyId, byte[] PublicVerifierSpki, byte[] PublicKeyThumbprint,
    DateTimeOffset SignedAtUtc, byte[] Nonce, byte[] CandidateProof,
    byte[] BootstrapDigest, DateTimeOffset NowUtc);

public interface ICaptureRuntimeManagementGateway
{
    Task<SessionOperationResult<CaptureRuntimeBootstrapPersistenceResult>> IssueBootstrapAsync(
        AuthenticatedPlatformOperatorContext actor, CaptureRuntimeBootstrapIssueRequest request,
        Guid idempotencyKey, byte[] requestFingerprint,
        string keyLookupPrefix, byte[] secretDigest, int verifierPepperVersion,
        DateTimeOffset now,
        CancellationToken cancellationToken);
    Task<SessionOperationResult<CaptureRuntimeBootstrapLifecycleResponse>> RevokeBootstrapAsync(
        AuthenticatedPlatformOperatorContext actor, CaptureRuntimeBootstrapRevokeRequest request,
        Guid idempotencyKey, byte[] requestFingerprint,
        DateTimeOffset now,
        CancellationToken cancellationToken);
    Task<SessionOperationResult<CaptureRuntimeLifecycleResponse>> SuspendRuntimeAsync(
        AuthenticatedPlatformOperatorContext actor, RuntimeLifecycleRequest request,
        Guid idempotencyKey, byte[] requestFingerprint,
        DateTimeOffset now,
        CancellationToken cancellationToken);
    Task<SessionOperationResult<CaptureRuntimeLifecycleResponse>> ReactivateRuntimeAsync(
        AuthenticatedPlatformOperatorContext actor, RuntimeLifecycleRequest request,
        Guid idempotencyKey, byte[] requestFingerprint,
        DateTimeOffset now,
        CancellationToken cancellationToken);
    Task<SessionOperationResult<CaptureRuntimeLifecycleResponse>> RevokeRuntimeAsync(
        AuthenticatedPlatformOperatorContext actor, RuntimeLifecycleRequest request,
        Guid idempotencyKey, byte[] requestFingerprint,
        DateTimeOffset now,
        CancellationToken cancellationToken);
    Task<SessionOperationResult<CaptureRuntimeLifecycleResponse>> RetireRuntimeAsync(
        AuthenticatedPlatformOperatorContext actor, RuntimeLifecycleRequest request,
        Guid idempotencyKey, byte[] requestFingerprint,
        DateTimeOffset now,
        CancellationToken cancellationToken);
    Task<SessionOperationResult<CaptureRuntimeCredentialResponse>> RevokeCredentialAsync(
        AuthenticatedPlatformOperatorContext actor, CaptureRuntimeCredentialRevokeRequest request,
        Guid idempotencyKey, byte[] requestFingerprint,
        DateTimeOffset now,
        CancellationToken cancellationToken);
    Task<SessionOperationResult<CaptureRuntimeRotationResponse>> AuthorizeRotationAsync(
        AuthenticatedPlatformOperatorContext actor, CaptureRuntimeRotationAuthorizeRequest request,
        Guid idempotencyKey, byte[] requestFingerprint,
        DateTimeOffset now,
        CancellationToken cancellationToken);
    Task<SessionOperationResult<CaptureRuntimeRotationLifecycleResponse>> RevokeRotationAsync(
        AuthenticatedPlatformOperatorContext actor, CaptureRuntimeRotationRevokeRequest request,
        Guid idempotencyKey, byte[] requestFingerprint,
        DateTimeOffset now,
        CancellationToken cancellationToken);
    Task<SessionOperationResult<CaptureRuntimeRolePolicyAssignmentResponse>> AssignRolePolicyAsync(
        AuthenticatedPlatformOperatorContext actor, CaptureRuntimeRolePolicyAssignmentRequest request,
        Guid idempotencyKey, byte[] requestFingerprint,
        DateTimeOffset now,
        CancellationToken cancellationToken);
    Task<SessionOperationResult<CaptureRuntimeConfigurationAssignmentResponse>> AssignConfigurationAsync(
        AuthenticatedPlatformOperatorContext actor, CaptureRuntimeConfigurationAssignmentRequest request,
        Guid idempotencyKey, byte[] requestFingerprint,
        DateTimeOffset now,
        CancellationToken cancellationToken);
    Task<SessionOperationResult<CaptureRuntimeReadinessResponse>> ReadReadinessAsync(
        AuthenticatedPlatformOperatorContext actor, Guid captureAgentId,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

public interface ICaptureRuntimeControlGateway
{
    Task<SessionOperationResult<CaptureRuntimeCatalogPublicationResponse>> PublishTrustProfileAsync(
        AuthenticatedPlatformOperatorContext actor, CaptureRuntimeTrustProfilePublicationRequest request,
        Guid idempotencyKey, byte[] requestFingerprint,
        DateTimeOffset now,
        CancellationToken cancellationToken);
    Task<SessionOperationResult<CaptureRuntimeCatalogPublicationResponse>> PublishRolePolicyAsync(
        AuthenticatedPlatformOperatorContext actor, CaptureRuntimeRolePolicyPublicationRequest request,
        Guid idempotencyKey, byte[] requestFingerprint,
        DateTimeOffset now,
        CancellationToken cancellationToken);
    Task<SessionOperationResult<CaptureRuntimeCatalogPublicationResponse>> PublishConfigurationAsync(
        AuthenticatedPlatformOperatorContext actor, CaptureRuntimeConfigurationPublicationRequest request,
        Guid idempotencyKey, byte[] requestFingerprint,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

public sealed record CaptureCapabilityPersistenceRequest(
    Guid ClientApplicationId, Guid VerificationSessionId, CaptureCapabilityRequest Request,
    Guid IdempotencyKey, Guid NewCapabilityId, string LookupPrefix, byte[] Digest,
    int PepperVersion, byte[] RequestFingerprint, DateTimeOffset Now);
public sealed record CaptureCapabilityPersistenceResult(
    string ResultCode, Guid? CapabilityId, bool SecretAvailable, DateTimeOffset? ExpiresAtUtc,
    string? State, long? Revision);
public sealed record CaptureCapabilityVerifier(byte[] Digest, int PepperVersion);
public sealed record CaptureRuntimeBindPersistenceRequest(
    AuthenticatedCaptureRuntimeContext Actor, Guid CapabilityId, bool SecretVerified,
    Guid BindOperationId, byte[] RequestFingerprint, DateTimeOffset Now);

public interface ICaptureRuntimeExecutionGateway
{
    Task<CaptureCapabilityPersistenceResult> IssueOrReplaceCapabilityAsync(
        CaptureCapabilityPersistenceRequest request, CancellationToken cancellationToken);
    Task<SessionOperationResult<CaptureCapabilityVerifier>> ResolveCapabilityVerifierAsync(
        Guid capabilityId, CancellationToken cancellationToken);
    Task<SessionOperationResult<CaptureRuntimeBindingResponse>> BindCapabilityAsync(
        CaptureRuntimeBindPersistenceRequest request, CancellationToken cancellationToken);
    Task<SessionOperationResult<CaptureRuntimeBindingResponse>> ReconcileBindingAsync(
        AuthenticatedCaptureRuntimeContext actor, CaptureRuntimeReconcileRequest request,
        CancellationToken cancellationToken);
    Task<SessionOperationResult<CaptureRuntimeConfigurationResponse>> ResolveConfigurationAsync(
        AuthenticatedCaptureRuntimeContext actor, CancellationToken cancellationToken);
}

public interface ICaptureRuntimeAppendGateway
{
    Task<SessionOperationResult<CaptureArtifactSubmissionResponseDto>> AppendCaptureArtifactAsync(
        AuthenticatedCaptureRuntimeContext actor, Guid bindingId,
        CaptureRuntimeCaptureArtifactRequest request, Guid idempotencyKey,
        CancellationToken cancellationToken);
    Task<SessionOperationResult<EvidenceResultSubmissionResponseDto>> AppendEvidenceResultAsync(
        AuthenticatedCaptureRuntimeContext actor, Guid verificationSessionId,
        CaptureRuntimeEvidenceResultRequest request, Guid idempotencyKey,
        CancellationToken cancellationToken);
}

public interface ICaptureRuntimeEnrollmentService
{
    Task<SessionOperationResult<CaptureRuntimeEnrollmentResponse>> RedeemAsync(
        CaptureRuntimeEnrollmentRedeemRequest request, Guid idempotencyKey,
        ReadOnlyMemory<byte> exactRequestBody,
        CancellationToken cancellationToken = default);
}

public interface ICaptureRuntimeManagementService
{
    Task<SessionOperationResult<CaptureRuntimeBootstrapIssueResponse>> IssueBootstrapAsync(
        AuthenticatedPlatformOperatorContext actor, CaptureRuntimeBootstrapIssueRequest request,
        Guid idempotencyKey, ReadOnlyMemory<byte> exactRequestBody,
        CancellationToken cancellationToken = default);
    Task<SessionOperationResult<CaptureRuntimeBootstrapLifecycleResponse>> RevokeBootstrapAsync(
        AuthenticatedPlatformOperatorContext actor, CaptureRuntimeBootstrapRevokeRequest request,
        Guid idempotencyKey, ReadOnlyMemory<byte> exactRequestBody,
        CancellationToken cancellationToken = default);
    Task<SessionOperationResult<CaptureRuntimeLifecycleResponse>> SuspendRuntimeAsync(
        AuthenticatedPlatformOperatorContext actor, RuntimeLifecycleRequest request,
        Guid idempotencyKey, ReadOnlyMemory<byte> exactRequestBody,
        CancellationToken cancellationToken = default);
    Task<SessionOperationResult<CaptureRuntimeLifecycleResponse>> ReactivateRuntimeAsync(
        AuthenticatedPlatformOperatorContext actor, RuntimeLifecycleRequest request,
        Guid idempotencyKey, ReadOnlyMemory<byte> exactRequestBody,
        CancellationToken cancellationToken = default);
    Task<SessionOperationResult<CaptureRuntimeLifecycleResponse>> RevokeRuntimeAsync(
        AuthenticatedPlatformOperatorContext actor, RuntimeLifecycleRequest request,
        Guid idempotencyKey, ReadOnlyMemory<byte> exactRequestBody,
        CancellationToken cancellationToken = default);
    Task<SessionOperationResult<CaptureRuntimeLifecycleResponse>> RetireRuntimeAsync(
        AuthenticatedPlatformOperatorContext actor, RuntimeLifecycleRequest request,
        Guid idempotencyKey, ReadOnlyMemory<byte> exactRequestBody,
        CancellationToken cancellationToken = default);
    Task<SessionOperationResult<CaptureRuntimeCredentialResponse>> RevokeCredentialAsync(
        AuthenticatedPlatformOperatorContext actor, CaptureRuntimeCredentialRevokeRequest request,
        Guid idempotencyKey, ReadOnlyMemory<byte> exactRequestBody,
        CancellationToken cancellationToken = default);
    Task<SessionOperationResult<CaptureRuntimeRotationResponse>> AuthorizeRotationAsync(
        AuthenticatedPlatformOperatorContext actor, CaptureRuntimeRotationAuthorizeRequest request,
        Guid idempotencyKey, ReadOnlyMemory<byte> exactRequestBody,
        CancellationToken cancellationToken = default);
    Task<SessionOperationResult<CaptureRuntimeRotationLifecycleResponse>> RevokeRotationAsync(
        AuthenticatedPlatformOperatorContext actor, CaptureRuntimeRotationRevokeRequest request,
        Guid idempotencyKey, ReadOnlyMemory<byte> exactRequestBody,
        CancellationToken cancellationToken = default);
    Task<SessionOperationResult<CaptureRuntimeRolePolicyAssignmentResponse>> AssignRolePolicyAsync(
        AuthenticatedPlatformOperatorContext actor, CaptureRuntimeRolePolicyAssignmentRequest request,
        Guid idempotencyKey, ReadOnlyMemory<byte> exactRequestBody,
        CancellationToken cancellationToken = default);
    Task<SessionOperationResult<CaptureRuntimeConfigurationAssignmentResponse>> AssignConfigurationAsync(
        AuthenticatedPlatformOperatorContext actor, CaptureRuntimeConfigurationAssignmentRequest request,
        Guid idempotencyKey, ReadOnlyMemory<byte> exactRequestBody,
        CancellationToken cancellationToken = default);
    Task<SessionOperationResult<CaptureRuntimeReadinessResponse>> ReadReadinessAsync(
        AuthenticatedPlatformOperatorContext actor, Guid captureAgentId,
        CancellationToken cancellationToken = default);
}

public interface ICaptureRuntimeControlService
{
    Task<SessionOperationResult<CaptureRuntimeCatalogPublicationResponse>> PublishTrustProfileAsync(
        AuthenticatedPlatformOperatorContext actor, CaptureRuntimeTrustProfilePublicationRequest request,
        Guid idempotencyKey, ReadOnlyMemory<byte> exactRequestBody,
        CancellationToken cancellationToken = default);
    Task<SessionOperationResult<CaptureRuntimeCatalogPublicationResponse>> PublishRolePolicyAsync(
        AuthenticatedPlatformOperatorContext actor, CaptureRuntimeRolePolicyPublicationRequest request,
        Guid idempotencyKey, ReadOnlyMemory<byte> exactRequestBody,
        CancellationToken cancellationToken = default);
    Task<SessionOperationResult<CaptureRuntimeCatalogPublicationResponse>> PublishConfigurationAsync(
        AuthenticatedPlatformOperatorContext actor, CaptureRuntimeConfigurationPublicationRequest request,
        Guid idempotencyKey, ReadOnlyMemory<byte> exactRequestBody,
        CancellationToken cancellationToken = default);
}

public interface ICaptureRuntimeExecutionService
{
    Task<SessionOperationResult<CaptureCapabilityResponse>> IssueOrReplaceCapabilityAsync(
        AuthenticatedClientContext actor, Guid verificationSessionId,
        CaptureCapabilityRequest request, Guid idempotencyKey,
        ReadOnlyMemory<byte> exactRequestBody,
        CancellationToken cancellationToken = default);
    Task<SessionOperationResult<CaptureRuntimeBindingResponse>> BindAsync(
        AuthenticatedCaptureRuntimeContext actor, CaptureRuntimeBindRequest request,
        Guid idempotencyKey, ReadOnlyMemory<byte> exactRequestBody,
        CancellationToken cancellationToken = default);
    Task<SessionOperationResult<CaptureRuntimeBindingResponse>> ReconcileAsync(
        AuthenticatedCaptureRuntimeContext actor, CaptureRuntimeReconcileRequest request,
        CancellationToken cancellationToken = default);
    Task<SessionOperationResult<CaptureRuntimeConfigurationResponse>> ResolveConfigurationAsync(
        AuthenticatedCaptureRuntimeContext actor, CancellationToken cancellationToken = default);
    Task<SessionOperationResult<CaptureArtifactSubmissionResponseDto>> AppendCaptureArtifactAsync(
        AuthenticatedCaptureRuntimeContext actor, Guid bindingId,
        CaptureRuntimeCaptureArtifactRequest request,
        Guid idempotencyKey,
        CancellationToken cancellationToken = default);
    Task<SessionOperationResult<EvidenceResultSubmissionResponseDto>> AppendEvidenceResultAsync(
        AuthenticatedCaptureRuntimeContext actor, Guid verificationSessionId,
        CaptureRuntimeEvidenceResultRequest request,
        Guid idempotencyKey,
        CancellationToken cancellationToken = default);
}


public sealed record CaptureRuntimeBootstrapPersistenceResult(
    Guid BootstrapIssuanceId, bool SecretAvailable, DateTimeOffset ExpiresAtUtc, long Revision);
