namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportRequirementRuleSetRow
{
    public string RuleSetId { get; set; } = string.Empty;

    public int RuleSetVersion { get; set; }

    public string MigrationRef { get; set; } = string.Empty;

    public string HomeJurisdictionCode { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}
