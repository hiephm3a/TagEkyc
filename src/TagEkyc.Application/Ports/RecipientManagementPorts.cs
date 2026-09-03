using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Domain;

namespace TagEkyc.Application.Ports;

public sealed record ManagedCredentialMaterial(
    Guid ApiKeyId,
    string PresentedKey,
    string KeyPrefix,
    byte[] KeyHash);

public sealed record ValidatedRecipientPublicKey(
    byte[] SubjectPublicKeyInfo,
    byte[] Fingerprint);

public sealed record RecipientManagementWriteResult<T>(T Value, int StatusCode);

public sealed class RecipientCredentialAuthenticationPolicy(
    IApiKeyStore apiKeyStore,
    ILocalDevClientPolicyProvider globalPolicies)
{
    private const string ManagementScope = "operator.raw-export.recipient.manage";
    private const string DeliveryScope = "business.raw-export.package.download";
    private const string ReferenceScope = "business.raw-export.package.references.read";
    private static readonly HashSet<string> ActivationScopes = new(StringComparer.Ordinal)
    {
        DeliveryScope,
        ReferenceScope,
    };

    public async Task<SessionOperationResult<AuthenticatedClientContext>> AuthenticateAsync(
        string? presented,
        string? requiredScope = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(presented))
            return Unauthorized("INVALID_API_KEY", "API key is required.");

        var apiKey = await apiKeyStore.FindByPresentedKeyAsync(presented, cancellationToken)
            .ConfigureAwait(false);
        if (apiKey is null)
            return Unauthorized("INVALID_API_KEY", "API key is invalid.");
        if (apiKey.Status == ApiKeyStatus.Revoked)
            return Unauthorized("API_KEY_REVOKED", "API key is revoked.");
        if (apiKey.Status == ApiKeyStatus.Expired || apiKey.ExpiresAt <= DateTimeOffset.UtcNow)
            return Unauthorized("API_KEY_EXPIRED", "API key is expired.");

        var recipientCredential = apiKey.CallerCategory == AuthenticatedCallerCategory.BusinessConsumer
            && apiKey.Scopes.SetEquals(ActivationScopes)
            && requiredScope is DeliveryScope or ReferenceScope;
        var managementCredential = apiKey.CallerCategory == AuthenticatedCallerCategory.OperatorAdmin
            && apiKey.Scopes.Contains(ManagementScope)
            && string.Equals(requiredScope, ManagementScope, StringComparison.Ordinal);
        if (!recipientCredential && !managementCredential)
        {
            var policy = await globalPolicies.GetPolicyAsync(apiKey.ClientApplicationId, cancellationToken)
                .ConfigureAwait(false);
            if (policy is null || policy.Status == ClientApplicationStatus.Disabled)
                return Unauthorized("CLIENT_APPLICATION_DISABLED", "Client application is disabled.");
        }

        if (!string.IsNullOrWhiteSpace(requiredScope) && !apiKey.Scopes.Contains(requiredScope))
        {
            return SessionOperationResult<AuthenticatedClientContext>.Failure(
                "MISSING_SCOPE", "The API key is not scoped for this endpoint.", 403);
        }

        return SessionOperationResult<AuthenticatedClientContext>.Success(new(
            apiKey.ApiKeyId, apiKey.ClientApplicationId, apiKey.KeyPrefix,
            apiKey.CallerCategory, apiKey.Scopes, apiKey.AllowedClientApplicationIds,
            apiKey.AllowedCaptureAgentIds, apiKey.PrincipalId));
    }

    private static SessionOperationResult<AuthenticatedClientContext> Unauthorized(string code, string message) =>
        SessionOperationResult<AuthenticatedClientContext>.Failure(code, message, 401);
}

public interface IManagedCredentialMaterialGenerator
{
    ManagedCredentialMaterial Generate();
}

public interface IRecipientPublicKeyProfileValidator
{
    bool TryValidate(
        string algorithm,
        string spkiBase64,
        string fingerprintHex,
        out ValidatedRecipientPublicKey? key);
}

public interface IRecipientManagementGateway
{
    Task<SessionOperationResult<RecipientManagementWriteResult<ManagedRecipientIdentityDto>>> EnrollRecipientAsync(
        AuthenticatedClientContext actor,
        EnrollManagedRecipientRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<SessionOperationResult<RecipientManagementWriteResult<ManagedCredentialDto>>> IssueCredentialAsync(
        AuthenticatedClientContext actor,
        IssueManagedRecipientCredentialRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<SessionOperationResult<RecipientManagementWriteResult<ManagedCredentialDto>>> ReplaceCredentialAsync(
        AuthenticatedClientContext actor,
        ReplaceManagedRecipientCredentialRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<SessionOperationResult<RecipientManagementWriteResult<ManagedCredentialDto>>> RevokeCredentialAsync(
        AuthenticatedClientContext actor,
        RevokeManagedRecipientCredentialRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<SessionOperationResult<RecipientManagementWriteResult<RecipientPublicKeyOperationResultDto>>> EnrollKeyAsync(
        AuthenticatedClientContext actor,
        EnrollRecipientPublicKeyRequest request,
        ValidatedRecipientPublicKey key,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<SessionOperationResult<RecipientManagementWriteResult<RecipientPublicKeyOperationResultDto>>> RotateKeyAsync(
        AuthenticatedClientContext actor,
        RotateRecipientPublicKeyRequest request,
        ValidatedRecipientPublicKey key,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<SessionOperationResult<RecipientManagementWriteResult<RecipientPublicKeyOperationResultDto>>> RevokeKeyAsync(
        AuthenticatedClientContext actor,
        RevokeRecipientPublicKeyRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<SessionOperationResult<ManagedRecipientReadinessDto>> ReadReadinessAsync(
        AuthenticatedClientContext actor,
        Guid recipientClientApplicationId,
        CancellationToken cancellationToken);
}

public interface IRecipientManagementApplicationService
{
    Task<SessionOperationResult<RecipientManagementWriteResult<ManagedRecipientIdentityDto>>> EnrollRecipientAsync(
        AuthenticatedClientContext actor, EnrollManagedRecipientRequest request,
        string? idempotencyKey, CancellationToken cancellationToken);
    Task<SessionOperationResult<RecipientManagementWriteResult<ManagedCredentialDto>>> IssueCredentialAsync(
        AuthenticatedClientContext actor, IssueManagedRecipientCredentialRequest request,
        string? idempotencyKey, CancellationToken cancellationToken);
    Task<SessionOperationResult<RecipientManagementWriteResult<ManagedCredentialDto>>> ReplaceCredentialAsync(
        AuthenticatedClientContext actor, ReplaceManagedRecipientCredentialRequest request,
        string? idempotencyKey, CancellationToken cancellationToken);
    Task<SessionOperationResult<RecipientManagementWriteResult<ManagedCredentialDto>>> RevokeCredentialAsync(
        AuthenticatedClientContext actor, RevokeManagedRecipientCredentialRequest request,
        string? idempotencyKey, CancellationToken cancellationToken);
    Task<SessionOperationResult<RecipientManagementWriteResult<RecipientPublicKeyOperationResultDto>>> EnrollKeyAsync(
        AuthenticatedClientContext actor, EnrollRecipientPublicKeyRequest request,
        string? idempotencyKey, CancellationToken cancellationToken);
    Task<SessionOperationResult<RecipientManagementWriteResult<RecipientPublicKeyOperationResultDto>>> RotateKeyAsync(
        AuthenticatedClientContext actor, RotateRecipientPublicKeyRequest request,
        string? idempotencyKey, CancellationToken cancellationToken);
    Task<SessionOperationResult<RecipientManagementWriteResult<RecipientPublicKeyOperationResultDto>>> RevokeKeyAsync(
        AuthenticatedClientContext actor, RevokeRecipientPublicKeyRequest request,
        string? idempotencyKey, CancellationToken cancellationToken);
    Task<SessionOperationResult<ManagedRecipientReadinessDto>> ReadReadinessAsync(
        AuthenticatedClientContext actor, Guid recipientClientApplicationId,
        CancellationToken cancellationToken);
}
