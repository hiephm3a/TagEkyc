namespace TagEkyc.Contracts.RawExport;

public sealed record RecipientPackageReferenceDto(
    Guid PackageId,
    DateTimeOffset FinalizedAtUtc);

public sealed record RecipientPackageReferencePageDto(
    IReadOnlyList<RecipientPackageReferenceDto> Items,
    string? NextCursor);

public static class RecipientPackageReferenceErrorCodes
{
    public const string Forbidden = "RAW_EXPORT_PACKAGE_REFERENCE_FORBIDDEN";
    public const string RequestInvalid = "RAW_EXPORT_PACKAGE_REFERENCE_REQUEST_INVALID";
    public const string CursorInvalid = "RAW_EXPORT_PACKAGE_REFERENCE_CURSOR_INVALID";
    public const string Unavailable = "RAW_EXPORT_PACKAGE_REFERENCE_UNAVAILABLE";
}
