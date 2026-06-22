namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class EvidenceManifestRow
{
    public Guid EvidencePackageId { get; set; }
    public Guid SessionGuid { get; set; }
    public string VerificationSessionId { get; set; } = string.Empty;
    public string PackageVersion { get; set; } = string.Empty;
    public string CanonicalizationScheme { get; set; } = string.Empty;
    public string HashAlgorithm { get; set; } = string.Empty;
    public string ManifestHash { get; set; } = string.Empty;
    public string PackageHash { get; set; } = string.Empty;
    public string EvidenceRefsJson { get; set; } = "[]";
    public string AuditEventRefsJson { get; set; } = "[]";
    public Guid ResultRef { get; set; }
    public string EvidencePackageSignatureStatus { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
