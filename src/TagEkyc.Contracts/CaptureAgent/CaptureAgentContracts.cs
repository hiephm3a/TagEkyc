namespace TagEkyc.Contracts.CaptureAgent;

public enum CaptureArtifactTypeDto
{
    DocumentFrontImage = 0,
    DocumentBackImage = 1,
    SelfieImage = 2,
    LivenessMedia = 3,
    NfcReadArtifact = 4,
    FingerprintCapture = 5,
    DeviceCaptureMetadata = 6,
}

public enum CaptureSourceDto
{
    MobileSdk = 0,
    PcAgent = 1,
    KioskAgent = 2,
    DeviceGateway = 3,
    InternalAdapter = 4,
    ExternalPreStaged = 5,
}

public sealed record CaptureArtifactSubmissionRequestDto(
    CaptureArtifactTypeDto ArtifactType,
    CaptureSourceDto CaptureSource,
    string? CaptureAgentId,
    string? DeviceId,
    string? ArtifactHash,
    string? MetadataHash,
    string? RequestId,
    string? CorrelationId);

public sealed record CaptureArtifactSubmissionResponseDto(
    string CaptureArtifactId,
    string VerificationSessionId,
    string? ArtifactHash,
    bool Accepted,
    string SessionState,
    string CorrelationId);
