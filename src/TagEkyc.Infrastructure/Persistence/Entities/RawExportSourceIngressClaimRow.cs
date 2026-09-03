namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportSourceIngressClaimRow
{
    public Guid IngressClaimId { get; set; }
    public Guid VerificationSessionId { get; set; }
    public Guid CaptureAcceptanceId { get; set; }
    public Guid CaptureArtifactId { get; set; }
    public Guid ClientApplicationId { get; set; }
    public Guid AuthenticatedPrincipalId { get; set; }
    public string ProducerId { get; set; } = string.Empty;
    public string CaptureAgentInstanceId { get; set; } = string.Empty;
    public int CaptureRevision { get; set; }
    public string RawClass { get; set; } = string.Empty;
    public string SessionChallengeHash { get; set; } = string.Empty;
    public string AuthoritySnapshotId { get; set; } = string.Empty;
    public byte[] IngressIdentityFingerprint { get; set; } = [];
    public string ClaimState { get; set; } = string.Empty;
    public string CommitmentKeySelectorId { get; set; } = string.Empty;
    public int CommitmentKeySelectorVersion { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
