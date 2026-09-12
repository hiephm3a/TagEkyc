using TagEkyc.Application;
using TagEkyc.Application.Ports;

namespace TagEkyc.Api;

public static class CaptureAgentConfigurationEndpoints
{
    public static IEndpointRouteBuilder MapCaptureAgentConfigurationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/ekyc/capture-agents/self/configuration", GetAsync);
        return endpoints;
    }

    private static async Task<IResult> GetAsync(HttpContext context, IApiKeyAuthenticator authenticator,
        ICaptureAgentConfigurationProvider provider, CancellationToken cancellationToken)
    {
        var authentication = await authenticator.AuthenticateAsync(context, RawExportSourceIngressEndpoints.Scope, cancellationToken);
        if (!authentication.IsSuccess)
            return Results.Json(new { error = new { code = authentication.Error!.Code } }, statusCode: authentication.Error.StatusCode);
        if (authentication.Value!.CallerCategory != AuthenticatedCallerCategory.CaptureAgent)
            return Results.Json(new { error = new { code = "ACCESS_DENIED" } }, statusCode: StatusCodes.Status403Forbidden);

        var projection = await provider.GetSelfAsync(authentication.Value, cancellationToken);
        if (projection is null)
            return Results.Json(new { error = new { code = "CAPTURE_AGENT_CONFIGURATION_NOT_AVAILABLE" } }, statusCode: StatusCodes.Status503ServiceUnavailable);

        context.Response.Headers.ETag = projection.ETag;
        if (context.Request.Headers.IfNoneMatch.Count == 1 &&
            string.Equals(context.Request.Headers.IfNoneMatch[0], projection.ETag, StringComparison.Ordinal))
            return Results.StatusCode(StatusCodes.Status304NotModified);

        return Results.Ok(projection.Configuration);
    }
}
