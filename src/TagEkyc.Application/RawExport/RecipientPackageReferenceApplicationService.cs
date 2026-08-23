using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.RawExport;

namespace TagEkyc.Application.RawExport;

public sealed class RecipientPackageReferenceApplicationService(
    IRecipientPackageReferenceGateway gateway) : IRecipientPackageReferenceApplicationService
{
    public const string RequiredScope = "business.raw-export.package.references.read";
    public Task<SessionOperationResult<RecipientPackageReferencePageDto>> ListAsync(
        AuthenticatedClientContext actor,
        RecipientPackageReferenceRawQuery query,
        CancellationToken cancellationToken)
    {
        if (actor.CallerCategory != AuthenticatedCallerCategory.BusinessConsumer
            || actor.ClientApplicationId == Guid.Empty
            || actor.PrincipalId == Guid.Empty
            || !actor.Scopes.Contains(RequiredScope))
        {
            return Task.FromResult(Failure(
                RecipientPackageReferenceErrorCodes.Forbidden,
                "Package reference access is forbidden.", 403));
        }

        ArgumentNullException.ThrowIfNull(query);
        return gateway.ListAsync(actor, query, cancellationToken);
    }

    private static SessionOperationResult<RecipientPackageReferencePageDto> Failure(
        string code, string message, int status) =>
        SessionOperationResult<RecipientPackageReferencePageDto>.Failure(code, message, status);
}
