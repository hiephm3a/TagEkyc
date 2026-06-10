namespace TagEkyc.Domain;

public sealed record CaptureArtifact(
    Guid Id,
    Guid VerificationSessionId,
    CaptureArtifactType ArtifactType,
    CaptureSource CaptureSource,
    string? CaptureAgentId,
    string? DeviceId,
    VaultRef? VaultRef,
    HashRef? ArtifactHash,
    HashRef? MetadataHash,
    CaptureArtifactQualityState QualityState,
    string? RetryReasonCode,
    string RequestId,
    string CorrelationId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt);
