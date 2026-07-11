namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportPolicyClosureRow
{
    public Guid PolicyId { get; set; }

    public int PolicyVersion { get; set; }

    public string ClosureType { get; set; } = string.Empty;

    public DateTimeOffset ClosedAtUtc { get; set; }

    public string ClosedByPrincipalId { get; set; } = string.Empty;

    public string DecisionRef { get; set; } = string.Empty;
}
