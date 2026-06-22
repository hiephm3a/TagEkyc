using TagEkyc.Contracts.Common;

namespace TagEkyc.Contracts.InternalAudit.Manifest;

public sealed record ManifestVaultRefDto(string Value);

public sealed record ManifestEvidenceRefDto(
    string Type,
    string Id,
    ManifestVaultRefDto? VaultRef,
    string? ArtifactHash,
    string? PayloadHash);

public sealed record ManifestAuditRefDto(
    string EventId,
    string EventType,
    string EventPayloadHash);

public sealed record EvidenceManifestDto(
    string EvidencePackageId,
    string VerificationSessionId,
    string PackageVersion,
    string CanonicalizationScheme,
    string HashAlgorithm,
    string ManifestHash,
    string PackageHash,
    IReadOnlyList<ManifestEvidenceRefDto> EvidenceRefs,
    IReadOnlyList<ManifestAuditRefDto> AuditEventRefs,
    string ResultRef,
    SignaturePlaceholderStatusDto EvidencePackageSignatureStatus,
    DateTimeOffset CreatedAt);
