using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using TagEkyc.Application.Ports;
using TagEkyc.Domain;

namespace TagEkyc.Infrastructure.Persistence;

public sealed class EfRawExportControlPlaneRepository : IRawExportControlPlaneRepository
{
    private readonly TagEkycDbContext db;
    private readonly IRawExportAuthorizationProjectionReader projections;

    public EfRawExportControlPlaneRepository(TagEkycDbContext db)
        : this(db, new EfRawExportAuthorizationProjectionReader(db))
    {
    }

    public EfRawExportControlPlaneRepository(
        TagEkycDbContext db,
        IRawExportAuthorizationProjectionReader projections)
    {
        this.db = db;
        this.projections = projections;
    }

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

        var projection = await projections.ReadEligibilityInputsAsync(
            principalId,
            policyId,
            policyVersion,
            cancellationToken);
        var fulfillmentRefs = new List<RawExportFulfillmentRef>();
        var missingFulfillment = false;
        foreach (var requirement in projection.Requirements.OrderBy(item => item.Ordinal))
        {
            var latest = requirement.LatestFulfillment;
            if (latest is null ||
                latest.EventType != RawExportFulfillmentEventType.Accepted ||
                latest.ValidFromUtc > projection.EvaluatedAtUtc ||
                (latest.ValidUntilUtc is not null && projection.EvaluatedAtUtc >= latest.ValidUntilUtc))
            {
                missingFulfillment = true;
                continue;
            }

            fulfillmentRefs.Add(new RawExportFulfillmentRef(
                requirement.RequirementType,
                latest.FulfillmentEventId,
                latest.Revision,
                latest.ArtifactRef ?? string.Empty,
                latest.ArtifactVersion ?? string.Empty,
                latest.ValidUntilUtc));
        }

        var causes = new List<RawExportEligibilityCause>();
        if (projection.ClosureType == RawExportPolicyClosureType.Abandoned)
        {
            causes.Add(RawExportEligibilityCause.NotCatalogApproved);
        }
        else if (!projection.PolicyExists ||
                 projection.ClosureType != RawExportPolicyClosureType.CatalogApproved)
        {
            causes.Add(RawExportEligibilityCause.NotCatalogApproved);
        }

        if (projection.Lifecycle?.EventType == RawExportLifecycleEventType.Revoked)
        {
            causes.Add(RawExportEligibilityCause.PolicyRevoked);
        }
        else if (projection.Lifecycle?.EventType == RawExportLifecycleEventType.Suspended)
        {
            causes.Add(RawExportEligibilityCause.PolicySuspended);
        }
        else if (projection.Lifecycle?.EventType != RawExportLifecycleEventType.Activated)
        {
            causes.Add(RawExportEligibilityCause.PolicyNotActive);
        }

        if (projection.Grant is null)
        {
            causes.Add(RawExportEligibilityCause.GrantMissing);
        }
        else if (projection.Grant.EventType == RawExportGrantEventType.Revoked)
        {
            causes.Add(RawExportEligibilityCause.GrantRevoked);
        }

        var boundRuleSetVersion = projection.BoundRuleSetVersion ?? 0;
        if (projection.PolicyExists &&
            boundRuleSetVersion != projection.CurrentRuleSetVersion)
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
            projection.EvaluatedAtUtc,
            boundRuleSetVersion,
            projection.CurrentRuleSetVersion,
            projection.Grant is null
                ? null
                : new RawExportGrantRef(
                    projection.Grant.PrincipalId,
                    projection.Grant.PolicyId,
                    projection.Grant.PolicyVersion,
                    projection.Grant.Revision),
            projection.Lifecycle is null
                ? null
                : new RawExportLifecycleRef(
                    projection.Lifecycle.PolicyId,
                    projection.Lifecycle.PolicyVersion,
                    projection.Lifecycle.Revision),
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

    private async Task<int> ScalarIntAsync(
        string sql,
        CancellationToken cancellationToken,
        params NpgsqlParameter[] parameters)
    {
        var value = await ScalarAsync(sql, cancellationToken, parameters);
        return Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);
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
