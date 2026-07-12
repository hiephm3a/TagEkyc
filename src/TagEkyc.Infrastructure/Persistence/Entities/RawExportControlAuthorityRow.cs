namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportControlAuthorityRow
{
    public Guid AuthorityEventId { get; set; }

    public Guid PrincipalId { get; set; }

    public string AuthorityType { get; set; } = string.Empty;

    public string ScopeType { get; set; } = string.Empty;

    public Guid? ScopeId { get; set; }

    public string? RequirementType { get; set; }

    public int Revision { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string DecisionRef { get; set; } = string.Empty;

    public Guid RecordedByPrincipalId { get; set; }

    public DateTimeOffset RecordedAtUtc { get; set; }
}
