using TagEkyc.Domain;

namespace TagEkyc.Application.Ports;

public interface IVerificationSessionRepository
{
    Task AddAsync(VerificationSession session, CancellationToken cancellationToken = default);

    Task<VerificationSession?> GetAsync(Guid verificationSessionId, CancellationToken cancellationToken = default);

    Task<VerificationSession?> GetByExternalSessionIdAsync(
        Guid clientApplicationId,
        string externalSessionId,
        CancellationToken cancellationToken = default);
}

public interface ICaptureArtifactRepository
{
    Task AppendAsync(CaptureArtifact artifact, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CaptureArtifact>> ListBySessionAsync(Guid verificationSessionId, CancellationToken cancellationToken = default);
}

public interface IEvidenceResultRepository
{
    Task AppendAsync(EvidenceResult evidenceResult, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EvidenceResult>> ListBySessionAsync(Guid verificationSessionId, CancellationToken cancellationToken = default);
}

public interface IVerificationDecisionRepository
{
    Task AppendAsync(VerificationDecision decision, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VerificationDecision>> ListBySessionAsync(Guid verificationSessionId, CancellationToken cancellationToken = default);
}

public interface IEvidencePackageRepository
{
    Task AppendAsync(EvidencePackage evidencePackage, CancellationToken cancellationToken = default);

    Task<EvidencePackage?> GetAsync(Guid evidencePackageId, CancellationToken cancellationToken = default);
}

public interface IAuditEventRepository
{
    Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditEvent>> ListBySessionAsync(Guid verificationSessionId, CancellationToken cancellationToken = default);
}

public interface IClientApplicationPolicyReader
{
    Task<ClientApplication?> GetClientApplicationAsync(Guid clientApplicationId, CancellationToken cancellationToken = default);

    Task<ApiKeyMetadata?> GetApiKeyMetadataAsync(string keyPrefix, CancellationToken cancellationToken = default);
}
