namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportRecipientKeyRegistrationRow
{
    public Guid RecipientClientApplicationId { get; set; }
    public string RecipientKeyId { get; set; } = string.Empty;
    public int RecipientKeyVersion { get; set; }
    public string PublicKeyAlgorithm { get; set; } = string.Empty;
    public byte[] PublicKeySpki { get; set; } = [];
    public byte[] PublicKeyFingerprint { get; set; } = [];
    public DateTimeOffset ValidFromUtc { get; set; }
    public DateTimeOffset ValidUntilUtc { get; set; }
    public string State { get; set; } = string.Empty;
    public long Revision { get; set; }
    public DateTimeOffset RegisteredAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public string? RevocationReason { get; set; }
}
