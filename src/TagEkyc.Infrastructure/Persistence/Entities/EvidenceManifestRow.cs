namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class EvidenceManifestRow
{
    public string EvidencePackageId { get; set; } = string.Empty;
    public Guid SessionGuid { get; set; }
    public string VerificationSessionId { get; set; } = string.Empty;
    public string PackageVersion { get; set; } = string.Empty;
    public string ManifestHash { get; set; } = string.Empty;
    public string PackageHash { get; set; } = string.Empty;
    public string EvidenceRefsJson { get; set; } = "[]";
    public string AuditEventRefsJson { get; set; } = "[]";
    public string ResultRef { get; set; } = string.Empty;
    public string EvidencePackageSignatureStatus { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
