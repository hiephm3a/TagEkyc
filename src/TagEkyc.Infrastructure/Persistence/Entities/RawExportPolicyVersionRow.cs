namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportPolicyVersionRow
{
    public Guid PolicyId { get; set; }

    public int PolicyVersion { get; set; }

    public string Mode { get; set; } = string.Empty;

    public string Purpose { get; set; } = string.Empty;

    public string? RetentionProfileRef { get; set; }

    public string? RetentionPurposeCode { get; set; }

    public string ConsentRequirement { get; set; } = string.Empty;

    public string? RecipientCategory { get; set; }

    public string? RecipientAssuranceRequirement { get; set; }

    public string? ControllerRole { get; set; }

    public string? ControllerEntityRef { get; set; }

    public string? ControllerJurisdiction { get; set; }

    public string? RecipientJurisdiction { get; set; }

    public string? ProcessingInfrastructureJurisdiction { get; set; }

    public string? TransferScenarioCode { get; set; }

    public string? TransferLegalBasisCode { get; set; }

    public string RequirementRuleSetId { get; set; } = string.Empty;

    public int RequirementRuleSetVersion { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
