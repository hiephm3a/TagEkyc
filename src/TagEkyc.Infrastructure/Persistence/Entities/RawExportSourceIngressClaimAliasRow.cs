namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportSourceIngressClaimAliasRow
{
    public Guid IngressClaimAliasId { get; set; }
    public Guid ClientApplicationId { get; set; }
    public string ProducerId { get; set; } = string.Empty;
    public string CaptureAgentInstanceId { get; set; } = string.Empty;
    public Guid IngressIdempotencyKey { get; set; }
    public byte[] AttemptedIngressIdentityFingerprint { get; set; } = [];
    public byte[] ProducerClaimEnvelopeFingerprint { get; set; } = [];
    public string AliasState { get; set; } = string.Empty;
    public Guid? IngressClaimId { get; set; }
    public Guid CurrentClaimEvaluationId { get; set; }
    public Guid CurrentClaimEvaluationOwnerId { get; set; }
    public string CurrentClaimEvaluationDisposition { get; set; } = string.Empty;
    public DateTimeOffset CurrentTokenIssuedAtUtc { get; set; }
    public DateTimeOffset CurrentTokenExpiresAtUtc { get; set; }
    public int CurrentTokenSchemaVersion { get; set; }
    public string CurrentTokenVariant { get; set; } = string.Empty;
    public string CurrentTokenAudience { get; set; } = string.Empty;
    public byte[] CurrentTokenDigest { get; set; } = [];
    public long CurrentClaimEvaluationRevision { get; set; }
    public long CurrentClaimEvaluationFence { get; set; }
    public DateTimeOffset LatestIssuedTokenExpiresAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
