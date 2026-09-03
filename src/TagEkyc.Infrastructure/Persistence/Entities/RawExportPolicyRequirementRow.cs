namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportPolicyRequirementRow
{
    public Guid PolicyId { get; set; }

    public int PolicyVersion { get; set; }

    public string RequirementType { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}
