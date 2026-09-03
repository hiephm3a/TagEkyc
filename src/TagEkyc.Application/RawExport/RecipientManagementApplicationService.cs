using System.Text;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.RawExport;

namespace TagEkyc.Application.RawExport;

public sealed class RecipientManagementApplicationService(
    IRecipientManagementGateway gateway,
    IRecipientPublicKeyProfileValidator keyProfileValidator)
    : IRecipientManagementApplicationService
{
    public const string RequiredScope = "operator.raw-export.recipient.manage";

    public Task<SessionOperationResult<RecipientManagementWriteResult<ManagedRecipientIdentityDto>>> EnrollRecipientAsync(
        AuthenticatedClientContext actor,
        EnrollManagedRecipientRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var forbidden = Authorize<RecipientManagementWriteResult<ManagedRecipientIdentityDto>>(actor);
        if (forbidden is not null) return Task.FromResult(forbidden);
        if (!ValidIdempotencyKey(idempotencyKey)
            || request.RecipientClientApplicationId == Guid.Empty
            || request.PrincipalId == Guid.Empty
            || request.PrincipalId == request.RecipientClientApplicationId)
            return Task.FromResult(Invalid<RecipientManagementWriteResult<ManagedRecipientIdentityDto>>());
        return gateway.EnrollRecipientAsync(actor, request, idempotencyKey!, cancellationToken);
    }

    public Task<SessionOperationResult<RecipientManagementWriteResult<ManagedCredentialDto>>> IssueCredentialAsync(
        AuthenticatedClientContext actor,
        IssueManagedRecipientCredentialRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var forbidden = Authorize<RecipientManagementWriteResult<ManagedCredentialDto>>(actor);
        if (forbidden is not null) return Task.FromResult(forbidden);
        if (!ValidIdempotencyKey(idempotencyKey)
            || request.RecipientClientApplicationId == Guid.Empty)
            return Task.FromResult(Invalid<RecipientManagementWriteResult<ManagedCredentialDto>>());
        return gateway.IssueCredentialAsync(actor, request, idempotencyKey!, cancellationToken);
    }

    public Task<SessionOperationResult<RecipientManagementWriteResult<ManagedCredentialDto>>> ReplaceCredentialAsync(
        AuthenticatedClientContext actor,
        ReplaceManagedRecipientCredentialRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var forbidden = Authorize<RecipientManagementWriteResult<ManagedCredentialDto>>(actor);
        if (forbidden is not null) return Task.FromResult(forbidden);
        if (!ValidIdempotencyKey(idempotencyKey)
            || request.RecipientClientApplicationId == Guid.Empty
            || request.CurrentApiKeyId == Guid.Empty
            || request.CurrentCredentialRevision <= 0
            || !ValidReason(request.Reason))
            return Task.FromResult(Invalid<RecipientManagementWriteResult<ManagedCredentialDto>>());
        return gateway.ReplaceCredentialAsync(actor, request, idempotencyKey!, cancellationToken);
    }

    public Task<SessionOperationResult<RecipientManagementWriteResult<ManagedCredentialDto>>> RevokeCredentialAsync(
        AuthenticatedClientContext actor,
        RevokeManagedRecipientCredentialRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var forbidden = Authorize<RecipientManagementWriteResult<ManagedCredentialDto>>(actor);
        if (forbidden is not null) return Task.FromResult(forbidden);
        if (!ValidIdempotencyKey(idempotencyKey)
            || request.RecipientClientApplicationId == Guid.Empty
            || request.ApiKeyId == Guid.Empty
            || request.ExpectedRevision <= 0
            || !ValidReason(request.Reason))
            return Task.FromResult(Invalid<RecipientManagementWriteResult<ManagedCredentialDto>>());
        return gateway.RevokeCredentialAsync(actor, request, idempotencyKey!, cancellationToken);
    }

    public Task<SessionOperationResult<RecipientManagementWriteResult<RecipientPublicKeyOperationResultDto>>> EnrollKeyAsync(
        AuthenticatedClientContext actor,
        EnrollRecipientPublicKeyRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var forbidden = Authorize<RecipientManagementWriteResult<RecipientPublicKeyOperationResultDto>>(actor);
        if (forbidden is not null) return Task.FromResult(forbidden);
        if (!ValidIdempotencyKey(idempotencyKey)
            || !ValidKeyIdentity(request.RecipientClientApplicationId, request.RecipientKeyId)
            || request.RecipientKeyVersion <= 0
            || request.ValidFromUtc >= request.ValidUntilUtc)
            return Task.FromResult(Invalid<RecipientManagementWriteResult<RecipientPublicKeyOperationResultDto>>());
        if (!keyProfileValidator.TryValidate(
                request.PublicKeyAlgorithm, request.PublicKeySpkiBase64,
                request.PublicKeyFingerprintHex, out var key))
            return Task.FromResult(KeyProfileInvalid<RecipientPublicKeyOperationResultDto>());
        return gateway.EnrollKeyAsync(actor, request, key!, idempotencyKey!, cancellationToken);
    }

    public Task<SessionOperationResult<RecipientManagementWriteResult<RecipientPublicKeyOperationResultDto>>> RotateKeyAsync(
        AuthenticatedClientContext actor,
        RotateRecipientPublicKeyRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var forbidden = Authorize<RecipientManagementWriteResult<RecipientPublicKeyOperationResultDto>>(actor);
        if (forbidden is not null) return Task.FromResult(forbidden);
        if (!ValidIdempotencyKey(idempotencyKey)
            || !ValidKeyIdentity(request.RecipientClientApplicationId, request.RecipientKeyId)
            || request.CurrentKeyVersion <= 0
            || request.NewKeyVersion <= request.CurrentKeyVersion
            || request.CurrentKeyRevision <= 0
            || request.ValidFromUtc >= request.ValidUntilUtc
            || !ValidReason(request.Reason))
            return Task.FromResult(Invalid<RecipientManagementWriteResult<RecipientPublicKeyOperationResultDto>>());
        if (!keyProfileValidator.TryValidate(
                request.PublicKeyAlgorithm, request.PublicKeySpkiBase64,
                request.PublicKeyFingerprintHex, out var key))
            return Task.FromResult(KeyProfileInvalid<RecipientPublicKeyOperationResultDto>());
        return gateway.RotateKeyAsync(actor, request, key!, idempotencyKey!, cancellationToken);
    }

    public Task<SessionOperationResult<RecipientManagementWriteResult<RecipientPublicKeyOperationResultDto>>> RevokeKeyAsync(
        AuthenticatedClientContext actor,
        RevokeRecipientPublicKeyRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var forbidden = Authorize<RecipientManagementWriteResult<RecipientPublicKeyOperationResultDto>>(actor);
        if (forbidden is not null) return Task.FromResult(forbidden);
        if (!ValidIdempotencyKey(idempotencyKey)
            || !ValidKeyIdentity(request.RecipientClientApplicationId, request.RecipientKeyId)
            || request.RecipientKeyVersion <= 0
            || request.ExpectedRevision <= 0
            || !ValidReason(request.Reason))
            return Task.FromResult(Invalid<RecipientManagementWriteResult<RecipientPublicKeyOperationResultDto>>());
        return gateway.RevokeKeyAsync(actor, request, idempotencyKey!, cancellationToken);
    }

    public Task<SessionOperationResult<ManagedRecipientReadinessDto>> ReadReadinessAsync(
        AuthenticatedClientContext actor,
        Guid recipientClientApplicationId,
        CancellationToken cancellationToken)
    {
        var forbidden = Authorize<ManagedRecipientReadinessDto>(actor);
        if (forbidden is not null) return Task.FromResult(forbidden);
        if (recipientClientApplicationId == Guid.Empty)
            return Task.FromResult(Invalid<ManagedRecipientReadinessDto>());
        return gateway.ReadReadinessAsync(actor, recipientClientApplicationId, cancellationToken);
    }

    private static SessionOperationResult<T>? Authorize<T>(AuthenticatedClientContext actor)
    {
        if (actor.CallerCategory != AuthenticatedCallerCategory.OperatorAdmin
            || actor.PrincipalId == Guid.Empty
            || actor.ApiKeyId == Guid.Empty
            || !actor.Scopes.Contains(RequiredScope))
            return SessionOperationResult<T>.Failure(
                RecipientManagementErrorCodes.Forbidden,
                "Recipient management is not authorized.", 403);
        return null;
    }

    private static bool ValidIdempotencyKey(string? value) =>
        value is { Length: >= 1 and <= 128 }
        && Encoding.ASCII.GetByteCount(value) == value.Length
        && value.All(character => char.IsAsciiLetterOrDigit(character)
            || character is '.' or '_' or ':' or '-');

    private static bool ValidReason(string? reason) =>
        reason is { Length: >= 1 and <= 128 }
        && reason == reason.Trim()
        && !reason.Any(char.IsControl);

    private static bool ValidKeyIdentity(Guid recipient, string? keyId) =>
        recipient != Guid.Empty
        && keyId is { Length: >= 1 and <= 128 }
        && Encoding.UTF8.GetByteCount(keyId) <= 128
        && keyId == keyId.Trim()
        && !keyId.Any(char.IsControl);

    private static SessionOperationResult<T> Invalid<T>() =>
        SessionOperationResult<T>.Failure(
            RecipientManagementErrorCodes.RequestInvalid,
            "Recipient management request is invalid.", 400);

    private static SessionOperationResult<RecipientManagementWriteResult<T>> KeyProfileInvalid<T>() =>
        SessionOperationResult<RecipientManagementWriteResult<T>>.Failure(
            RecipientManagementErrorCodes.KeyProfileInvalid,
            "Recipient public key profile is invalid.", 400);
}
