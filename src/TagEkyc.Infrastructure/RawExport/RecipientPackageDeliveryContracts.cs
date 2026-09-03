using Npgsql;
using TagEkyc.Contracts.RawExport;

namespace TagEkyc.Infrastructure.RawExport;

internal interface IRecipientPackageDeliveryConnectionFactory
{
    Task<NpgsqlConnection> OpenAsync(CancellationToken cancellationToken);
}

internal sealed record RecipientPackageDeliveryLocator(
    string ProviderConfigurationId,
    Uri ServiceUrl,
    string RegionIdentifier,
    bool ForcePathStyle,
    string BucketName,
    string ObjectKey,
    byte[] ObjectBindingDigest);

internal enum RecipientPackageDeliveryReadOutcome
{
    Opened,
    PositivelyAbsent,
    BucketUnavailable,
    Forbidden,
    Unavailable,
}

internal sealed record RecipientPackageDeliveryReadResult(
    RecipientPackageDeliveryReadOutcome Outcome,
    Stream? Content);

internal interface IRecipientPackageDeliveryReader
{
    Task<RecipientPackageDeliveryReadResult> OpenExactAsync(
        RecipientPackageDeliveryLocator locator,
        CancellationToken cancellationToken);
}

internal sealed record RecipientPackageDeliveryProjection(
    Guid DeliveryId,
    Guid PackageId,
    Guid RecipientClientApplicationId,
    string State,
    long Revision,
    DateTimeOffset AuthorizedAtUtc,
    DateTimeOffset AuthorizationExpiresAtUtc,
    int StreamAttemptCount,
    long DeliveryFence,
    DateTimeOffset? StreamStartedAtUtc,
    DateTimeOffset? StreamLeaseExpiresAtUtc,
    DateTimeOffset? ServerStreamCompletedAtUtc,
    long EncryptedPackageLength,
    byte[] PackageCiphertextDigest,
    byte[] EnvelopeDigest,
    byte[] ObjectBindingDigest,
    string ProviderConfigurationId,
    string BucketName,
    string ObjectKey,
    byte[]? DeliveryReceiptDigest);

internal sealed record RecipientPackageDeliveryMutation(
    string Outcome,
    RecipientPackageDeliveryProjection? Delivery);

internal static class RecipientPackageDeliveryErrors
{
    internal static readonly IReadOnlyDictionary<string, (string Code, string Message, int Status)> ByOutcome =
        new Dictionary<string, (string, string, int)>(StringComparer.Ordinal)
        {
            ["IdempotencyConflict"] = (RecipientPackageDeliveryErrorCodes.IdempotencyConflict, "The idempotency key is already bound to another package.", 409),
            ["NotFound"] = (RecipientPackageDeliveryErrorCodes.NotFound, "Package delivery was not found.", 404),
            ["Ineligible"] = (RecipientPackageDeliveryErrorCodes.Ineligible, "The package is not eligible for delivery.", 409),
            ["Unavailable"] = (RecipientPackageDeliveryErrorCodes.Unavailable, "Package delivery is temporarily unavailable.", 503),
            ["InProgress"] = (RecipientPackageDeliveryErrorCodes.InProgress, "Package delivery is already in progress.", 409),
            ["IntegrityUnavailable"] = (RecipientPackageDeliveryErrorCodes.IntegrityUnavailable, "Verified package content is unavailable.", 503),
            ["Expired"] = (RecipientPackageDeliveryErrorCodes.Expired, "Package delivery authorization has expired.", 410),
            ["OutcomeUnknown"] = (RecipientPackageDeliveryErrorCodes.OutcomeUnknown, "Package delivery outcome is unknown.", 410),
            ["Completed"] = (RecipientPackageDeliveryErrorCodes.AlreadyCompleted, "Package delivery is already complete.", 410),
            ["Terminal"] = (RecipientPackageDeliveryErrorCodes.AlreadyCompleted, "Package delivery is already complete.", 410),
            ["StateConflict"] = (RecipientPackageDeliveryErrorCodes.Unavailable, "Package delivery is temporarily unavailable.", 503),
        };
}
