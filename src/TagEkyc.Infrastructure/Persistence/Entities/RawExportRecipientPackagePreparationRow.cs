namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportRecipientPackagePreparationRow
{
    public Guid C2PreparationId { get; set; }
    public Guid PackageId { get; set; }
    public byte[] PackageEqualityFingerprint { get; set; } = [];
    public Guid AssemblyId { get; set; }
    public Guid JobId { get; set; }
    public Guid AttemptId { get; set; }
    public long FencingToken { get; set; }
    public byte[] AssemblyFingerprint { get; set; } = [];
    public byte[] ManifestDigest { get; set; } = [];
    public byte[] AssemblyDigest { get; set; } = [];
    public byte[] AssemblyAuthenticationValue { get; set; } = [];
    public long CompleteAssemblyLength { get; set; }
    public Guid RecipientClientApplicationId { get; set; }
    public string RecipientKeyId { get; set; } = string.Empty;
    public int RecipientKeyVersion { get; set; }
    public byte[] RecipientKeyFingerprint { get; set; } = [];
    public byte[] RecipientPublicKeySpki { get; set; } = [];
    public long RecipientKeyRevision { get; set; }
    public DateTimeOffset RecipientKeyValidFromUtc { get; set; }
    public DateTimeOffset RecipientKeyValidUntilUtc { get; set; }
    public string PackageProfile { get; set; } = string.Empty;
    public byte[] ProviderOperationTokenDigest { get; set; } = [];
    public string ProviderKind { get; set; } = string.Empty;
    public string ProviderConfigurationId { get; set; } = string.Empty;
    public byte[] ProviderEndpointFingerprint { get; set; } = [];
    public string BucketName { get; set; } = string.Empty;
    public string ObjectKey { get; set; } = string.Empty;
    public byte[] ObjectBindingDigest { get; set; } = [];
    public byte[]? EnvelopeDigest { get; set; }
    public long? EncryptedPackageLength { get; set; }
    public byte[]? PackageCiphertextDigest { get; set; }
    public byte[]? ConditionalCreateEvidenceDigest { get; set; }
    public byte[]? ProviderReceiptDigest { get; set; }
    public byte[]? AbortAuthorizationDigest { get; set; }
    public byte[]? PositiveAbsenceEvidenceDigest { get; set; }
    public byte[]? CleanupProgressEvidenceDigest { get; set; }
    public byte[]? QuarantineEvidenceDigest { get; set; }
    public string State { get; set; } = string.Empty;
    public long Revision { get; set; }
    public DateTimeOffset SnapshotFrozenAtUtc { get; set; }
    public DateTimeOffset? PutStartedAtUtc { get; set; }
    public DateTimeOffset? PreparedAtUtc { get; set; }
    public DateTimeOffset? FinalizedAtUtc { get; set; }
    public DateTimeOffset? AbortAuthorizedAtUtc { get; set; }
    public DateTimeOffset? CleanupPendingAtUtc { get; set; }
    public DateTimeOffset? AbortedAtUtc { get; set; }
    public DateTimeOffset? QuarantinedAtUtc { get; set; }
}
