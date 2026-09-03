namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportRequirementRuleRow
{
    public Guid Id { get; set; }

    public string RuleSetId { get; set; } = string.Empty;

    public int RuleSetVersion { get; set; }

    public string RuleSelector { get; set; } = string.Empty;

    public string? SelectorOperand { get; set; }

    public string RequirementType { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}
