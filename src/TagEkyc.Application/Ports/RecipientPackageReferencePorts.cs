using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.RawExport;

namespace TagEkyc.Application.Ports;

public sealed record RecipientPackageReferenceRawQuery(
    IReadOnlyList<string> Keys,
    IReadOnlyList<string> PageSizeValues,
    IReadOnlyList<string> CursorValues);

public interface IRecipientPackageReferenceGateway
{
    Task<SessionOperationResult<RecipientPackageReferencePageDto>> ListAsync(
        AuthenticatedClientContext actor,
        RecipientPackageReferenceRawQuery query,
        CancellationToken cancellationToken);
}

public interface IRecipientPackageReferenceApplicationService
{
    Task<SessionOperationResult<RecipientPackageReferencePageDto>> ListAsync(
        AuthenticatedClientContext actor,
        RecipientPackageReferenceRawQuery query,
        CancellationToken cancellationToken);
}
