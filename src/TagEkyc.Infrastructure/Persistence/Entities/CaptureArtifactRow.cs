namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class CaptureArtifactRow
{
    public Guid Id { get; set; }
    public Guid VerificationSessionId { get; set; }
    public string ArtifactType { get; set; } = string.Empty;
    public string CaptureSource { get; set; } = string.Empty;
    public string? CaptureAgentId { get; set; }
    public string? DeviceId { get; set; }
    public string? VaultRef { get; set; }
    public string? ArtifactHash { get; set; }
    public string? MetadataHash { get; set; }
    public string QualityState { get; set; } = string.Empty;
    public string? RetryReasonCode { get; set; }
    public string RequestId { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
}
