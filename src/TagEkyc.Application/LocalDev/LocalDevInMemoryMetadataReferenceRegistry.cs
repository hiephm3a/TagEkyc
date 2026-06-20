using System.Collections.Concurrent;
using TagEkyc.Application.Ports;
using TagEkyc.Domain;

namespace TagEkyc.Application.LocalDev;

public sealed class LocalDevInMemoryMetadataReferenceRegistry : IMetadataReferenceRegistry
{
    private readonly ConcurrentDictionary<MetadataReferenceId, MetadataReferenceRecord> references = new();

    public Task RegisterAsync(
        MetadataReferenceRegistration registration,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var record = new MetadataReferenceRecord(
            registration.ReferenceId,
            registration.ReferenceKind,
            registration.State,
            registration.MetadataHash,
            registration.RegisteredAt,
            LastObservedAt: null);

        lock (LocalDevInMemoryVisibilityGate.SyncRoot)
        {
            references[registration.ReferenceId] = record;
        }

        return Task.CompletedTask;
    }

    public Task<MetadataReferenceQueryResult> QueryAsync(
        MetadataReferenceId referenceId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (LocalDevInMemoryVisibilityGate.SyncRoot)
        {
            if (!references.TryGetValue(referenceId, out var record))
            {
                return Task.FromResult(new MetadataReferenceQueryResult(null));
            }

            var observedRecord = record with { LastObservedAt = DateTimeOffset.UtcNow };
            references[referenceId] = observedRecord;

            return Task.FromResult(new MetadataReferenceQueryResult(observedRecord));
        }
    }
}
