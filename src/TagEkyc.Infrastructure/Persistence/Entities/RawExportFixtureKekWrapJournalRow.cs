namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportFixtureKekWrapJournalRow
{
    public Guid FixtureWrapId { get; set; }
    public string KeyProviderId { get; set; } = string.Empty;
    public string ProviderOperationToken { get; set; } = string.Empty;
    public byte[] AttemptKeyContextFingerprint { get; set; } = [];
    public string WrappingSuiteId { get; set; } = string.Empty;
    public int WrappingSuiteVersion { get; set; }
    public byte[] WrappedDekCiphertext { get; set; } = [];
    public byte[] WrappedDekNonce { get; set; } = [];
    public byte[] WrappedDekTag { get; set; } = [];
    public string ProviderResourceReference { get; set; } = string.Empty;
    public string ProviderOperationReceipt { get; set; } = string.Empty;
    public byte[] WrappedDekMetadataDigest { get; set; } = [];
    public DateTimeOffset CreatedAtUtc { get; set; }
}
