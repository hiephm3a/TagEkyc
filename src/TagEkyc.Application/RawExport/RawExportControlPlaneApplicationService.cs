using System.Text.RegularExpressions;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Domain;

namespace TagEkyc.Application.RawExport;

public sealed class RawExportControlPlaneApplicationService(
    IRawExportAuthorizationRepository authorizations,
    IRawExportJobRepository jobs,
    IRawExportJobPackageProjectionReader packages)
    : IRawExportControlPlaneApplicationService
{
    public const string AuthorizeScope = "business.raw-export.authorize";
    public const string JobScope = "business.raw-export.job.manage";

    private static readonly Regex IdempotencyPattern = new(
        "^[A-Za-z0-9._:-]{1,128}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public async Task<SessionOperationResult<RawExportAuthorizationDecisionDto>> AuthorizeAsync(
        AuthenticatedClientContext actor,
        AuthorizeRawExportRequestDto request,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!Authorized(actor, AuthorizeScope))
            return Failure<RawExportAuthorizationDecisionDto>(RawExportControlPlaneErrorCodes.Forbidden, 403);
        if (request is null || request.VerificationSessionId == Guid.Empty
            || request.PolicyId == Guid.Empty || request.PolicyVersion < 1
            || !ValidIdempotency(idempotencyKey)
            || !TryClasses(request.RequestedRawClasses, out var classes))
            return Failure<RawExportAuthorizationDecisionDto>(RawExportControlPlaneErrorCodes.RequestInvalid, 400);

        try
        {
            var result = await authorizations.AuthorizeExportAsync(
                new AuthorizeRawExportCommand(
                    Actor(actor), request.VerificationSessionId, request.PolicyId,
                    request.PolicyVersion, classes, idempotencyKey!),
                cancellationToken).ConfigureAwait(false);
            return SessionOperationResult<RawExportAuthorizationDecisionDto>.Success(
                new RawExportAuthorizationDecisionDto(
                    result.Decision.ExportDecisionId,
                    result.Decision.Outcome.ToString(),
                    result.Decision.PrimaryCause?.ToString(),
                    result.Permit?.PermitId,
                    result.Permit?.DecisionExpiresAtUtc,
                    result.PermitClasses.OrderBy(value => value.Ordinal)
                        .Select(value => value.RawClass.ToString()).ToArray()));
        }
        catch (RawExportAuthorizationInputException)
        {
            return Failure<RawExportAuthorizationDecisionDto>(RawExportControlPlaneErrorCodes.RequestInvalid, 400);
        }
        catch (RawExportAuthorizationException)
        {
            return Failure<RawExportAuthorizationDecisionDto>(RawExportControlPlaneErrorCodes.Unavailable, 503);
        }
    }

    public async Task<SessionOperationResult<RawExportJobBindingDto>> BindJobAsync(
        AuthenticatedClientContext actor,
        BindRawExportJobRequestDto request,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!Authorized(actor, JobScope))
            return Failure<RawExportJobBindingDto>(RawExportControlPlaneErrorCodes.Forbidden, 403);
        if (request is null || request.PermitId == Guid.Empty || !ValidIdempotency(idempotencyKey))
            return Failure<RawExportJobBindingDto>(RawExportControlPlaneErrorCodes.RequestInvalid, 400);
        try
        {
            var result = await jobs.BindAsync(
                new BindRawExportJobCommand(Actor(actor), request.PermitId, idempotencyKey!),
                cancellationToken).ConfigureAwait(false);
            return SessionOperationResult<RawExportJobBindingDto>.Success(
                new RawExportJobBindingDto(result.JobId, result.Status.ToString()));
        }
        catch (RawExportJobException exception) when (exception.Code.EndsWith("REQUEST_VALIDATION_FAILED", StringComparison.Ordinal))
        {
            return Failure<RawExportJobBindingDto>(RawExportControlPlaneErrorCodes.RequestInvalid, 400);
        }
        catch (RawExportJobException)
        {
            return Failure<RawExportJobBindingDto>(RawExportControlPlaneErrorCodes.Unavailable, 503);
        }
    }

    public async Task<SessionOperationResult<RawExportJobStatusDto>> ReadJobAsync(
        AuthenticatedClientContext actor,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        if (!Authorized(actor, JobScope))
            return Failure<RawExportJobStatusDto>(RawExportControlPlaneErrorCodes.Forbidden, 403);
        if (jobId == Guid.Empty)
            return Failure<RawExportJobStatusDto>(RawExportControlPlaneErrorCodes.NotFound, 404);
        try
        {
            var result = await jobs.ReadAsync(
                new ReadRawExportJobCommand(Actor(actor), jobId), cancellationToken).ConfigureAwait(false);
            if (result.Status != RawExportJobReadStatus.Found || result.Job is null)
                return Failure<RawExportJobStatusDto>(RawExportControlPlaneErrorCodes.NotFound, 404);
            var package = await packages.ReadByJobAsync(jobId, cancellationToken).ConfigureAwait(false);
            var job = result.Job;
            return SessionOperationResult<RawExportJobStatusDto>.Success(new(
                job.Identity.JobId,
                job.Identity.VerificationSessionId,
                job.Identity.RecipientClientApplicationId,
                job.Head.CurrentState.ToString(),
                job.Head.Revision,
                job.Identity.JobExpiresAt,
                job.Classes.OrderBy(value => value.Ordinal).Select(value => value.RawClass.ToString()).ToArray(),
                package?.PackageId,
                package?.State,
                package?.FinalizedAtUtc));
        }
        catch (RawExportJobException)
        {
            return Failure<RawExportJobStatusDto>(RawExportControlPlaneErrorCodes.Unavailable, 503);
        }
    }

    private static bool Authorized(AuthenticatedClientContext actor, string scope) =>
        actor.CallerCategory == AuthenticatedCallerCategory.BusinessConsumer
        && actor.ApiKeyId != Guid.Empty
        && actor.ClientApplicationId != Guid.Empty
        && actor.PrincipalId != Guid.Empty
        && actor.Scopes.Contains(scope);

    private static AuthenticatedRawExportActor Actor(AuthenticatedClientContext actor) =>
        new(actor.PrincipalId, actor.ClientApplicationId, actor.ApiKeyId);

    private static bool ValidIdempotency(string? value) =>
        value is not null && IdempotencyPattern.IsMatch(value);

    private static bool TryClasses(
        IReadOnlyList<string>? values,
        out IReadOnlyList<RawExportRawClass>? classes)
    {
        classes = null;
        if (values is null) return true;
        if (values.Count == 0 || values.Count > Enum.GetValues<RawExportRawClass>().Length) return false;
        var parsed = new List<RawExportRawClass>(values.Count);
        foreach (var value in values)
        {
            if (!Enum.TryParse<RawExportRawClass>(value, ignoreCase: false, out var item)
                || !Enum.IsDefined(item) || parsed.Contains(item)) return false;
            parsed.Add(item);
        }
        classes = parsed;
        return true;
    }

    private static SessionOperationResult<T> Failure<T>(string code, int status) =>
        SessionOperationResult<T>.Failure(code, "Raw export control request could not be completed.", status);
}
