using TagEkyc.Api.LocalDev;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.BusinessConsumer;

namespace TagEkyc.Api;

public static class VerificationSessionEndpoints
{
    private const string CreateScope = "business.session.create";
    private const string ReadScope = "business.session.read";

    public static IEndpointRouteBuilder MapVerificationSessionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/ekyc/verification-sessions", CreateAsync);
        endpoints.MapGet("/api/ekyc/verification-sessions/{id}", GetAsync);
        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        HttpContext httpContext,
        CreateVerificationSessionRequestDto request,
        ILocalDevApiKeyAuthenticator authenticator,
        IVerificationSessionCommands commands,
        CancellationToken cancellationToken)
    {
        var authentication = authenticator.Authenticate(httpContext, CreateScope);
        if (!authentication.IsSuccess)
        {
            return ToError(authentication.Error!, request.CorrelationId ?? httpContext.TraceIdentifier);
        }

        var idempotencyKey = httpContext.Request.Headers["Idempotency-Key"].FirstOrDefault();
        var result = await commands.CreateAsync(authentication.Value!, request, idempotencyKey, cancellationToken);
        if (!result.IsSuccess)
        {
            return ToError(result.Error!, request.CorrelationId ?? httpContext.TraceIdentifier);
        }

        return Results.Created($"/api/ekyc/verification-sessions/{result.Value!.VerificationSessionId}", result.Value);
    }

    private static async Task<IResult> GetAsync(
        HttpContext httpContext,
        string id,
        ILocalDevApiKeyAuthenticator authenticator,
        IVerificationSessionQueries queries,
        CancellationToken cancellationToken)
    {
        var authentication = authenticator.Authenticate(httpContext, ReadScope);
        if (!authentication.IsSuccess)
        {
            return ToError(authentication.Error!, httpContext.TraceIdentifier);
        }

        var result = await queries.GetSummaryAsync(authentication.Value!, id, cancellationToken);
        if (!result.IsSuccess)
        {
            return ToError(result.Error!, httpContext.TraceIdentifier);
        }

        return Results.Ok(result.Value);
    }

    private static IResult ToError(SessionOperationError error, string correlationId) =>
        Results.Json(
            new ErrorEnvelope(new ErrorBody(error.Code, error.Message, correlationId)),
            statusCode: error.StatusCode);

    private sealed record ErrorEnvelope(ErrorBody Error);

    private sealed record ErrorBody(string Code, string Message, string CorrelationId);
}

