namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class EvidencePackageRow
{
    public Guid Id { get; set; }
    public Guid VerificationSessionId { get; set; }
    public string PackageVersion { get; set; } = string.Empty;
    public string CanonicalizationScheme { get; set; } = string.Empty;
    public string HashAlgorithm { get; set; } = string.Empty;
    public string ManifestHash { get; set; } = string.Empty;
    public string EvidenceRefsJson { get; set; } = "[]";
    public string AuditEventRefsJson { get; set; } = "[]";
    public Guid ResultRef { get; set; }
    public string PackageHash { get; set; } = string.Empty;
    public string EvidencePackageSignatureStatus { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
