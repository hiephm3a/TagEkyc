using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NpgsqlTypes;
using TagEkyc.Application.Ports;
using TagEkyc.Domain;

namespace TagEkyc.Infrastructure.Persistence;

public sealed class EfRawExportAuthorizationProjectionReader(TagEkycDbContext db)
    : IRawExportAuthorizationProjectionReader
{
    private const string InvariantFailureCode = "RAW_EXPORT_AUTHORIZATION_INVARIANT_FAILURE";

    public async Task<RawExportAuthorizationEligibilityProjection> ReadEligibilityInputsAsync(
        Guid principalId,
        Guid policyId,
        int policyVersion,
        CancellationToken cancellationToken = default)
    {
        var transaction = CurrentTransaction();
        var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT *
            FROM tagekyc.raw_export_read_authorization_eligibility_inputs(
                @principalId,@policyId,@policyVersion);
            """,
            connection,
            transaction);
        AddIdentityParameters(command, principalId, policyId, policyVersion);

        Guid? echoedPolicyId = null;
        int? echoedPolicyVersion = null;
        DateTimeOffset? evaluatedAt = null;
        bool? policyExists = null;
        int? boundRuleSetVersion = null;
        var boundRuleSetVersionObserved = false;
        int? currentRuleSetVersion = null;
        RawExportPolicyClosureType? closureType = null;
        var closureObserved = false;
        RawExportAuthorizationGrantProjection? grant = null;
        var grantObserved = false;
        RawExportAuthorizationLifecycleProjection? lifecycle = null;
        var lifecycleObserved = false;
        var requirements = new List<RawExportAuthorizationRequirementProjection>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var rowPolicyId = reader.GetGuid(0);
            var rowPolicyVersion = reader.GetInt32(1);
            var rowEvaluatedAt = ReadTimestamp(reader, 2);
            var rowPolicyExists = reader.GetBoolean(3);
            int? rowBoundRuleSetVersion = reader.IsDBNull(4) ? null : reader.GetInt32(4);
            var rowCurrentRuleSetVersion = reader.GetInt32(5);
            RawExportPolicyClosureType? rowClosureType = reader.IsDBNull(6)
                ? null
                : ParseEnum<RawExportPolicyClosureType>(reader.GetString(6));
            var rowGrant = ReadGrant(reader);
            var rowLifecycle = ReadLifecycle(reader);

            if (echoedPolicyId is null)
            {
                echoedPolicyId = rowPolicyId;
                echoedPolicyVersion = rowPolicyVersion;
                evaluatedAt = rowEvaluatedAt;
                policyExists = rowPolicyExists;
                boundRuleSetVersion = rowBoundRuleSetVersion;
                boundRuleSetVersionObserved = true;
                currentRuleSetVersion = rowCurrentRuleSetVersion;
                closureType = rowClosureType;
                closureObserved = true;
                grant = rowGrant;
                grantObserved = true;
                lifecycle = rowLifecycle;
                lifecycleObserved = true;
            }
            else if (echoedPolicyId != rowPolicyId ||
                     echoedPolicyVersion != rowPolicyVersion ||
                     evaluatedAt != rowEvaluatedAt ||
                     policyExists != rowPolicyExists ||
                     !boundRuleSetVersionObserved ||
                     boundRuleSetVersion != rowBoundRuleSetVersion ||
                     currentRuleSetVersion != rowCurrentRuleSetVersion ||
                     !closureObserved ||
                     closureType != rowClosureType ||
                     !grantObserved ||
                     grant != rowGrant ||
                     !lifecycleObserved ||
                     lifecycle != rowLifecycle)
            {
                throw InvariantFailure();
            }

            if (reader.IsDBNull(16))
            {
                if (!reader.IsDBNull(17) || HasAnyValue(reader, 18, 24))
                {
                    throw InvariantFailure();
                }

                continue;
            }

            if (reader.IsDBNull(17))
            {
                throw InvariantFailure();
            }

            var ordinal = reader.GetInt32(16);
            if (ordinal != requirements.Count)
            {
                throw InvariantFailure();
            }

            var requirementType = ParseEnum<RawExportRequirementType>(reader.GetString(17));
            if (requirements.Any(item => item.RequirementType == requirementType))
            {
                throw InvariantFailure();
            }

            requirements.Add(new RawExportAuthorizationRequirementProjection(
                ordinal,
                requirementType,
                ReadFulfillment(reader)));
        }

        if (echoedPolicyId is null ||
            echoedPolicyId != policyId ||
            echoedPolicyVersion != policyVersion ||
            evaluatedAt is null ||
            policyExists is null ||
            currentRuleSetVersion is null)
        {
            throw InvariantFailure();
        }

        if (policyExists.Value != boundRuleSetVersion.HasValue)
        {
            throw InvariantFailure();
        }

        return new RawExportAuthorizationEligibilityProjection(
            echoedPolicyId.Value,
            echoedPolicyVersion.Value,
            evaluatedAt.Value,
            policyExists.Value,
            boundRuleSetVersion,
            currentRuleSetVersion.Value,
            closureType,
            grant,
            lifecycle,
            requirements);
    }

    public async Task<RawExportAuthorizationPolicyProjection> ReadPolicyInputsAsync(
        Guid principalId,
        Guid policyId,
        int policyVersion,
        CancellationToken cancellationToken = default)
    {
        var transaction = CurrentTransaction();
        var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT *
            FROM tagekyc.raw_export_read_authorization_policy_inputs(
                @principalId,@policyId,@policyVersion);
            """,
            connection,
            transaction);
        AddIdentityParameters(command, principalId, policyId, policyVersion);

        Guid? echoedPolicyId = null;
        int? echoedPolicyVersion = null;
        DateTimeOffset? evaluatedAt = null;
        bool? policyExists = null;
        int? permitTtlSeconds = null;
        var permitTtlObserved = false;
        RawExportPolicyClosureType? closureType = null;
        var closureObserved = false;
        var allowedClasses = new HashSet<RawExportRawClass>();
        var expectedOrdinal = 0;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var rowPolicyId = reader.GetGuid(0);
            var rowPolicyVersion = reader.GetInt32(1);
            var rowEvaluatedAt = ReadTimestamp(reader, 2);
            var rowPolicyExists = reader.GetBoolean(3);
            int? rowPermitTtlSeconds = reader.IsDBNull(4) ? null : reader.GetInt32(4);
            RawExportPolicyClosureType? rowClosureType = reader.IsDBNull(5)
                ? null
                : ParseEnum<RawExportPolicyClosureType>(reader.GetString(5));

            if (echoedPolicyId is null)
            {
                echoedPolicyId = rowPolicyId;
                echoedPolicyVersion = rowPolicyVersion;
                evaluatedAt = rowEvaluatedAt;
                policyExists = rowPolicyExists;
                permitTtlSeconds = rowPermitTtlSeconds;
                permitTtlObserved = true;
                closureType = rowClosureType;
                closureObserved = true;
            }
            else if (echoedPolicyId != rowPolicyId ||
                     echoedPolicyVersion != rowPolicyVersion ||
                     evaluatedAt != rowEvaluatedAt ||
                     policyExists != rowPolicyExists ||
                     !permitTtlObserved ||
                     permitTtlSeconds != rowPermitTtlSeconds ||
                     !closureObserved ||
                     closureType != rowClosureType)
            {
                throw InvariantFailure();
            }

            if (reader.IsDBNull(6))
            {
                if (!reader.IsDBNull(7))
                {
                    throw InvariantFailure();
                }

                continue;
            }

            if (reader.IsDBNull(7) || reader.GetInt32(6) != expectedOrdinal++)
            {
                throw InvariantFailure();
            }

            if (!allowedClasses.Add(ParseEnum<RawExportRawClass>(reader.GetString(7))))
            {
                throw InvariantFailure();
            }
        }

        if (echoedPolicyId is null ||
            echoedPolicyId != policyId ||
            echoedPolicyVersion != policyVersion ||
            evaluatedAt is null ||
            policyExists is null)
        {
            throw InvariantFailure();
        }

        if (!policyExists.Value &&
            (permitTtlSeconds is not null || closureType is not null || allowedClasses.Count != 0))
        {
            throw InvariantFailure();
        }

        return new RawExportAuthorizationPolicyProjection(
            echoedPolicyId.Value,
            echoedPolicyVersion.Value,
            evaluatedAt.Value,
            policyExists.Value,
            permitTtlSeconds,
            closureType,
            allowedClasses);
    }

    private static void AddIdentityParameters(
        NpgsqlCommand command,
        Guid principalId,
        Guid policyId,
        int policyVersion)
    {
        command.Parameters.Add("principalId", NpgsqlDbType.Uuid).Value = principalId;
        command.Parameters.Add("policyId", NpgsqlDbType.Uuid).Value = policyId;
        command.Parameters.Add("policyVersion", NpgsqlDbType.Integer).Value = policyVersion;
    }

    private static RawExportAuthorizationGrantProjection? ReadGrant(NpgsqlDataReader reader)
    {
        if (!HasAnyValue(reader, 7, 11))
        {
            return null;
        }

        if (HasAnyNull(reader, 7, 11))
        {
            throw InvariantFailure();
        }

        return new RawExportAuthorizationGrantProjection(
            reader.GetGuid(7),
            reader.GetGuid(8),
            reader.GetInt32(9),
            reader.GetInt32(10),
            ParseEnum<RawExportGrantEventType>(reader.GetString(11)));
    }

    private static RawExportAuthorizationLifecycleProjection? ReadLifecycle(NpgsqlDataReader reader)
    {
        if (!HasAnyValue(reader, 12, 15))
        {
            return null;
        }

        if (HasAnyNull(reader, 12, 15))
        {
            throw InvariantFailure();
        }

        return new RawExportAuthorizationLifecycleProjection(
            reader.GetGuid(12),
            reader.GetInt32(13),
            reader.GetInt32(14),
            ParseEnum<RawExportLifecycleEventType>(reader.GetString(15)));
    }

    private static RawExportAuthorizationFulfillmentProjection? ReadFulfillment(NpgsqlDataReader reader)
    {
        if (!HasAnyValue(reader, 18, 24))
        {
            return null;
        }

        if (reader.IsDBNull(18) ||
            reader.IsDBNull(19) ||
            reader.IsDBNull(20))
        {
            throw InvariantFailure();
        }

        var eventId = reader.GetGuid(18);
        var revision = reader.GetInt32(19);
        var eventType = ParseEnum<RawExportFulfillmentEventType>(reader.GetString(20));
        var artifactRef = reader.IsDBNull(21) ? null : reader.GetString(21);
        var artifactVersion = reader.IsDBNull(22) ? null : reader.GetString(22);
        DateTimeOffset? validFromUtc =
            reader.IsDBNull(23) ? null : ReadTimestamp(reader, 23);
        DateTimeOffset? validUntilUtc =
            reader.IsDBNull(24) ? null : ReadTimestamp(reader, 24);

        if (eventId == Guid.Empty || revision <= 0)
        {
            throw InvariantFailure();
        }

        switch (eventType)
        {
            case RawExportFulfillmentEventType.Accepted:
                if (string.IsNullOrWhiteSpace(artifactRef) ||
                    string.IsNullOrWhiteSpace(artifactVersion) ||
                    validFromUtc is null ||
                    validUntilUtc is not null &&
                    validUntilUtc.Value <= validFromUtc.Value)
                {
                    throw InvariantFailure();
                }

                break;

            case RawExportFulfillmentEventType.Withdrawn:
                if (artifactRef is not null ||
                    artifactVersion is not null ||
                    validFromUtc is not null ||
                    validUntilUtc is not null)
                {
                    throw InvariantFailure();
                }

                break;

            default:
                throw InvariantFailure();
        }

        return new RawExportAuthorizationFulfillmentProjection(
            eventId,
            revision,
            eventType,
            artifactRef,
            artifactVersion,
            validFromUtc,
            validUntilUtc);
    }

    private static T ParseEnum<T>(string value)
        where T : struct, Enum
    {
        if (!Enum.TryParse<T>(value, ignoreCase: false, out var parsed) ||
            !Enum.IsDefined(parsed))
        {
            throw InvariantFailure();
        }

        return parsed;
    }

    private static DateTimeOffset ReadTimestamp(NpgsqlDataReader reader, int ordinal)
    {
        return reader.GetValue(ordinal) switch
        {
            DateTimeOffset value => value.ToUniversalTime(),
            DateTime value => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)),
            _ => throw InvariantFailure(),
        };
    }

    private static bool HasAnyValue(NpgsqlDataReader reader, int start, int end)
    {
        for (var ordinal = start; ordinal <= end; ordinal++)
        {
            if (!reader.IsDBNull(ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasAnyNull(NpgsqlDataReader reader, int start, int end)
    {
        for (var ordinal = start; ordinal <= end; ordinal++)
        {
            if (reader.IsDBNull(ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        return connection;
    }

    private NpgsqlTransaction CurrentTransaction() =>
        (NpgsqlTransaction)(db.Database.CurrentTransaction?.GetDbTransaction()
            ?? throw new InvalidOperationException("RAW_EXPORT_AUTHORIZATION_REQUIRES_AMBIENT_TRANSACTION"));

    private static RawExportAuthorizationException InvariantFailure() =>
        new(InvariantFailureCode);
}
