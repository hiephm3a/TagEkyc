namespace TagEkyc.Contracts.RawExport;

public sealed record RecipientPackageDeliveryDto(
    Guid DeliveryId,
    Guid PackageId,
    string State,
    DateTimeOffset AuthorizedAtUtc,
    DateTimeOffset AuthorizationExpiresAtUtc,
    int StreamAttemptCount,
    DateTimeOffset? ServerStreamCompletedAtUtc,
    long EncryptedPackageLength,
    string PackageCiphertextDigest,
    string? DeliveryReceiptDigest);

public static class RecipientPackageDeliveryErrorCodes
{
    public const string PrincipalRequired = "RAW_EXPORT_PACKAGE_DELIVERY_PRINCIPAL_REQUIRED";
    public const string Forbidden = "RAW_EXPORT_PACKAGE_DELIVERY_FORBIDDEN";
    public const string RequestInvalid = "RAW_EXPORT_PACKAGE_DELIVERY_REQUEST_INVALID";
    public const string IdempotencyConflict = "RAW_EXPORT_PACKAGE_DELIVERY_IDEMPOTENCY_CONFLICT";
    public const string NotFound = "RAW_EXPORT_PACKAGE_DELIVERY_NOT_FOUND";
    public const string Ineligible = "RAW_EXPORT_PACKAGE_DELIVERY_INELIGIBLE";
    public const string RangeNotSupported = "RAW_EXPORT_PACKAGE_RANGE_NOT_SUPPORTED";
    public const string InProgress = "RAW_EXPORT_PACKAGE_DELIVERY_IN_PROGRESS";
    public const string CapacityUnavailable = "RAW_EXPORT_PACKAGE_DELIVERY_CAPACITY_UNAVAILABLE";
    public const string Unavailable = "RAW_EXPORT_PACKAGE_DELIVERY_UNAVAILABLE";
    public const string IntegrityUnavailable = "RAW_EXPORT_PACKAGE_DELIVERY_INTEGRITY_UNAVAILABLE";
    public const string Expired = "RAW_EXPORT_PACKAGE_DELIVERY_EXPIRED";
    public const string OutcomeUnknown = "RAW_EXPORT_PACKAGE_DELIVERY_OUTCOME_UNKNOWN";
    public const string AlreadyCompleted = "RAW_EXPORT_PACKAGE_DELIVERY_ALREADY_COMPLETED";
}
