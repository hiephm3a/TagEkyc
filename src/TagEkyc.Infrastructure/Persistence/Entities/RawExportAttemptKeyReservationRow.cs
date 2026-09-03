namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportAttemptKeyReservationRow
{
    public Guid AttemptKeyReservationId { get; set; }
    public Guid AttemptId { get; set; }
    public byte[] EncryptionAttemptFingerprint { get; set; } = [];
    public string KeyProviderId { get; set; } = string.Empty;
    public string KekId { get; set; } = string.Empty;
    public int KekVersion { get; set; }
    public string KekFingerprint { get; set; } = string.Empty;
    public byte[] AttemptKeyContextFingerprint { get; set; } = [];
    public string WrappingSuiteId { get; set; } = string.Empty;
    public int WrappingSuiteVersion { get; set; }
    public string PreparationDisposition { get; set; } = string.Empty;
    public Guid? CurrentPreparationId { get; set; }
    public long CurrentPreparationFence { get; set; }
    public DateTimeOffset? CurrentPreparationLeaseExpiresAtUtc { get; set; }
    public string? CurrentProviderOperationToken { get; set; }
    public long ResolutionAttemptCount { get; set; }
    public DateTimeOffset? NextResolutionAttemptNotBeforeUtc { get; set; }
    public DateTimeOffset? ResolutionDeadlineUtc { get; set; }
    public long CleanupAttemptCount { get; set; }
    public DateTimeOffset? NextCleanupAttemptNotBeforeUtc { get; set; }
    public DateTimeOffset? CleanupDeadlineUtc { get; set; }
    public bool CleanupOperatorInterventionRequired { get; set; }
    public byte[]? WrappedDekCiphertext { get; set; }
    public byte[]? WrappedDekNonce { get; set; }
    public byte[]? WrappedDekTag { get; set; }
    public byte[]? WrappedDekMetadataDigest { get; set; }
    public long RowRevision { get; set; }
    public DateTimeOffset? PreparedAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public string? RevocationReasonCode { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
