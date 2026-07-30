namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportCaptureAcceptanceEventRow
{
    public Guid CaptureAcceptanceId { get; set; }
    public Guid VerificationSessionId { get; set; }
    public Guid ClientApplicationId { get; set; }
    public string RawClass { get; set; } = string.Empty;
    public Guid CaptureArtifactId { get; set; }
    public int CaptureRevision { get; set; }
    public string SessionChallengeHash { get; set; } = string.Empty;
    public string AcceptedEvidenceRef { get; set; } = string.Empty;
    public DateTimeOffset AcceptedAtUtc { get; set; }
    public string AcceptancePolicyId { get; set; } = string.Empty;
    public int AcceptancePolicyVersion { get; set; }
}
