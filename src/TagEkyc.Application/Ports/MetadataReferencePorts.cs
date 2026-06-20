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
    MetadataReferenceRecord? Reference);

public interface IMetadataReferenceRegistry
{
    Task RegisterAsync(
        MetadataReferenceRegistration registration,
        CancellationToken cancellationToken = default);

    Task<MetadataReferenceQueryResult> QueryAsync(
        MetadataReferenceId referenceId,
        CancellationToken cancellationToken = default);
}
