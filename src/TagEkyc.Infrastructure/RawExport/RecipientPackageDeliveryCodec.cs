using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TagEkyc.Application.VerificationSessions;

namespace TagEkyc.Infrastructure.RawExport;

public static class RecipientPackageDeliveryCodec
{
    public static byte[] IdempotencyKeyDigest(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var bytes = Encoding.UTF8.GetBytes(value);
        try { return SHA256.HashData(bytes); }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }

    public static Guid DeliveryId(Guid recipientClientApplicationId, ReadOnlySpan<byte> idempotencyKeyDigest)
    {
        RequireGuid(recipientClientApplicationId, nameof(recipientClientApplicationId));
        RequireDigest(idempotencyKeyDigest, nameof(idempotencyKeyDigest));
        return EvidenceCanonicalization.DeterministicGuid(
            "tip-88c1-c3-delivery-id-v1",
            new
            {
                idempotencyKeyDigest = Hex(idempotencyKeyDigest),
                recipientClientApplicationId = recipientClientApplicationId.ToString("N"),
            });
    }

    public static byte[] EqualityFingerprint(
        Guid deliveryId, Guid packageId, Guid recipientClientApplicationId,
        ReadOnlySpan<byte> idempotencyKeyDigest, ReadOnlySpan<byte> packageEqualityFingerprint,
        Guid c2PreparationId, string recipientKeyId, int recipientKeyVersion,
        ReadOnlySpan<byte> recipientKeyFingerprint, long recipientKeyRevision,
        long encryptedPackageLength, ReadOnlySpan<byte> packageCiphertextDigest,
        ReadOnlySpan<byte> envelopeDigest) => Compute(
            "tip-88c1-c3-delivery-equality-v1",
            G(deliveryId), G(packageId), G(recipientClientApplicationId), H32(idempotencyKeyDigest),
            H32(packageEqualityFingerprint), G(c2PreparationId), Required(recipientKeyId),
            recipientKeyVersion.ToString(CultureInfo.InvariantCulture), H32(recipientKeyFingerprint),
            recipientKeyRevision.ToString(CultureInfo.InvariantCulture),
            encryptedPackageLength.ToString(CultureInfo.InvariantCulture), H32(packageCiphertextDigest), H32(envelopeDigest));

    public static byte[] ReceiptDigest(
        Guid deliveryId, Guid packageId, Guid recipientClientApplicationId,
        int deliveryAttemptNumber, long deliveryFence, ReadOnlySpan<byte> packageCiphertextDigest,
        long encryptedPackageLength, DateTimeOffset authorizedAtUtc, DateTimeOffset streamStartedAtUtc,
        DateTimeOffset serverStreamCompletedAtUtc, Guid authenticatedApiKeyId, Guid authenticatedPrincipalId) => Compute(
            "tip-88c1-c3-delivery-receipt-v1",
            G(deliveryId), G(packageId), G(recipientClientApplicationId),
            deliveryAttemptNumber.ToString(CultureInfo.InvariantCulture), deliveryFence.ToString(CultureInfo.InvariantCulture),
            H32(packageCiphertextDigest), encryptedPackageLength.ToString(CultureInfo.InvariantCulture),
            T(authorizedAtUtc), T(streamStartedAtUtc), T(serverStreamCompletedAtUtc),
            G(authenticatedApiKeyId), G(authenticatedPrincipalId));

    public static byte[] CorrelationDigest(string traceIdentifier) =>
        Compute("tip-88c1-c3-request-correlation-v1", Required(traceIdentifier).Normalize(NormalizationForm.FormC));

    public static Guid EventId(Guid deliveryId, long revision)
    {
        RequireGuid(deliveryId, nameof(deliveryId));
        if (revision < 1) throw new ArgumentOutOfRangeException(nameof(revision));
        return EvidenceCanonicalization.DeterministicGuid(
            "tip-88c1-c3-delivery-event-id-v1",
            new
            {
                deliveryId = deliveryId.ToString("N"),
                revision,
            });
    }

    public static byte[] AuthorizationEvidence(
        Guid deliveryId, Guid packageId, Guid recipientClientApplicationId,
        ReadOnlySpan<byte> deliveryEqualityFingerprint, Guid creatorApiKeyId,
        Guid creatorPrincipalId, ReadOnlySpan<byte> authorizationCorrelationDigest,
        DateTimeOffset authorizedAtUtc, DateTimeOffset authorizationExpiresAtUtc) => Compute(
            "tip-88c1-c3-delivery-authorization-v1",
            G(deliveryId), G(packageId), G(recipientClientApplicationId), H32(deliveryEqualityFingerprint),
            G(creatorApiKeyId), G(creatorPrincipalId), H32(authorizationCorrelationDigest),
            T(authorizedAtUtc), T(authorizationExpiresAtUtc));

    public static byte[] StreamAdmissionEvidence(
        Guid deliveryId, Guid packageId, Guid recipientClientApplicationId,
        int deliveryAttemptNumber, long deliveryFence, Guid streamApiKeyId,
        Guid streamPrincipalId, ReadOnlySpan<byte> streamCorrelationDigest,
        DateTimeOffset streamStartedAtUtc, DateTimeOffset streamLeaseExpiresAtUtc) => Compute(
            "tip-88c1-c3-delivery-stream-admission-v1",
            G(deliveryId), G(packageId), G(recipientClientApplicationId), I(deliveryAttemptNumber),
            I(deliveryFence), G(streamApiKeyId), G(streamPrincipalId), H32(streamCorrelationDigest),
            T(streamStartedAtUtc), T(streamLeaseExpiresAtUtc));

    public static byte[] FailureEvidence(
        string domain, Guid deliveryId, Guid packageId, Guid recipientClientApplicationId,
        int deliveryAttemptNumber, long deliveryFence, string failureKind,
        long? observedLength, ReadOnlySpan<byte> observedPackageDigest,
        bool observedPackageDigestPresent, ReadOnlySpan<byte> observedEnvelopeDigest,
        bool observedEnvelopeDigestPresent, ReadOnlySpan<byte> objectBindingDigest,
        DateTimeOffset occurredAtUtc) => Compute(
            domain,
            G(deliveryId), G(packageId), G(recipientClientApplicationId), I(deliveryAttemptNumber), I(deliveryFence),
            Required(failureKind), P(observedLength.HasValue), observedLength.HasValue ? I(observedLength.Value) : string.Empty,
            P(observedPackageDigestPresent), observedPackageDigestPresent ? H32(observedPackageDigest) : string.Empty,
            P(observedEnvelopeDigestPresent), observedEnvelopeDigestPresent ? H32(observedEnvelopeDigest) : string.Empty,
            H32(objectBindingDigest), T(occurredAtUtc));

    public static byte[] OutcomeUnknownEvidence(
        Guid deliveryId, Guid packageId, Guid recipientClientApplicationId,
        int deliveryAttemptNumber, long deliveryFence, Guid streamApiKeyId,
        Guid streamPrincipalId, ReadOnlySpan<byte> streamCorrelationDigest,
        DateTimeOffset streamStartedAtUtc, DateTimeOffset streamLeaseExpiresAtUtc,
        DateTimeOffset outcomeUnknownAtUtc) => Compute(
            "tip-88c1-c3-delivery-outcome-unknown-v1",
            G(deliveryId), G(packageId), G(recipientClientApplicationId), I(deliveryAttemptNumber), I(deliveryFence),
            G(streamApiKeyId), G(streamPrincipalId), H32(streamCorrelationDigest),
            T(streamStartedAtUtc), T(streamLeaseExpiresAtUtc), T(outcomeUnknownAtUtc),
            "LeaseExpiredAfterRestartOrLateCompletion");

    public static byte[] ExpiryEvidence(
        Guid deliveryId, Guid packageId, Guid recipientClientApplicationId,
        string predecessorState, Guid apiKeyId, Guid principalId,
        ReadOnlySpan<byte> correlationDigest, DateTimeOffset authorizationExpiresAtUtc,
        DateTimeOffset expiredAtUtc) => Compute(
            "tip-88c1-c3-delivery-expiry-v1",
            G(deliveryId), G(packageId), G(recipientClientApplicationId), Required(predecessorState),
            G(apiKeyId), G(principalId), H32(correlationDigest), T(authorizationExpiresAtUtc), T(expiredAtUtc));

    internal static byte[] EncodeFailureObservations(
        long? observedLength,
        ReadOnlySpan<byte> observedPackageDigest,
        bool observedPackageDigestPresent,
        ReadOnlySpan<byte> observedEnvelopeDigest,
        bool observedEnvelopeDigestPresent)
    {
        if (observedPackageDigestPresent) RequireDigest(observedPackageDigest, nameof(observedPackageDigest));
        if (observedEnvelopeDigestPresent) RequireDigest(observedEnvelopeDigest, nameof(observedEnvelopeDigest));
        return JsonSerializer.SerializeToUtf8Bytes(new
        {
            observedEnvelopeDigest = observedEnvelopeDigestPresent ? Hex(observedEnvelopeDigest) : string.Empty,
            observedEnvelopeDigestPresent,
            observedLength = observedLength?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            observedLengthPresent = observedLength.HasValue,
            observedPackageDigest = observedPackageDigestPresent ? Hex(observedPackageDigest) : string.Empty,
            observedPackageDigestPresent,
        });
    }

    internal static byte[] Compute(string domain, params string[] fields) =>
        C1HashCanonical.Compute(domain,
            fields.Select(field => (C1HashCanonical.Component)new C1HashCanonical.Scalar(field)).ToArray());

    internal static string Base64Url(ReadOnlySpan<byte> value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string T(DateTimeOffset value) => value.UtcDateTime.ToString(
        "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture);
    private static string I(long value) => value.ToString(CultureInfo.InvariantCulture);
    private static string P(bool value) => value ? "1" : "0";
    private static string H32(ReadOnlySpan<byte> value) { RequireDigest(value, nameof(value)); return Hex(value); }
    private static string Hex(ReadOnlySpan<byte> value) => Convert.ToHexString(value).ToLowerInvariant();
    private static string G(Guid value) { RequireGuid(value, nameof(value)); return value.ToString("N"); }
    private static string Required(string value) => !string.IsNullOrWhiteSpace(value) ? value : throw new ArgumentException("Value is required.");
    private static void RequireGuid(Guid value, string name) { if (value == Guid.Empty) throw new ArgumentException("GUID cannot be empty.", name); }
    private static void RequireDigest(ReadOnlySpan<byte> value, string name) { if (value.Length != 32) throw new ArgumentException("Digest must be 32 bytes.", name); }
}
