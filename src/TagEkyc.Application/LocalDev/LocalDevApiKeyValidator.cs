using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Domain;

namespace TagEkyc.Application.LocalDev;

public sealed class LocalDevApiKeyValidator(IApiKeyStore apiKeyStore, ILocalDevClientPolicyProvider policies)
{
    public async Task<SessionOperationResult<AuthenticatedClientContext>> ValidateAsync(
        string? apiKeyValue,
        string? requiredScope = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKeyValue))
        {
            return Unauthorized("INVALID_API_KEY", "API key is required.");
        }

        var apiKey = await apiKeyStore.FindByPresentedKeyAsync(apiKeyValue, cancellationToken);
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

        var policy = await policies.GetPolicyAsync(apiKey.ClientApplicationId, cancellationToken);
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
