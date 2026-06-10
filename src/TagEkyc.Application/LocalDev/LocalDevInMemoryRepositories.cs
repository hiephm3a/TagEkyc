using System.Collections.Concurrent;
using TagEkyc.Application.Ports;
using TagEkyc.Domain;

namespace TagEkyc.Application.LocalDev;

public sealed class LocalDevInMemoryVerificationSessionRepository : IVerificationSessionRepository
{
    private readonly ConcurrentDictionary<Guid, VerificationSession> sessions = new();

    public Task AddAsync(VerificationSession session, CancellationToken cancellationToken = default)
    {
        sessions[session.Id] = session;
        return Task.CompletedTask;
    }

    public Task<VerificationSession?> GetAsync(Guid verificationSessionId, CancellationToken cancellationToken = default)
    {
        sessions.TryGetValue(verificationSessionId, out var session);
        return Task.FromResult(session);
    }

    public Task<VerificationSession?> GetByExternalSessionIdAsync(
        Guid clientApplicationId,
        string externalSessionId,
        CancellationToken cancellationToken = default)
    {
        var session = sessions.Values.SingleOrDefault(candidate =>
            candidate.ClientApplicationId == clientApplicationId &&
            string.Equals(candidate.ExternalSessionId, externalSessionId, StringComparison.Ordinal));

        return Task.FromResult(session);
    }

    public Task SetStateAsync(
        Guid verificationSessionId,
        VerificationSessionState state,
        CancellationToken cancellationToken = default)
    {
        sessions.AddOrUpdate(
            verificationSessionId,
            _ => throw new InvalidOperationException("Session must exist before state can be updated."),
            (_, existing) => existing.WithState(state));

        return Task.CompletedTask;
    }
}

public sealed class LocalDevInMemoryCaptureArtifactRepository : ICaptureArtifactRepository
{
    private readonly ConcurrentQueue<CaptureArtifact> artifacts = new();

    public IReadOnlyList<CaptureArtifact> Artifacts => artifacts.ToArray();

    public Task AppendAsync(CaptureArtifact artifact, CancellationToken cancellationToken = default)
    {
        artifacts.Enqueue(artifact);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<CaptureArtifact>> ListBySessionAsync(Guid verificationSessionId, CancellationToken cancellationToken = default)
    {
        var matchingArtifacts = artifacts
            .Where(candidate => candidate.VerificationSessionId == verificationSessionId)
            .ToArray();

        return Task.FromResult<IReadOnlyList<CaptureArtifact>>(matchingArtifacts);
    }
}

public sealed class LocalDevInMemoryEvidenceResultRepository : IEvidenceResultRepository
{
    private readonly ConcurrentQueue<EvidenceResult> evidenceResults = new();

    public IReadOnlyList<EvidenceResult> EvidenceResults => evidenceResults.ToArray();

    public Task AppendAsync(EvidenceResult evidenceResult, CancellationToken cancellationToken = default)
    {
        evidenceResults.Enqueue(evidenceResult);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<EvidenceResult>> ListBySessionAsync(Guid verificationSessionId, CancellationToken cancellationToken = default)
    {
        var matchingEvidence = evidenceResults
            .Where(candidate => candidate.VerificationSessionId == verificationSessionId)
            .ToArray();

        return Task.FromResult<IReadOnlyList<EvidenceResult>>(matchingEvidence);
    }
}

public sealed class LocalDevInMemoryAuditEventRepository : IAuditEventRepository
{
    private readonly ConcurrentQueue<AuditEvent> events = new();

    public IReadOnlyList<AuditEvent> Events => events.ToArray();

    public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        events.Enqueue(auditEvent);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AuditEvent>> ListBySessionAsync(Guid verificationSessionId, CancellationToken cancellationToken = default)
    {
        var matchingEvents = events
            .Where(candidate => candidate.VerificationSessionId == verificationSessionId)
            .ToArray();

        return Task.FromResult<IReadOnlyList<AuditEvent>>(matchingEvents);
    }
}
