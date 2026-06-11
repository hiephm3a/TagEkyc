using TagEkyc.Contracts.BusinessConsumer;
using TagEkyc.Contracts.CaptureAgent;
using TagEkyc.Contracts.TrustedAdapter;
using TagEkyc.Application;
using TagEkyc.Application.VerificationSessions;

namespace TagEkyc.Application.Ports;

public interface IVerificationSessionCommands
{
    Task<SessionOperationResult<CreateVerificationSessionResponseDto>> CreateAsync(
        AuthenticatedClientContext caller,
        CreateVerificationSessionRequestDto request,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default);
}

public interface IVerificationSessionCompletionCommands
{
    Task<SessionOperationResult<CompleteVerificationSessionResponseDto>> CompleteAsync(
        AuthenticatedClientContext caller,
        string verificationSessionId,
        CompleteVerificationSessionRequestDto request,
        CancellationToken cancellationToken = default);
}

public interface IVerificationSessionQueries
{
    Task<SessionOperationResult<VerificationSessionSummaryDto>> GetSummaryAsync(
        AuthenticatedClientContext caller,
        string verificationSessionId,
        CancellationToken cancellationToken = default);
}

public interface ICaptureArtifactCommands
{
    Task<SessionOperationResult<CaptureArtifactSubmissionResponseDto>> AppendCaptureArtifactAsync(
        AuthenticatedClientContext caller,
        string verificationSessionId,
        CaptureArtifactSubmissionRequestDto request,
        CancellationToken cancellationToken = default);
}

public interface ITrustedEvidenceResultCommands
{
    Task<SessionOperationResult<EvidenceResultSubmissionResponseDto>> AppendEvidenceResultAsync(
        AuthenticatedClientContext caller,
        string verificationSessionId,
        EvidenceResultSubmissionRequestDto request,
        CancellationToken cancellationToken = default);
}

public interface IEvidencePackageQueries
{
    Task<SessionOperationResult<EvidencePackageSummaryDto>> GetEvidencePackageAsync(
        AuthenticatedClientContext caller,
        string evidencePackageId,
        CancellationToken cancellationToken = default);
}

public interface ICompletionNotificationQueries
{
    Task<SessionOperationResult<VerificationCompletedEventDto>> GetCompletionNotificationAsync(
        AuthenticatedClientContext caller,
        string verificationSessionId,
        CancellationToken cancellationToken = default);
}
