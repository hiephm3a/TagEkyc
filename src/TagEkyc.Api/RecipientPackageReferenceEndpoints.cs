using TagEkyc.Application.Ports;
using TagEkyc.Application.RawExport;
using TagEkyc.Application.VerificationSessions;

namespace TagEkyc.Api;

public static class RecipientPackageReferenceEndpoints
{
    public static IEndpointRouteBuilder MapRecipientPackageReferenceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/ekyc/raw-export/package-references", ListAsync);
        return endpoints;
    }

    private static async Task<IResult> ListAsync(
        HttpContext context,
        IApiKeyAuthenticator authenticator,
        IRecipientPackageReferenceApplicationService service,
        CancellationToken cancellationToken)
    {
        var auth = await authenticator.AuthenticateAsync(
            context, RecipientPackageReferenceApplicationService.RequiredScope, cancellationToken).ConfigureAwait(false);
        if (!auth.IsSuccess) return Error(auth.Error!, context.TraceIdentifier);

        var keys = context.Request.Query.Keys.ToArray();
        var pageSizes = context.Request.Query.TryGetValue("pageSize", out var pageSize)
            ? pageSize.Select(value => value ?? string.Empty).ToArray() : [];
        var cursors = context.Request.Query.TryGetValue("cursor", out var cursor)
            ? cursor.Select(value => value ?? string.Empty).ToArray() : [];
        var result = await service.ListAsync(auth.Value!,
            new RecipientPackageReferenceRawQuery(keys, pageSizes, cursors), cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? Results.Ok(result.Value) : Error(result.Error!, context.TraceIdentifier);
    }

    private static IResult Error(SessionOperationError error, string correlationId) =>
        Results.Json(new ErrorEnvelope(new ErrorBody(error.Code, error.Message, correlationId)), statusCode: error.StatusCode);

    private sealed record ErrorEnvelope(ErrorBody Error);
    private sealed record ErrorBody(string Code, string Message, string CorrelationId);
}
