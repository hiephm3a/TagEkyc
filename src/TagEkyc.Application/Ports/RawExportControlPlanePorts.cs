using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.RawExport;

namespace TagEkyc.Application.Ports;

public sealed record RawExportJobPackageProjection(
    Guid PackageId,
    string State,
    DateTimeOffset? FinalizedAtUtc);

public interface IRawExportJobPackageProjectionReader
{
    Task<RawExportJobPackageProjection?> ReadByJobAsync(
        Guid jobId,
        CancellationToken cancellationToken = default);
}

public interface IRawExportControlPlaneApplicationService
{
    Task<SessionOperationResult<RawExportAuthorizationDecisionDto>> AuthorizeAsync(
        AuthenticatedClientContext actor,
        AuthorizeRawExportRequestDto request,
        string? idempotencyKey,
        CancellationToken cancellationToken);

    Task<SessionOperationResult<RawExportJobBindingDto>> BindJobAsync(
        AuthenticatedClientContext actor,
        BindRawExportJobRequestDto request,
        string? idempotencyKey,
        CancellationToken cancellationToken);

    Task<SessionOperationResult<RawExportJobStatusDto>> ReadJobAsync(
        AuthenticatedClientContext actor,
        Guid jobId,
        CancellationToken cancellationToken);
}
