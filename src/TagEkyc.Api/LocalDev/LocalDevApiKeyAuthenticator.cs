using TagEkyc.Application;
using TagEkyc.Application.LocalDev;
using TagEkyc.Application.VerificationSessions;

namespace TagEkyc.Api.LocalDev;

public interface ILocalDevApiKeyAuthenticator
{
    SessionOperationResult<AuthenticatedClientContext> Authenticate(HttpContext httpContext, string? requiredScope = null);
}

public sealed class LocalDevApiKeyAuthenticator(LocalDevApiKeyValidator validator) : ILocalDevApiKeyAuthenticator
{
    public const string HeaderName = "X-TagEkyc-Api-Key";

    public SessionOperationResult<AuthenticatedClientContext> Authenticate(HttpContext httpContext, string? requiredScope = null)
    {
        httpContext.Request.Headers.TryGetValue(HeaderName, out var values);
        return validator.Validate(values.FirstOrDefault(), requiredScope);
    }
}
