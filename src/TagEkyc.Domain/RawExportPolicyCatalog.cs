namespace TagEkyc.Domain;

public enum RawExportMode
{
    ExternalExportOnlyNoRetain = 0,
    EncryptedExportPacket = 1,
    EncryptedRawVaultRetained = 2,
}

public enum RawExportRawClass
{
    ChipDg1 = 0,
    ChipDg2Portrait = 1,
    ChipDg13 = 2,
    ChipDg15 = 3,
    ChipSod = 4,
    AaChallenge = 5,
    AaResponse = 6,
    LiveSelfieImage = 7,
    LivenessMedia = 8,
    HandSignatureImage = 9,
}

public enum RawExportConsentRequirement
{
    Required = 0,
    NotRequired = 1,
}

public enum RawExportRequirementType
{
    LegalApproval = 0,
    ConsentArtifact = 1,
    Dpia = 2,
    CrossBorderAssessment = 3,
    RetentionSchedule = 4,
}

public enum RawExportPolicyStatus
{
    Draft = 0,
    CatalogApproved = 1,
    Abandoned = 2,
}

public enum RawExportPolicyClosureType
{
    CatalogApproved = 0,
    Abandoned = 1,
}

public enum RawExportRuleSelector
{
    Always = 0,
    ModeEquals = 1,
    ConsentRequired = 2,
    AnyJurisdictionForeign = 3,
}

public sealed record RawExportPolicyVersion(
    Guid PolicyId,
    int PolicyVersion,
    RawExportMode Mode,
    string Purpose,
    string? RetentionProfileRef,
    string? RetentionPurposeCode,
    RawExportConsentRequirement ConsentRequirement,
    string? RecipientCategory,
    string? RecipientAssuranceRequirement,
    string? ControllerRole,
    string? ControllerEntityRef,
    string? ControllerJurisdiction,
    string? RecipientJurisdiction,
    string? ProcessingInfrastructureJurisdiction,
    string? TransferScenarioCode,
    string? TransferLegalBasisCode,
    string RequirementRuleSetId,
    int RequirementRuleSetVersion,
    IReadOnlySet<RawExportRawClass> AllowedClasses,
    IReadOnlySet<RawExportRequirementType> Requirements,
    RawExportPolicyStatus Status,
    RawExportPolicyClosure? Closure);

public sealed record RawExportPolicyClosure(
    RawExportPolicyClosureType ClosureType,
    DateTimeOffset ClosedAtUtc,
    string ClosedByPrincipalId,
    string DecisionRef);

public sealed record RawExportRequirementRuleSet(
    string RuleSetId,
    int RuleSetVersion,
    string MigrationRef,
    string HomeJurisdictionCode);

public sealed record RawExportRequirementRule(
    string RuleSetId,
    int RuleSetVersion,
    RawExportRuleSelector RuleSelector,
    string? SelectorOperand,
    RawExportRequirementType RequirementType);

public static class RawExportPolicyConstants
{
    public const string RequirementRuleSetId = "RAW_EXPORT_REQUIREMENTS";
}
