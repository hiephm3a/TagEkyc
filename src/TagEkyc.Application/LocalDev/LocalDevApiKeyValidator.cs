using TagEkyc.Application.VerificationSessions;
using TagEkyc.Domain;

namespace TagEkyc.Application.LocalDev;

public sealed class LocalDevApiKeyValidator(LocalDevRuntimePolicySource policies)
{
    public SessionOperationResult<AuthenticatedClientContext> Validate(string? apiKeyValue, string? requiredScope = null)
    {
        if (string.IsNullOrWhiteSpace(apiKeyValue))
        {
            return Unauthorized("INVALID_API_KEY", "API key is required.");
        }

        var apiKey = policies.ApiKeys.SingleOrDefault(candidate => candidate.ApiKeyValue == apiKeyValue);
        if (apiKey is null)
        {
            return Unauthorized("INVALID_API_KEY", "API key is invalid.");
        }

        if (apiKey.Status == ApiKeyStatus.Revoked)
        {
            return Unauthorized("API_KEY_REVOKED", "API key is revoked.");
        }

        if (apiKey.Status == ApiKeyStatus.Expired || apiKey.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return Unauthorized("API_KEY_EXPIRED", "API key is expired.");
        }

        var policy = policies.Policies.SingleOrDefault(candidate => candidate.ClientApplicationId == apiKey.ClientApplicationId);
        if (policy is null || policy.Status == ClientApplicationStatus.Disabled)
        {
            return Unauthorized("CLIENT_APPLICATION_DISABLED", "Client application is disabled.");
        }

        if (!string.IsNullOrWhiteSpace(requiredScope) && !apiKey.Scopes.Contains(requiredScope))
        {
            return SessionOperationResult<AuthenticatedClientContext>.Failure(
                "MISSING_SCOPE",
                "The API key is not scoped for this endpoint.",
                403);
        }

        return SessionOperationResult<AuthenticatedClientContext>.Success(new AuthenticatedClientContext(
            apiKey.ApiKeyId,
            apiKey.ClientApplicationId,
            apiKey.KeyPrefix,
            apiKey.CallerCategory,
            apiKey.Scopes,
            apiKey.AllowedClientApplicationIds,
            apiKey.AllowedCaptureAgentIds));
    }

    private static SessionOperationResult<AuthenticatedClientContext> Unauthorized(string code, string message) =>
        SessionOperationResult<AuthenticatedClientContext>.Failure(code, message, 401);
}
