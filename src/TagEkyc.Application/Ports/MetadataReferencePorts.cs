using TagEkyc.Domain;

namespace TagEkyc.Application.Ports;

public sealed record MetadataReferenceRegistration(
    MetadataReferenceId ReferenceId,
    string ReferenceKind,
    MetadataReferenceState State,
    HashRef? MetadataHash,
    DateTimeOffset RegisteredAt);

public sealed record MetadataReferenceRecord(
    MetadataReferenceId ReferenceId,
    string ReferenceKind,
    MetadataReferenceState State,
    HashRef? MetadataHash,
    DateTimeOffset RegisteredAt,
    DateTimeOffset? LastObservedAt);

public sealed record MetadataReferenceQueryResult(
    MetadataReferenceRecord? Reference)
{
    public bool HasRegisteredMetadata() =>
        Reference?.State == MetadataReferenceState.RegisteredMetadata;

    public bool IsNonSuccess() => !HasRegisteredMetadata();

    public bool RequiresPacketBeforeReliance() => true;

    public bool IsNotReliableForEvidenceReliance() => true;

    public bool DeniesEvidenceAvailabilityProof() => true;

    public bool DeniesCompletePackageProof() => true;

    public bool DeniesArtifactAccessProof() => true;

    public bool DeniesProviderEvidenceAvailabilityProof() => true;

    public bool DeniesReadinessProof() => true;
}

public interface IMetadataReferenceRegistry
{
    Task RegisterAsync(
        MetadataReferenceRegistration registration,
        CancellationToken cancellationToken = default);

    Task<MetadataReferenceQueryResult> QueryAsync(
        MetadataReferenceId referenceId,
        CancellationToken cancellationToken = default);
}
