namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportFulfillmentRow
{
    public Guid FulfillmentEventId { get; set; }

    public Guid PolicyId { get; set; }

    public int PolicyVersion { get; set; }

    public string RequirementType { get; set; } = string.Empty;

    public int Revision { get; set; }

    public string EventType { get; set; } = string.Empty;

    public int? SupersedesRevision { get; set; }

    public int? TargetRevision { get; set; }

    public string? ArtifactRef { get; set; }

    public string? ArtifactVersion { get; set; }

    public DateTimeOffset? ValidFromUtc { get; set; }

    public DateTimeOffset? ValidUntilUtc { get; set; }

    public Guid RecordedByPrincipalId { get; set; }

    public string FulfillmentDecisionRef { get; set; } = string.Empty;

    public DateTimeOffset RecordedAtUtc { get; set; }
}
