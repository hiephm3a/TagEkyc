using TagEkyc.Contracts.BusinessConsumer;
using TagEkyc.Contracts.CaptureAgent;
using TagEkyc.Contracts.TrustedAdapter;

namespace TagEkyc.Application.Ports;

public interface IVerificationSessionCommands
{
    Task<CreateVerificationSessionResponseDto> CreateAsync(
        CreateVerificationSessionRequestDto request,
        CancellationToken cancellationToken = default);
}

public interface IVerificationSessionQueries
{
    Task<VerificationSessionSummaryDto?> GetSummaryAsync(
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
