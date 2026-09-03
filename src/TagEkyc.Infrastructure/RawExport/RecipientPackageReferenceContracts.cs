using Npgsql;

namespace TagEkyc.Infrastructure.RawExport;

internal interface IRecipientPackageReferenceConnectionFactory
{
    Task<NpgsqlConnection> OpenAsync(CancellationToken cancellationToken);
}

internal sealed record RecipientPackageReferenceRow(Guid PackageId, DateTimeOffset FinalizedAtUtc);

public sealed record RecipientPackageReferenceCursor(
    string KeyId,
    int KeyVersion,
    Guid ClientApplicationId,
    Guid PrincipalId,
    int PageSize,
    DateTimeOffset BoundaryFinalizedAtUtc,
    Guid BoundaryPackageId,
    long IssuedAtUnixSeconds,
    long ExpiresAtUnixSeconds);

public enum RecipientPackageReferenceCursorParseOutcome
{
    Parsed,
    Invalid,
}

public sealed record RecipientPackageReferenceCursorParseResult(
    RecipientPackageReferenceCursorParseOutcome Outcome,
    RecipientPackageReferenceCursor? Cursor,
    byte[]? Payload,
    byte[]? Mac);
