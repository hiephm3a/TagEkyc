using TagEkyc.Api;
using TagEkyc.Application;
using TagEkyc.Application.LocalDev;
using TagEkyc.Application.VerificationSessions;

namespace TagEkyc.Api.LocalDev;

public sealed class LocalDevApiKeyAuthenticator(LocalDevApiKeyValidator validator) : IApiKeyAuthenticator
{
    public const string HeaderName = "X-TagEkyc-Api-Key";

    public Task<SessionOperationResult<AuthenticatedClientContext>> AuthenticateAsync(
        HttpContext httpContext,
        string? requiredScope = null,
        CancellationToken cancellationToken = default)
    {
        httpContext.Request.Headers.TryGetValue(HeaderName, out var values);
        return validator.ValidateAsync(values.FirstOrDefault(), requiredScope, cancellationToken);
    }
}
