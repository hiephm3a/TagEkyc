namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class AppendIdempotencyRecordRow
{
    public Guid VerificationSessionId { get; set; }

    public string IdempotencyKey { get; set; } = string.Empty;

    public string EndpointKind { get; set; } = string.Empty;

    public string SubmissionSlot { get; set; } = string.Empty;

    public Guid MintedId { get; set; }

    public string Fingerprint { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}
