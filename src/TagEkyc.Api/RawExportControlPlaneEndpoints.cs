using System.Text.Json;
using System.Text.Json.Serialization;
using TagEkyc.Application.Ports;
using TagEkyc.Application.RawExport;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.RawExport;

namespace TagEkyc.Api;

public static class RawExportControlPlaneEndpoints
{
    private const string IdempotencyHeader = "Idempotency-Key";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    public static IEndpointRouteBuilder MapRawExportControlPlaneEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/ekyc/raw-export/authorizations", AuthorizeAsync);
        endpoints.MapPost("/api/ekyc/raw-export/jobs", BindJobAsync);
        endpoints.MapGet("/api/ekyc/raw-export/jobs/{jobId:guid}", ReadJobAsync);
        return endpoints;
    }

    private static async Task<IResult> AuthorizeAsync(
        HttpContext context,
        IApiKeyAuthenticator authenticator,
        IRawExportControlPlaneApplicationService service,
        CancellationToken cancellationToken)
    {
        var auth = await authenticator.AuthenticateAsync(
            context, RawExportControlPlaneApplicationService.AuthorizeScope, cancellationToken).ConfigureAwait(false);
        if (!auth.IsSuccess) return Error(auth.Error!, context.TraceIdentifier);
        var request = await ReadAsync<AuthorizeRawExportRequestDto>(context, cancellationToken).ConfigureAwait(false);
        if (request is null) return Invalid(context.TraceIdentifier);
        var result = await service.AuthorizeAsync(
            auth.Value!, request, Idempotency(context), cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? Results.Ok(result.Value) : Error(result.Error!, context.TraceIdentifier);
    }

    private static async Task<IResult> BindJobAsync(
        HttpContext context,
        IApiKeyAuthenticator authenticator,
        IRawExportControlPlaneApplicationService service,
        CancellationToken cancellationToken)
    {
        var auth = await authenticator.AuthenticateAsync(
            context, RawExportControlPlaneApplicationService.JobScope, cancellationToken).ConfigureAwait(false);
        if (!auth.IsSuccess) return Error(auth.Error!, context.TraceIdentifier);
        var request = await ReadAsync<BindRawExportJobRequestDto>(context, cancellationToken).ConfigureAwait(false);
        if (request is null) return Invalid(context.TraceIdentifier);
        var result = await service.BindJobAsync(
            auth.Value!, request, Idempotency(context), cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess) return Error(result.Error!, context.TraceIdentifier);
        return result.Value!.BindStatus == "NewJob"
            ? Results.Created($"/api/ekyc/raw-export/jobs/{result.Value.JobId:D}", result.Value)
            : Results.Ok(result.Value);
    }

    private static async Task<IResult> ReadJobAsync(
        HttpContext context,
        Guid jobId,
        IApiKeyAuthenticator authenticator,
        IRawExportControlPlaneApplicationService service,
        CancellationToken cancellationToken)
    {
        var auth = await authenticator.AuthenticateAsync(
            context, RawExportControlPlaneApplicationService.JobScope, cancellationToken).ConfigureAwait(false);
        if (!auth.IsSuccess) return Error(auth.Error!, context.TraceIdentifier);
        var result = await service.ReadJobAsync(auth.Value!, jobId, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? Results.Ok(result.Value) : Error(result.Error!, context.TraceIdentifier);
    }

    private static async Task<T?> ReadAsync<T>(HttpContext context, CancellationToken cancellationToken)
    {
        try
        {
            return await context.Request.ReadFromJsonAsync<T>(JsonOptions, cancellationToken).ConfigureAwait(false);
        }
        catch (JsonException) { return default; }
        catch (BadHttpRequestException) { return default; }
    }

    private static string? Idempotency(HttpContext context) =>
        context.Request.Headers.TryGetValue(IdempotencyHeader, out var values) && values.Count == 1
            ? values[0]
            : null;

    private static IResult Invalid(string correlationId) => Results.Json(
        new ErrorEnvelope(new ErrorBody(
            RawExportControlPlaneErrorCodes.RequestInvalid,
            "Raw export control request is invalid.", correlationId)), statusCode: 400);

    private static IResult Error(SessionOperationError error, string correlationId) => Results.Json(
        new ErrorEnvelope(new ErrorBody(error.Code, error.Message, correlationId)), statusCode: error.StatusCode);

    private sealed record ErrorEnvelope(ErrorBody Error);
    private sealed record ErrorBody(string Code, string Message, string CorrelationId);
}
