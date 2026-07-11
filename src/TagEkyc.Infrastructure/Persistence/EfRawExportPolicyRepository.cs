using Microsoft.EntityFrameworkCore;
using TagEkyc.Application.Ports;
using TagEkyc.Domain;
using TagEkyc.Infrastructure.Persistence.Entities;

namespace TagEkyc.Infrastructure.Persistence;

public sealed class EfRawExportPolicyRepository(TagEkycDbContext db) : IRawExportPolicyRepository
{
    public async Task<RawExportPolicyVersion> AddVersionAsync(
        AddRawExportPolicyVersionCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.AllowedClasses.Count == 0)
        {
            throw new InvalidOperationException("RAW_EXPORT_POLICY_ALLOWED_CLASSES_EMPTY");
        }

        var ruleSet = await GetCurrentRuleSetAsync(cancellationToken);
        var rules = await GetRulesAsync(ruleSet.RuleSetId, ruleSet.RuleSetVersion, cancellationToken);
        if (rules.Count == 0)
        {
            throw new InvalidOperationException("RAW_EXPORT_REQUIREMENT_RULE_SET_EMPTY");
        }

        var latestVersion = await db.RawExportPolicyVersions
            .Where(row => row.PolicyId == command.PolicyId)
            .MaxAsync(row => (int?)row.PolicyVersion, cancellationToken) ?? 0;
        if (latestVersion != command.ExpectedLatestVersion)
        {
            throw new InvalidOperationException("RAW_EXPORT_POLICY_VERSION_CONFLICT");
        }

        var nextVersion = latestVersion + 1;
        var now = DateTimeOffset.UtcNow;
        var version = new RawExportPolicyVersionRow
        {
            PolicyId = command.PolicyId,
            PolicyVersion = nextVersion,
            Mode = command.Mode.ToString(),
            Purpose = RequireNonBlank(command.Purpose, nameof(command.Purpose)),
            RetentionProfileRef = Normalize(command.RetentionProfileRef),
            RetentionPurposeCode = Normalize(command.RetentionPurposeCode),
            ConsentRequirement = command.ConsentRequirement.ToString(),
            RecipientCategory = Normalize(command.RecipientCategory),
            RecipientAssuranceRequirement = Normalize(command.RecipientAssuranceRequirement),
            ControllerRole = Normalize(command.ControllerRole),
            ControllerEntityRef = Normalize(command.ControllerEntityRef),
            ControllerJurisdiction = NormalizeCountry(command.ControllerJurisdiction),
            RecipientJurisdiction = NormalizeCountry(command.RecipientJurisdiction),
            ProcessingInfrastructureJurisdiction = NormalizeCountry(command.ProcessingInfrastructureJurisdiction),
            TransferScenarioCode = Normalize(command.TransferScenarioCode),
            TransferLegalBasisCode = Normalize(command.TransferLegalBasisCode),
            RequirementRuleSetId = ruleSet.RuleSetId,
            RequirementRuleSetVersion = ruleSet.RuleSetVersion,
            CreatedAt = now,
        };
        var requirements = RawExportRequirementEvaluator.Derive(version, ruleSet, rules);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO tagekyc.raw_export_policy_versions
                ("PolicyId","PolicyVersion","Mode","Purpose","RetentionProfileRef","RetentionPurposeCode","ConsentRequirement",
                 "RecipientCategory","RecipientAssuranceRequirement","ControllerRole","ControllerEntityRef","ControllerJurisdiction",
                 "RecipientJurisdiction","ProcessingInfrastructureJurisdiction","TransferScenarioCode","TransferLegalBasisCode",
                 "RequirementRuleSetId","RequirementRuleSetVersion","CreatedAt")
            VALUES
                ({version.PolicyId},{version.PolicyVersion},{version.Mode},{version.Purpose},{version.RetentionProfileRef},{version.RetentionPurposeCode},{version.ConsentRequirement},
                 {version.RecipientCategory},{version.RecipientAssuranceRequirement},{version.ControllerRole},{version.ControllerEntityRef},{version.ControllerJurisdiction},
                 {version.RecipientJurisdiction},{version.ProcessingInfrastructureJurisdiction},{version.TransferScenarioCode},{version.TransferLegalBasisCode},
                 {version.RequirementRuleSetId},{version.RequirementRuleSetVersion},{version.CreatedAt});
            """, cancellationToken);

        foreach (var rawClass in command.AllowedClasses.OrderBy(rawClass => rawClass.ToString()))
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO tagekyc.raw_export_policy_allowed_classes
                    ("PolicyId","PolicyVersion","RawClass","CreatedAt")
                VALUES
                    ({command.PolicyId},{nextVersion},{rawClass.ToString()},{now});
                """, cancellationToken);
        }

        foreach (var requirement in requirements.OrderBy(requirement => requirement.ToString()))
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO tagekyc.raw_export_policy_requirements
                    ("PolicyId","PolicyVersion","RequirementType","CreatedAt")
                VALUES
                    ({command.PolicyId},{nextVersion},{requirement.ToString()},{now});
                """, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        return await GetVersionAsync(command.PolicyId, nextVersion, cancellationToken)
            ?? throw new InvalidOperationException("RAW_EXPORT_POLICY_VERSION_NOT_FOUND_AFTER_INSERT");
    }

    public Task<RawExportPolicyVersion> CatalogApproveAsync(
        CloseRawExportPolicyVersionCommand command,
        CancellationToken cancellationToken = default)
        => CloseAsync(command, RawExportPolicyClosureType.CatalogApproved, cancellationToken);

    public Task<RawExportPolicyVersion> AbandonDraftAsync(
        CloseRawExportPolicyVersionCommand command,
        CancellationToken cancellationToken = default)
        => CloseAsync(command, RawExportPolicyClosureType.Abandoned, cancellationToken);

    public async Task<RawExportPolicyVersion?> GetVersionAsync(
        Guid policyId,
        int policyVersion,
        CancellationToken cancellationToken = default)
    {
        var version = await db.RawExportPolicyVersions.AsNoTracking()
            .SingleOrDefaultAsync(row => row.PolicyId == policyId && row.PolicyVersion == policyVersion, cancellationToken);
        return version is null ? null : await ToDomainAsync(version, cancellationToken);
    }

    public async Task<RawExportPolicyVersion?> GetLatestVersionAsync(
        Guid policyId,
        CancellationToken cancellationToken = default)
    {
        var version = await db.RawExportPolicyVersions.AsNoTracking()
            .Where(row => row.PolicyId == policyId)
            .OrderByDescending(row => row.PolicyVersion)
            .FirstOrDefaultAsync(cancellationToken);
        return version is null ? null : await ToDomainAsync(version, cancellationToken);
    }

    public async Task<RawExportPolicyVersion?> GetLatestCatalogApprovedVersionAsync(
        Guid policyId,
        CancellationToken cancellationToken = default)
    {
        var version = await (
            from policy in db.RawExportPolicyVersions.AsNoTracking()
            join closure in db.RawExportPolicyClosures.AsNoTracking()
                on new { policy.PolicyId, policy.PolicyVersion } equals new { closure.PolicyId, closure.PolicyVersion }
            where policy.PolicyId == policyId && closure.ClosureType == RawExportPolicyClosureType.CatalogApproved.ToString()
            orderby policy.PolicyVersion descending
            select policy).FirstOrDefaultAsync(cancellationToken);
        return version is null ? null : await ToDomainAsync(version, cancellationToken);
    }

    public async Task<IReadOnlyList<RawExportPolicyVersion>> ListAsync(CancellationToken cancellationToken = default)
    {
        var versions = await db.RawExportPolicyVersions.AsNoTracking()
            .OrderBy(row => row.PolicyId)
            .ThenBy(row => row.PolicyVersion)
            .ToListAsync(cancellationToken);

        var result = new List<RawExportPolicyVersion>(versions.Count);
        foreach (var version in versions)
        {
            result.Add(await ToDomainAsync(version, cancellationToken));
        }

        return result;
    }

    private async Task<RawExportPolicyVersion> CloseAsync(
        CloseRawExportPolicyVersionCommand command,
        RawExportPolicyClosureType closureType,
        CancellationToken cancellationToken)
    {
        var closure = new RawExportPolicyClosureRow
        {
            PolicyId = command.PolicyId,
            PolicyVersion = command.PolicyVersion,
            ClosureType = closureType.ToString(),
            ClosedAtUtc = DateTimeOffset.UtcNow,
            ClosedByPrincipalId = RequireNonBlank(command.ClosedByPrincipalId, nameof(command.ClosedByPrincipalId)),
            DecisionRef = RequireNonBlank(command.DecisionRef, nameof(command.DecisionRef)),
        };

        db.RawExportPolicyClosures.Add(closure);
        await db.SaveChangesAsync(cancellationToken);
        return await GetVersionAsync(command.PolicyId, command.PolicyVersion, cancellationToken)
            ?? throw new InvalidOperationException("RAW_EXPORT_POLICY_VERSION_NOT_FOUND_AFTER_CLOSE");
    }

    private async Task<RawExportRequirementRuleSetRow> GetCurrentRuleSetAsync(CancellationToken cancellationToken)
    {
        var ruleSet = await db.RawExportRequirementRuleSets.AsNoTracking()
            .Where(row => row.RuleSetId == RawExportPolicyConstants.RequirementRuleSetId)
            .OrderByDescending(row => row.RuleSetVersion)
            .FirstOrDefaultAsync(cancellationToken);
        return ruleSet ?? throw new InvalidOperationException("RAW_EXPORT_REQUIREMENT_RULE_SET_MISSING");
    }

    private async Task<IReadOnlyList<RawExportRequirementRuleRow>> GetRulesAsync(
        string ruleSetId,
        int ruleSetVersion,
        CancellationToken cancellationToken)
        => await db.RawExportRequirementRules.AsNoTracking()
            .Where(row => row.RuleSetId == ruleSetId && row.RuleSetVersion == ruleSetVersion)
            .ToListAsync(cancellationToken);

    private async Task<RawExportPolicyVersion> ToDomainAsync(
        RawExportPolicyVersionRow version,
        CancellationToken cancellationToken)
    {
        var allowedClasses = await db.RawExportPolicyAllowedClasses.AsNoTracking()
            .Where(row => row.PolicyId == version.PolicyId && row.PolicyVersion == version.PolicyVersion)
            .Select(row => Enum.Parse<RawExportRawClass>(row.RawClass))
            .ToListAsync(cancellationToken);
        var requirements = await db.RawExportPolicyRequirements.AsNoTracking()
            .Where(row => row.PolicyId == version.PolicyId && row.PolicyVersion == version.PolicyVersion)
            .Select(row => Enum.Parse<RawExportRequirementType>(row.RequirementType))
            .ToListAsync(cancellationToken);
        var closure = await db.RawExportPolicyClosures.AsNoTracking()
            .SingleOrDefaultAsync(row => row.PolicyId == version.PolicyId && row.PolicyVersion == version.PolicyVersion, cancellationToken);

        var status = closure is null
            ? RawExportPolicyStatus.Draft
            : Enum.Parse<RawExportPolicyStatus>(closure.ClosureType);
        var domainClosure = closure is null
            ? null
            : new RawExportPolicyClosure(
                Enum.Parse<RawExportPolicyClosureType>(closure.ClosureType),
                closure.ClosedAtUtc,
                closure.ClosedByPrincipalId,
                closure.DecisionRef);

        return new RawExportPolicyVersion(
            version.PolicyId,
            version.PolicyVersion,
            Enum.Parse<RawExportMode>(version.Mode),
            version.Purpose,
            version.RetentionProfileRef,
            version.RetentionPurposeCode,
            Enum.Parse<RawExportConsentRequirement>(version.ConsentRequirement),
            version.RecipientCategory,
            version.RecipientAssuranceRequirement,
            version.ControllerRole,
            version.ControllerEntityRef,
            version.ControllerJurisdiction,
            version.RecipientJurisdiction,
            version.ProcessingInfrastructureJurisdiction,
            version.TransferScenarioCode,
            version.TransferLegalBasisCode,
            version.RequirementRuleSetId,
            version.RequirementRuleSetVersion,
            allowedClasses.ToHashSet(),
            requirements.ToHashSet(),
            status,
            domainClosure);
    }

    private static string RequireNonBlank(string value, string paramName)
        => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value must not be blank.", paramName) : value.Trim();

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeCountry(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
}

internal static class RawExportRequirementEvaluator
{
    public static IReadOnlySet<RawExportRequirementType> Derive(
        RawExportPolicyVersionRow version,
        RawExportRequirementRuleSetRow ruleSet,
        IReadOnlyCollection<RawExportRequirementRuleRow> rules)
    {
        var derived = new HashSet<RawExportRequirementType>();
        foreach (var rule in rules)
        {
            if (Applies(version, ruleSet, rule))
            {
                derived.Add(Enum.Parse<RawExportRequirementType>(rule.RequirementType));
            }
        }

        return derived;
    }

    private static bool Applies(
        RawExportPolicyVersionRow version,
        RawExportRequirementRuleSetRow ruleSet,
        RawExportRequirementRuleRow rule)
        => Enum.Parse<RawExportRuleSelector>(rule.RuleSelector) switch
        {
            RawExportRuleSelector.Always => true,
            RawExportRuleSelector.ModeEquals => string.Equals(version.Mode, rule.SelectorOperand, StringComparison.Ordinal),
            RawExportRuleSelector.ConsentRequired => version.ConsentRequirement == RawExportConsentRequirement.Required.ToString(),
            RawExportRuleSelector.AnyJurisdictionForeign => IsForeign(version.ControllerJurisdiction, ruleSet.HomeJurisdictionCode)
                || IsForeign(version.RecipientJurisdiction, ruleSet.HomeJurisdictionCode)
                || IsForeign(version.ProcessingInfrastructureJurisdiction, ruleSet.HomeJurisdictionCode),
            _ => false,
        };

    private static bool IsForeign(string? jurisdiction, string homeJurisdiction)
        => !string.IsNullOrWhiteSpace(jurisdiction) &&
            !string.Equals(jurisdiction, homeJurisdiction, StringComparison.Ordinal);
}
