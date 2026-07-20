using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using TagEkyc.Application.Ports;
using TagEkyc.Domain;

namespace TagEkyc.Infrastructure.Persistence;

public sealed class EfRawExportControlPlaneRepository(TagEkycDbContext db) : IRawExportControlPlaneRepository
{
    private static readonly RawExportRequirementType[] PolicyScopedRequirements =
    [
        RawExportRequirementType.LegalApproval,
        RawExportRequirementType.Dpia,
        RawExportRequirementType.CrossBorderAssessment,
        RawExportRequirementType.RetentionSchedule,
    ];

    public Task<int> GrantExportPolicyAsync(RawExportGrantCommand command, CancellationToken cancellationToken = default) =>
        AppendGrantAsync(command, RawExportGrantEventType.Granted, cancellationToken);

    public Task<int> RevokeExportPolicyGrantAsync(RawExportGrantCommand command, CancellationToken cancellationToken = default) =>
        AppendGrantAsync(command, RawExportGrantEventType.Revoked, cancellationToken);

    public Task<int> GrantControlAuthorityAsync(RawExportAuthorityCommand command, CancellationToken cancellationToken = default) =>
        AppendAuthorityAsync(command, RawExportAuthorityEventType.Granted, cancellationToken);

    public Task<int> RevokeControlAuthorityAsync(RawExportAuthorityCommand command, CancellationToken cancellationToken = default) =>
        AppendAuthorityAsync(command, RawExportAuthorityEventType.Revoked, cancellationToken);

    public async Task<int> AcceptFulfillmentAsync(
        RawExportFulfillmentAcceptCommand command,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await BeginCommandTransactionAsync(command.ActorPrincipalId, cancellationToken);
        var revision = await ScalarIntAsync(
            """
            SELECT tagekyc.raw_export_append_fulfillment(
                @policyId,@policyVersion,@requirementType,@expectedRevision,'Accepted',
                @supersedesRevision,NULL,@artifactRef,@artifactVersion,@validFromUtc,@validUntilUtc,@decisionRef);
            """,
            cancellationToken,
            Parameter("policyId", command.PolicyId),
            Parameter("policyVersion", command.PolicyVersion),
            Parameter("requirementType", command.RequirementType.ToString()),
            Parameter("expectedRevision", command.ExpectedRevision),
            Parameter("supersedesRevision", command.SupersedesRevision),
            Parameter("artifactRef", command.ArtifactRef),
            Parameter("artifactVersion", command.ArtifactVersion),
            Parameter("validFromUtc", command.ValidFromUtc),
            Parameter("validUntilUtc", command.ValidUntilUtc),
            Parameter("decisionRef", command.DecisionRef));
        await transaction.CommitAsync(cancellationToken);
        return revision;
    }

    public async Task<int> WithdrawFulfillmentAsync(
        RawExportFulfillmentWithdrawCommand command,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await BeginCommandTransactionAsync(command.ActorPrincipalId, cancellationToken);
        var revision = await ScalarIntAsync(
            """
            SELECT tagekyc.raw_export_append_fulfillment(
                @policyId,@policyVersion,@requirementType,@expectedRevision,'Withdrawn',
                NULL,@targetRevision,NULL,NULL,NULL,NULL,@decisionRef);
            """,
            cancellationToken,
            Parameter("policyId", command.PolicyId),
            Parameter("policyVersion", command.PolicyVersion),
            Parameter("requirementType", command.RequirementType.ToString()),
            Parameter("expectedRevision", command.ExpectedRevision),
            Parameter("targetRevision", command.TargetRevision),
            Parameter("decisionRef", command.DecisionRef));
        await transaction.CommitAsync(cancellationToken);
        return revision;
    }

    public Task<int> ActivatePolicyAsync(RawExportLifecycleCommand command, CancellationToken cancellationToken = default) =>
        AppendLifecycleAsync(command, RawExportLifecycleEventType.Activated, cancellationToken);

    public Task<int> SuspendPolicyAsync(RawExportLifecycleCommand command, CancellationToken cancellationToken = default) =>
        AppendLifecycleAsync(command, RawExportLifecycleEventType.Suspended, cancellationToken);

    public Task<int> RevokePolicyAsync(RawExportLifecycleCommand command, CancellationToken cancellationToken = default) =>
        AppendLifecycleAsync(command, RawExportLifecycleEventType.Revoked, cancellationToken);

    public async Task<RawExportEligibilitySnapshot> ResolveExportEligibilityForAuthorizationAsync(
        Guid principalId,
        Guid policyId,
        int policyVersion,
        CancellationToken cancellationToken = default)
    {
        if (db.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("RAW_EXPORT_AUTHORIZATION_REQUIRES_AMBIENT_TRANSACTION");
        }

        await TakeSharedLockAsync(RawExportControlPlaneConstants.RuleSetPublishLockKey, cancellationToken);
        await TakeSharedLockAsync($"tip88b1:grant:{principalId}:{policyId}:{policyVersion}", cancellationToken);
        await TakeSharedLockAsync($"tip88b1:lifecycle:{policyId}:{policyVersion}", cancellationToken);
        foreach (var requirement in PolicyScopedRequirements.OrderBy(requirement => requirement.ToString()))
        {
            await TakeSharedLockAsync($"tip88b1:fulfillment:{policyId}:{policyVersion}:{requirement}", cancellationToken);
        }

        var evaluatedAt = await ScalarDateTimeOffsetAsync("SELECT transaction_timestamp();", cancellationToken);
        var policy = await db.RawExportPolicyVersions.AsNoTracking()
            .SingleOrDefaultAsync(row => row.PolicyId == policyId && row.PolicyVersion == policyVersion, cancellationToken);
        var currentRuleSetVersion = await db.RawExportRequirementRuleSets.AsNoTracking()
            .Where(row => row.RuleSetId == RawExportPolicyConstants.RequirementRuleSetId)
            .MaxAsync(row => (int?)row.RuleSetVersion, cancellationToken) ?? 0;
        var closure = await db.RawExportPolicyClosures.AsNoTracking()
            .SingleOrDefaultAsync(row => row.PolicyId == policyId && row.PolicyVersion == policyVersion, cancellationToken);
        var grant = await db.RawExportGrants.AsNoTracking()
            .Where(row => row.PrincipalId == principalId && row.PolicyId == policyId && row.PolicyVersion == policyVersion)
            .OrderByDescending(row => row.Revision)
            .FirstOrDefaultAsync(cancellationToken);
        var lifecycle = await db.RawExportPolicyLifecycles.AsNoTracking()
            .Where(row => row.PolicyId == policyId && row.PolicyVersion == policyVersion)
            .OrderByDescending(row => row.Revision)
            .FirstOrDefaultAsync(cancellationToken);
        var declaredRequirements = await db.RawExportPolicyRequirements.AsNoTracking()
            .Where(row => row.PolicyId == policyId &&
                          row.PolicyVersion == policyVersion &&
                          row.RequirementType != RawExportRequirementType.ConsentArtifact.ToString())
            .Select(row => row.RequirementType)
            .ToListAsync(cancellationToken);
        var fulfillmentRefs = new List<RawExportFulfillmentRef>();
        var missingFulfillment = false;
        foreach (var requirement in declaredRequirements.Order(StringComparer.Ordinal))
        {
            var latest = await db.RawExportFulfillments.AsNoTracking()
                .Where(row => row.PolicyId == policyId && row.PolicyVersion == policyVersion && row.RequirementType == requirement)
                .OrderByDescending(row => row.Revision)
                .FirstOrDefaultAsync(cancellationToken);
            if (latest is null ||
                latest.EventType != RawExportFulfillmentEventType.Accepted.ToString() ||
                latest.ValidFromUtc > evaluatedAt ||
                (latest.ValidUntilUtc is not null && evaluatedAt >= latest.ValidUntilUtc))
            {
                missingFulfillment = true;
                continue;
            }

            fulfillmentRefs.Add(new RawExportFulfillmentRef(
                Enum.Parse<RawExportRequirementType>(latest.RequirementType),
                latest.FulfillmentEventId,
                latest.Revision,
                latest.ArtifactRef ?? string.Empty,
                latest.ArtifactVersion ?? string.Empty,
                latest.ValidUntilUtc));
        }

        var causes = new List<RawExportEligibilityCause>();
        if (closure?.ClosureType == RawExportPolicyClosureType.Abandoned.ToString())
        {
            causes.Add(RawExportEligibilityCause.Abandoned);
        }
        else if (policy is null || closure?.ClosureType != RawExportPolicyClosureType.CatalogApproved.ToString())
        {
            causes.Add(RawExportEligibilityCause.NotCatalogApproved);
        }

        if (lifecycle?.EventType == RawExportLifecycleEventType.Revoked.ToString())
        {
            causes.Add(RawExportEligibilityCause.Revoked);
        }
        else if (lifecycle?.EventType == RawExportLifecycleEventType.Suspended.ToString())
        {
            causes.Add(RawExportEligibilityCause.Suspended);
        }
        else if (lifecycle?.EventType != RawExportLifecycleEventType.Activated.ToString())
        {
            causes.Add(RawExportEligibilityCause.NotActivated);
        }

        if (grant?.EventType != RawExportGrantEventType.Granted.ToString())
        {
            causes.Add(RawExportEligibilityCause.NoGrant);
        }

        var boundRuleSetVersion = policy?.RequirementRuleSetVersion ?? 0;
        if (policy is not null && boundRuleSetVersion != currentRuleSetVersion)
        {
            causes.Add(RawExportEligibilityCause.StaleRuleSet);
        }

        if (missingFulfillment)
        {
            causes.Add(RawExportEligibilityCause.MissingOrInvalidFulfillment);
        }

        var orderedCauses = causes
            .Distinct()
            .OrderBy(cause => (int)cause)
            .ToArray();
        var snapshot = new RawExportEligibilitySnapshot(
            orderedCauses.Length == 0 ? RawExportEligibilityState.Active : RawExportEligibilityState.Inactive,
            orderedCauses.Length == 0 ? null : orderedCauses[0],
            orderedCauses,
            evaluatedAt,
            boundRuleSetVersion,
            currentRuleSetVersion,
            grant is null ? null : new RawExportGrantRef(grant.PrincipalId, grant.PolicyId, grant.PolicyVersion, grant.Revision),
            lifecycle is null ? null : new RawExportLifecycleRef(lifecycle.PolicyId, lifecycle.PolicyVersion, lifecycle.Revision),
            fulfillmentRefs);

        return snapshot;
    }

    private async Task<int> AppendGrantAsync(
        RawExportGrantCommand command,
        RawExportGrantEventType eventType,
        CancellationToken cancellationToken)
    {
        await using var transaction = await BeginCommandTransactionAsync(command.ActorPrincipalId, cancellationToken);
        var revision = await ScalarIntAsync(
            "SELECT tagekyc.raw_export_append_grant(@principalId,@policyId,@policyVersion,@expectedRevision,@eventType,@clientApplicationId,@decisionRef);",
            cancellationToken,
            Parameter("principalId", command.PrincipalId),
            Parameter("policyId", command.PolicyId),
            Parameter("policyVersion", command.PolicyVersion),
            Parameter("expectedRevision", command.ExpectedRevision),
            Parameter("eventType", eventType.ToString()),
            Parameter("clientApplicationId", command.ClientApplicationId),
            Parameter("decisionRef", command.DecisionRef));
        await transaction.CommitAsync(cancellationToken);
        return revision;
    }

    private async Task<int> AppendAuthorityAsync(
        RawExportAuthorityCommand command,
        RawExportAuthorityEventType eventType,
        CancellationToken cancellationToken)
    {
        await using var transaction = await BeginCommandTransactionAsync(command.ActorPrincipalId, cancellationToken);
        var revision = await ScalarIntAsync(
            """
            SELECT tagekyc.raw_export_append_control_authority(
                @principalId,@authorityType,@scopeType,@scopeId,@requirementType,@expectedRevision,@eventType,@decisionRef);
            """,
            cancellationToken,
            Parameter("principalId", command.PrincipalId),
            Parameter("authorityType", command.AuthorityType.ToString()),
            Parameter("scopeType", command.ScopeType.ToString()),
            Parameter("scopeId", command.ScopeId),
            Parameter("requirementType", command.RequirementType?.ToString()),
            Parameter("expectedRevision", command.ExpectedRevision),
            Parameter("eventType", eventType.ToString()),
            Parameter("decisionRef", command.DecisionRef));
        await transaction.CommitAsync(cancellationToken);
        return revision;
    }

    private async Task<int> AppendLifecycleAsync(
        RawExportLifecycleCommand command,
        RawExportLifecycleEventType eventType,
        CancellationToken cancellationToken)
    {
        await using var transaction = await BeginCommandTransactionAsync(command.ActorPrincipalId, cancellationToken);
        var revision = await ScalarIntAsync(
            "SELECT tagekyc.raw_export_append_lifecycle(@policyId,@policyVersion,@expectedRevision,@eventType,@decisionRef);",
            cancellationToken,
            Parameter("policyId", command.PolicyId),
            Parameter("policyVersion", command.PolicyVersion),
            Parameter("expectedRevision", command.ExpectedRevision),
            Parameter("eventType", eventType.ToString()),
            Parameter("decisionRef", command.DecisionRef));
        await transaction.CommitAsync(cancellationToken);
        return revision;
    }

    private async Task<IDbContextTransaction> BeginCommandTransactionAsync(
        Guid actorPrincipalId,
        CancellationToken cancellationToken)
    {
        if (actorPrincipalId == Guid.Empty)
        {
            throw new InvalidOperationException("RAW_EXPORT_ACTOR_CONTEXT_INVALID");
        }

        var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
#pragma warning disable EF1002
        await db.Database.ExecuteSqlRawAsync(
            $"SET LOCAL tagekyc.actor_principal_id = '{actorPrincipalId:D}';",
            cancellationToken);
#pragma warning restore EF1002
        return transaction;
    }

    private async Task TakeSharedLockAsync(string key, CancellationToken cancellationToken) =>
        await ScalarIntAsync(
            "SELECT pg_advisory_xact_lock_shared(hashtext(@key)); SELECT 1;",
            cancellationToken,
            Parameter("key", key));

    private async Task<int> ScalarIntAsync(
        string sql,
        CancellationToken cancellationToken,
        params NpgsqlParameter[] parameters)
    {
        var value = await ScalarAsync(sql, cancellationToken, parameters);
        return Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    private async Task<DateTimeOffset> ScalarDateTimeOffsetAsync(
        string sql,
        CancellationToken cancellationToken,
        params NpgsqlParameter[] parameters)
    {
        var value = await ScalarAsync(sql, cancellationToken, parameters);
        return value switch
        {
            DateTimeOffset dto => dto,
            DateTime dt => new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc)),
            _ => throw new InvalidOperationException("RAW_EXPORT_DB_TIME_INVALID"),
        };
    }

    private async Task<object?> ScalarAsync(
        string sql,
        CancellationToken cancellationToken,
        params NpgsqlParameter[] parameters)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        if (db.Database.CurrentTransaction is not null)
        {
            command.Transaction = db.Database.CurrentTransaction.GetDbTransaction();
        }

        foreach (var parameter in parameters)
        {
            command.Parameters.Add(parameter);
        }

        return await command.ExecuteScalarAsync(cancellationToken);
    }

    private static NpgsqlParameter Parameter(string name, object? value) =>
        new(name, value ?? DBNull.Value);
}
