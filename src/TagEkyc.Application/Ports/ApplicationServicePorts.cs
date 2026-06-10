using TagEkyc.Contracts.BusinessConsumer;
using TagEkyc.Contracts.CaptureAgent;
using TagEkyc.Contracts.TrustedAdapter;
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

public interface IVerificationSessionQueries
{
    Task<SessionOperationResult<VerificationSessionSummaryDto>> GetSummaryAsync(
        AuthenticatedClientContext caller,
        string verificationSessionId,
        CancellationToken cancellationToken = default);
}

public interface ICaptureArtifactCommands
{
    Task<CaptureArtifactSubmissionResponseDto> AppendCaptureArtifactAsync(
        string verificationSessionId,
        CaptureArtifactSubmissionRequestDto request,
        CancellationToken cancellationToken = default);
}

public interface ITrustedEvidenceResultCommands
{
    Task<EvidenceResultSubmissionResponseDto> AppendEvidenceResultAsync(
        string verificationSessionId,
        EvidenceResultSubmissionRequestDto request,
        CancellationToken cancellationToken = default);
}
