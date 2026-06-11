using System.Collections.Concurrent;
using TagEkyc.Application.Ports;
using TagEkyc.Contracts.InternalAudit.Manifest;
using TagEkyc.Domain;

namespace TagEkyc.Application.LocalDev;

internal static class LocalDevInMemoryVisibilityGate
{
    public static readonly object SyncRoot = new();
}

public sealed class LocalDevInMemoryVerificationSessionRepository : IVerificationSessionRepository
{
    private readonly ConcurrentDictionary<Guid, VerificationSession> sessions = new();

    public Task AddAsync(VerificationSession session, CancellationToken cancellationToken = default)
    {
        lock (LocalDevInMemoryVisibilityGate.SyncRoot)
        {
            sessions[session.Id] = session;
        }

        return Task.CompletedTask;
    }

    public Task<VerificationSession?> GetAsync(Guid verificationSessionId, CancellationToken cancellationToken = default)
    {
        lock (LocalDevInMemoryVisibilityGate.SyncRoot)
        {
            sessions.TryGetValue(verificationSessionId, out var session);
            return Task.FromResult(session);
        }
    }

    public Task<VerificationSession?> GetByExternalSessionIdAsync(
        Guid clientApplicationId,
        string externalSessionId,
        CancellationToken cancellationToken = default)
    {
        lock (LocalDevInMemoryVisibilityGate.SyncRoot)
        {
            var session = sessions.Values.SingleOrDefault(candidate =>
                candidate.ClientApplicationId == clientApplicationId &&
                string.Equals(candidate.ExternalSessionId, externalSessionId, StringComparison.Ordinal));

            return Task.FromResult(session);
        }
    }

    public Task SetStateAsync(
        Guid verificationSessionId,
        VerificationSessionState state,
        CancellationToken cancellationToken = default)
    {
        lock (LocalDevInMemoryVisibilityGate.SyncRoot)
        {
            sessions.AddOrUpdate(
                verificationSessionId,
                _ => throw new InvalidOperationException("Session must exist before state can be updated."),
                (_, existing) => existing.WithState(state));
        }

        return Task.CompletedTask;
    }

    public bool TryReplace(VerificationSession expectedSession, VerificationSession replacementSession)
    {
        lock (LocalDevInMemoryVisibilityGate.SyncRoot)
        {
            return sessions.TryUpdate(expectedSession.Id, replacementSession, expectedSession);
        }
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

    public IReadOnlyList<AuditEvent> Events
    {
        get
        {
            lock (LocalDevInMemoryVisibilityGate.SyncRoot)
            {
                return events.ToArray();
            }
        }
    }

    public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        lock (LocalDevInMemoryVisibilityGate.SyncRoot)
        {
            events.Enqueue(auditEvent);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AuditEvent>> ListBySessionAsync(Guid verificationSessionId, CancellationToken cancellationToken = default)
    {
        lock (LocalDevInMemoryVisibilityGate.SyncRoot)
        {
            var matchingEvents = events
                .Where(candidate => candidate.VerificationSessionId == verificationSessionId)
                .ToArray();

            return Task.FromResult<IReadOnlyList<AuditEvent>>(matchingEvents);
        }
    }
}

public sealed class LocalDevInMemoryVerificationDecisionRepository : IVerificationDecisionRepository
{
    private readonly ConcurrentQueue<VerificationDecision> decisions = new();

    public IReadOnlyList<VerificationDecision> Decisions
    {
        get
        {
            lock (LocalDevInMemoryVisibilityGate.SyncRoot)
            {
                return decisions.ToArray();
            }
        }
    }

    public Task AppendAsync(VerificationDecision decision, CancellationToken cancellationToken = default)
    {
        lock (LocalDevInMemoryVisibilityGate.SyncRoot)
        {
            decisions.Enqueue(decision);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<VerificationDecision>> ListBySessionAsync(
        Guid verificationSessionId,
        CancellationToken cancellationToken = default)
    {
        lock (LocalDevInMemoryVisibilityGate.SyncRoot)
        {
            var matchingDecisions = decisions
                .Where(candidate => candidate.VerificationSessionId == verificationSessionId)
                .ToArray();

            return Task.FromResult<IReadOnlyList<VerificationDecision>>(matchingDecisions);
        }
    }

    public Task<VerificationDecision?> GetAsync(Guid verificationDecisionId, CancellationToken cancellationToken = default)
    {
        lock (LocalDevInMemoryVisibilityGate.SyncRoot)
        {
            var decision = decisions.SingleOrDefault(candidate => candidate.Id == verificationDecisionId);
            return Task.FromResult(decision);
        }
    }
}

public sealed class LocalDevInMemoryEvidencePackageRepository : IEvidencePackageRepository
{
    private readonly ConcurrentQueue<EvidencePackage> packages = new();

    public IReadOnlyList<EvidencePackage> Packages
    {
        get
        {
            lock (LocalDevInMemoryVisibilityGate.SyncRoot)
            {
                return packages.ToArray();
            }
        }
    }

    public Task AppendAsync(EvidencePackage evidencePackage, CancellationToken cancellationToken = default)
    {
        lock (LocalDevInMemoryVisibilityGate.SyncRoot)
        {
            packages.Enqueue(evidencePackage);
        }

        return Task.CompletedTask;
    }

    public Task<EvidencePackage?> GetAsync(Guid evidencePackageId, CancellationToken cancellationToken = default)
    {
        lock (LocalDevInMemoryVisibilityGate.SyncRoot)
        {
            var package = packages.SingleOrDefault(candidate => candidate.Id == evidencePackageId);
            return Task.FromResult(package);
        }
    }

    public Task<EvidencePackage?> GetBySessionAsync(Guid verificationSessionId, CancellationToken cancellationToken = default)
    {
        lock (LocalDevInMemoryVisibilityGate.SyncRoot)
        {
            var package = packages
                .Where(candidate => candidate.VerificationSessionId == verificationSessionId)
                .OrderByDescending(candidate => candidate.CreatedAt)
                .FirstOrDefault();

            return Task.FromResult(package);
        }
    }
}

public sealed class LocalDevInMemoryEvidenceManifestRepository : IInternalEvidenceManifestRepository
{
    private readonly ConcurrentQueue<EvidenceManifestDto> manifests = new();

    public IReadOnlyList<EvidenceManifestDto> Manifests
    {
        get
        {
            lock (LocalDevInMemoryVisibilityGate.SyncRoot)
            {
                return manifests.ToArray();
            }
        }
    }

    public Task AppendAsync(EvidenceManifestDto manifest, CancellationToken cancellationToken = default)
    {
        lock (LocalDevInMemoryVisibilityGate.SyncRoot)
        {
            manifests.Enqueue(manifest);
        }

        return Task.CompletedTask;
    }

    public Task<EvidenceManifestDto?> GetByPackageAsync(Guid evidencePackageId, CancellationToken cancellationToken = default)
    {
        lock (LocalDevInMemoryVisibilityGate.SyncRoot)
        {
            var packageId = evidencePackageId.ToString("N");
            var manifest = manifests.SingleOrDefault(candidate =>
                string.Equals(candidate.EvidencePackageId, packageId, StringComparison.Ordinal));

            return Task.FromResult(manifest);
        }
    }
}

public sealed class LocalDevInMemoryVerificationFinalizationBoundary(
    LocalDevInMemoryVerificationSessionRepository sessions,
    LocalDevInMemoryVerificationDecisionRepository decisions,
    LocalDevInMemoryEvidencePackageRepository packages,
    LocalDevInMemoryEvidenceManifestRepository manifests,
    LocalDevInMemoryAuditEventRepository auditEvents)
    : IVerificationFinalizationBoundary
{
    public Task<VerificationFinalizationWriteResult> TryFinalizeAsync(
        VerificationFinalizationWrite write,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (LocalDevInMemoryVisibilityGate.SyncRoot)
        {
            var current = sessions.GetAsync(write.ExpectedSession.Id, cancellationToken)
                .GetAwaiter()
                .GetResult();
            if (current is null)
            {
                return Task.FromResult(new VerificationFinalizationWriteResult(
                    VerificationFinalizationWriteStatus.NotFound,
                    Session: null));
            }

            if (current.State == VerificationSessionState.Completed)
            {
                return Task.FromResult(new VerificationFinalizationWriteResult(
                    VerificationFinalizationWriteStatus.AlreadyCompleted,
                    current));
            }

            if (current != write.ExpectedSession ||
                current.State is VerificationSessionState.Expired
                    or VerificationSessionState.Cancelled
                    or VerificationSessionState.TechnicalTerminal)
            {
                return Task.FromResult(new VerificationFinalizationWriteResult(
                    VerificationFinalizationWriteStatus.StateMismatch,
                    current));
            }

            if (!HasCompleteSnapshot(write.CompletedSession))
            {
                return Task.FromResult(new VerificationFinalizationWriteResult(
                    VerificationFinalizationWriteStatus.StateMismatch,
                    current));
            }

            if (!sessions.TryReplace(current, write.CompletedSession))
            {
                return Task.FromResult(new VerificationFinalizationWriteResult(
                    VerificationFinalizationWriteStatus.StateMismatch,
                    current));
            }

            decisions.AppendAsync(write.Decision, cancellationToken).GetAwaiter().GetResult();
            packages.AppendAsync(write.EvidencePackage, cancellationToken).GetAwaiter().GetResult();
            manifests.AppendAsync(write.Manifest, cancellationToken).GetAwaiter().GetResult();
            auditEvents.AppendAsync(write.CompletionAuditEvent, cancellationToken).GetAwaiter().GetResult();

            return Task.FromResult(new VerificationFinalizationWriteResult(
                VerificationFinalizationWriteStatus.Applied,
                write.CompletedSession));
        }
    }

    private static bool HasCompleteSnapshot(VerificationSession session) =>
        session.State == VerificationSessionState.Completed &&
        session.FinalDecisionId is not null &&
        session.EvidencePackageId is not null &&
        session.EvidencePackageHash is not null &&
        session.ManifestHash is not null &&
        session.CompletedAt is not null &&
        !string.IsNullOrWhiteSpace(session.RequestId) &&
        !string.IsNullOrWhiteSpace(session.CorrelationId);
}
