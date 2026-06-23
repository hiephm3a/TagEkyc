using TagEkyc.Api.LocalDev;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.BusinessConsumer;
using TagEkyc.Contracts.CaptureAgent;
using TagEkyc.Contracts.TrustedAdapter;

namespace TagEkyc.Api;

public static class VerificationSessionEndpoints
{
    private const string CreateScope = "business.session.create";
    private const string ReadScope = "business.session.read";

    public static IEndpointRouteBuilder MapVerificationSessionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/ekyc/verification-sessions", CreateAsync);
        endpoints.MapGet("/api/ekyc/verification-sessions/{id}", GetAsync);
        endpoints.MapPost("/api/ekyc/verification-sessions/{id}/capture-artifacts", AppendCaptureArtifactAsync);
        endpoints.MapPost("/api/ekyc/verification-sessions/{id}/evidence-results", AppendEvidenceResultAsync);
        endpoints.MapPost("/api/ekyc/verification-sessions/{id}/complete", CompleteAsync);
        endpoints.MapGet("/api/ekyc/evidence-packages/{id}", GetEvidencePackageAsync);
        endpoints.MapGet("/api/ekyc/evidence-packages/{id}/verification-view", GetEvidencePackageVerificationViewAsync);
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

    private static async Task<IResult> AppendCaptureArtifactAsync(
        HttpContext httpContext,
        string id,
        CaptureArtifactSubmissionRequestDto request,
        ILocalDevApiKeyAuthenticator authenticator,
        ICaptureArtifactCommands commands,
        CancellationToken cancellationToken)
    {
        var authentication = authenticator.Authenticate(httpContext);
        if (!authentication.IsSuccess)
        {
            return ToError(authentication.Error!, request.CorrelationId ?? httpContext.TraceIdentifier);
        }

        var result = await commands.AppendCaptureArtifactAsync(authentication.Value!, id, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return ToError(result.Error!, request.CorrelationId ?? httpContext.TraceIdentifier);
        }

        return Results.Created(
            $"/api/ekyc/verification-sessions/{id}/capture-artifacts/{result.Value!.CaptureArtifactId}",
            result.Value);
    }

    private static async Task<IResult> AppendEvidenceResultAsync(
        HttpContext httpContext,
        string id,
        EvidenceResultSubmissionRequestDto request,
        ILocalDevApiKeyAuthenticator authenticator,
        ITrustedEvidenceResultCommands commands,
        CancellationToken cancellationToken)
    {
        var authentication = authenticator.Authenticate(httpContext);
        if (!authentication.IsSuccess)
        {
            return ToError(authentication.Error!, request.CorrelationId ?? httpContext.TraceIdentifier);
        }

        var result = await commands.AppendEvidenceResultAsync(authentication.Value!, id, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return ToError(result.Error!, request.CorrelationId ?? httpContext.TraceIdentifier);
        }

        return Results.Created(
            $"/api/ekyc/verification-sessions/{id}/evidence-results/{result.Value!.EvidenceResultId}",
            result.Value);
    }

    private static async Task<IResult> CompleteAsync(
        HttpContext httpContext,
        string id,
        CompleteVerificationSessionRequestDto request,
        ILocalDevApiKeyAuthenticator authenticator,
        IVerificationSessionCompletionCommands commands,
        CancellationToken cancellationToken)
    {
        var authentication = authenticator.Authenticate(httpContext);
        if (!authentication.IsSuccess)
        {
            return ToError(authentication.Error!, request.CorrelationId ?? httpContext.TraceIdentifier);
        }

        var result = await commands.CompleteAsync(authentication.Value!, id, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return ToError(result.Error!, request.CorrelationId ?? httpContext.TraceIdentifier);
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> GetEvidencePackageAsync(
        HttpContext httpContext,
        string id,
        ILocalDevApiKeyAuthenticator authenticator,
        IEvidencePackageQueries queries,
        CancellationToken cancellationToken)
    {
        var authentication = authenticator.Authenticate(httpContext);
        if (!authentication.IsSuccess)
        {
            return ToError(authentication.Error!, httpContext.TraceIdentifier);
        }

        var result = await queries.GetEvidencePackageAsync(authentication.Value!, id, cancellationToken);
        if (!result.IsSuccess)
        {
            return ToError(result.Error!, httpContext.TraceIdentifier);
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> GetEvidencePackageVerificationViewAsync(
        HttpContext httpContext,
        string id,
        ILocalDevApiKeyAuthenticator authenticator,
        IEvidencePackageQueries queries,
        CancellationToken cancellationToken)
    {
        var authentication = authenticator.Authenticate(httpContext);
        if (!authentication.IsSuccess)
        {
            return ToError(authentication.Error!, httpContext.TraceIdentifier);
        }

        var result = await queries.GetEvidencePackageVerificationViewAsync(authentication.Value!, id, cancellationToken);
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
