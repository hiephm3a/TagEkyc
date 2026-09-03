namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportRecipientPackageDeliveryRow
{
    public Guid DeliveryId { get; set; }
    public Guid PackageId { get; set; }
    public Guid RecipientClientApplicationId { get; set; }
    public byte[] IdempotencyKeyDigest { get; set; } = [];
    public byte[] DeliveryEqualityFingerprint { get; set; } = [];
    public Guid C2PreparationId { get; set; }
    public byte[] PackageEqualityFingerprint { get; set; } = [];
    public string RecipientKeyId { get; set; } = string.Empty;
    public int RecipientKeyVersion { get; set; }
    public byte[] RecipientKeyFingerprint { get; set; } = [];
    public long RecipientKeyRevision { get; set; }
    public long PackageRevisionAtAuthorization { get; set; }
    public long EncryptedPackageLength { get; set; }
    public byte[] PackageCiphertextDigest { get; set; } = [];
    public byte[] EnvelopeDigest { get; set; } = [];
    public byte[] ObjectBindingDigest { get; set; } = [];
    public Guid CreatorApiKeyId { get; set; }
    public Guid CreatorPrincipalId { get; set; }
    public byte[] AuthorizationCorrelationDigest { get; set; } = [];
    public string State { get; set; } = string.Empty;
    public long Revision { get; set; }
    public DateTimeOffset AuthorizedAtUtc { get; set; }
    public DateTimeOffset AuthorizationExpiresAtUtc { get; set; }
    public int StreamAttemptCount { get; set; }
    public long DeliveryFence { get; set; }
    public DateTimeOffset? StreamStartedAtUtc { get; set; }
    public DateTimeOffset? StreamLeaseExpiresAtUtc { get; set; }
    public Guid? StreamApiKeyId { get; set; }
    public Guid? StreamPrincipalId { get; set; }
    public byte[]? StreamCorrelationDigest { get; set; }
    public string? InterruptionKind { get; set; }
    public byte[]? InterruptionEvidenceDigest { get; set; }
    public DateTimeOffset? InterruptedAtUtc { get; set; }
    public string? IntegrityFailureKind { get; set; }
    public byte[]? IntegrityEvidenceDigest { get; set; }
    public DateTimeOffset? IntegrityUnavailableAtUtc { get; set; }
    public DateTimeOffset? OutcomeUnknownAtUtc { get; set; }
    public DateTimeOffset? ServerStreamCompletedAtUtc { get; set; }
    public long? VerifiedByteCount { get; set; }
    public byte[]? VerifiedPackageCiphertextDigest { get; set; }
    public byte[]? DeliveryReceiptDigest { get; set; }
    public DateTimeOffset? ExpiredAtUtc { get; set; }
}
