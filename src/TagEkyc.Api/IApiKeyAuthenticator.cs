using TagEkyc.Application;
using TagEkyc.Application.VerificationSessions;

namespace TagEkyc.Api;

public interface IApiKeyAuthenticator
{
    Task<SessionOperationResult<AuthenticatedClientContext>> AuthenticateAsync(
        HttpContext httpContext,
        string? requiredScope = null,
        CancellationToken cancellationToken = default);
}
