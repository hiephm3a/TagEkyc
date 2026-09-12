namespace TagEkyc.Contracts.CaptureRuntime;

public sealed record CaptureRuntimeBootstrapIssueRequest(
    string RuntimeType,
    Guid TrustProfileId,
    long TrustProfileRevision,
    Guid RolePolicyId,
    long RolePolicyRevision,
    Guid ConfigurationId,
    long ConfigurationRevision,
    DateTimeOffset ExpiresAtUtc,
    string HandoffAttestationDigest);

public sealed record CaptureRuntimeBootstrapIssueResponse(
    Guid BootstrapIssuanceId, string? BootstrapSecret, DateTimeOffset ExpiresAtUtc, long Revision);

public sealed record CaptureRuntimeBootstrapRevokeRequest(
    Guid BootstrapIssuanceId, long ExpectedRevision, string Reason);

public sealed record CaptureRuntimeBootstrapLifecycleResponse(
    Guid BootstrapIssuanceId, string State, long Revision, DateTimeOffset? RevokedAtUtc);

public sealed record CaptureRuntimeRotationLifecycleResponse(
    Guid RotationId, string State, long Revision, DateTimeOffset? RevokedAtUtc);

public sealed record RuntimeLifecycleRequest(Guid CaptureAgentId, long ExpectedRevision, string Reason);

public sealed record CaptureRuntimeLifecycleResponse(
    Guid CaptureAgentId, string State, long Revision, DateTimeOffset TransitionedAtUtc);

public sealed record CaptureRuntimeCredentialRevokeRequest(
    Guid CaptureAgentId, Guid InstallationId, Guid CredentialId, long Generation,
    long ExpectedCredentialRevision, string Reason);

public sealed record CaptureRuntimeCredentialResponse(
    Guid CredentialId, long Generation, string State, long Revision, DateTimeOffset? RevokedAtUtc);

public sealed record CaptureRuntimeRotationAuthorizeRequest(
    Guid CaptureAgentId, Guid InstallationId, Guid CredentialId, long CurrentGeneration,
    long ExpectedCredentialRevision, DateTimeOffset ExpiresAtUtc);

public sealed record CaptureRuntimeRotationResponse(
    Guid RotationId, Guid CaptureAgentId, Guid InstallationId, Guid CredentialId,
    long CurrentGeneration, string State, long Revision, DateTimeOffset ExpiresAtUtc);

public sealed record CaptureRuntimeRotationRevokeRequest(Guid RotationId, long ExpectedRevision, string Reason);

public sealed record CaptureRuntimeRotationCompleteRequest(
    Guid CandidateKeyId, string SuccessorPublicVerifierSpki,
    string SuccessorPublicKeyThumbprint, string SuccessorProof);

public sealed record CaptureRuntimeRotationCompletionResponse(
    Guid CredentialId, long Generation, Guid CandidateKeyId, string PublicKeyThumbprint,
    long CredentialRevision, long InstallationRevision, long RotationRevision);

public sealed record CaptureRuntimeRolePolicyAssignmentRequest(
    Guid AgentId, Guid RolePolicyId, long RolePolicyRevision, long ExpectedRuntimeRevision);

public sealed record CaptureRuntimeRolePolicyAssignmentResponse(
    Guid CaptureAgentId, Guid RolePolicyId, long RolePolicyRevision, long RuntimeRevision);

public sealed record CaptureRuntimeConfigurationAssignmentRequest(
    Guid AgentId, Guid ConfigurationId, long ConfigurationRevision,
    long ExpectedRuntimeRevision, Guid? OverrideId);

public sealed record CaptureRuntimeConfigurationAssignmentResponse(
    Guid CaptureAgentId, Guid ConfigurationId, long ConfigurationRevision,
    Guid? OverrideId, long RuntimeRevision);

public sealed record CaptureRuntimeReadinessResponse(
    Guid CaptureAgentId, string RuntimeState, long RuntimeRevision,
    string InstallationState, long InstallationRevision, string CredentialState,
    long CredentialGeneration, long CredentialRevision, long TrustProfileRevision,
    long RolePolicyRevision, long ConfigurationRevision,
    IReadOnlyList<int> RequiredPepperVersions, bool NonceStoreReady,
    bool CutoverSentinelReady, bool DatabaseReady, bool PlatformPepperReady, bool IsReady);
