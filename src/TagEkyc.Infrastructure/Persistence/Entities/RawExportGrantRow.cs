namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportGrantRow
{
    public Guid PrincipalId { get; set; }

    public Guid PolicyId { get; set; }

    public int PolicyVersion { get; set; }

    public int Revision { get; set; }

    public string EventType { get; set; } = string.Empty;

    public Guid? ClientApplicationId { get; set; }

    public string DecisionRef { get; set; } = string.Empty;

    public Guid RecordedByPrincipalId { get; set; }

    public DateTimeOffset RecordedAtUtc { get; set; }
}
