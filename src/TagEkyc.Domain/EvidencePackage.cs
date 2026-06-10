namespace TagEkyc.Domain;

public sealed record EvidencePackage(
    Guid Id,
    Guid VerificationSessionId,
    string PackageVersion,
    HashRef ManifestHash,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> AuditEventRefs,
    Guid ResultRef,
    HashRef PackageHash,
    SignaturePlaceholderStatus EvidencePackageSignatureStatus,
    DateTimeOffset CreatedAt);
