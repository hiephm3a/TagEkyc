namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportSubjectConsentAuthorityRow
{
    public Guid AuthorityEventId { get; set; }

    public Guid AuthorityPrincipalId { get; set; }

    public Guid ClientApplicationId { get; set; }

    public string AuthorityType { get; set; } = string.Empty;

    public int Revision { get; set; }

    public string EventType { get; set; } = string.Empty;

    public int? TargetRevision { get; set; }

    public DateTimeOffset? ValidFromUtc { get; set; }

    public DateTimeOffset? ValidUntilUtc { get; set; }

    public string? DecisionRef { get; set; }

    public Guid RecordedByPrincipalId { get; set; }

    public DateTimeOffset RecordedAtUtc { get; set; }
}
