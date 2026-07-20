using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using TagEkyc.Application.Ports;
using TagEkyc.Domain;

namespace TagEkyc.Infrastructure.Persistence;

public sealed class EfRawExportSubjectConsentRepository(TagEkycDbContext db) : IRawExportSubjectConsentRepository
{
    public Task<int> GrantConsentAuthorityAsync(
        RawExportSubjectConsentAuthorityCommand command,
        CancellationToken cancellationToken = default) =>
        AppendAuthorityAsync(command, RawExportSubjectConsentAuthorityEventType.Granted, cancellationToken);

    public Task<int> RevokeConsentAuthorityAsync(
        RawExportSubjectConsentAuthorityCommand command,
        CancellationToken cancellationToken = default) =>
        AppendAuthorityAsync(command, RawExportSubjectConsentAuthorityEventType.Revoked, cancellationToken);

    public async Task<RawExportSubjectConsentSnapshot> RecordSubjectConsentGrantedAsync(
        RawExportSubjectConsentGrantCommand command,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await BeginCommandTransactionAsync(command.ActorPrincipalId, cancellationToken);
        var rawClasses = RawExportConsentScopeCodec.CanonicalizeClasses(command.ConsentedRawClasses)
            .Select(rawClass => rawClass.ToString())
            .ToArray();
        await ScalarIntAsync(
            """
            SELECT tagekyc.raw_export_append_subject_consent_granted(
                @verificationSessionId,@policyId,@policyVersion,@rawClasses,
                @consentTextVersion,@consentTextContentHash,@externalConsentArtifactRef,@decisionRef,@validUntilUtc);
            """,
            cancellationToken,
            Parameter("verificationSessionId", command.VerificationSessionId),
            Parameter("policyId", command.PolicyId),
            Parameter("policyVersion", command.PolicyVersion),
            Parameter("rawClasses", rawClasses),
            Parameter("consentTextVersion", command.ConsentTextVersion),
            Parameter("consentTextContentHash", command.ConsentTextContentHash),
            Parameter("externalConsentArtifactRef", command.ExternalConsentArtifactRef),
            Parameter("decisionRef", command.DecisionRef),
            Parameter("validUntilUtc", command.ValidUntilUtc));

        var snapshot = await ResolveSubjectExportConsentForAuthorizationAsync(
            command.VerificationSessionId,
            command.PolicyId,
            command.PolicyVersion,
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return snapshot;
    }

    public async Task<RawExportSubjectConsentSnapshot> RecordSubjectConsentWithdrawnAsync(
        RawExportSubjectConsentWithdrawCommand command,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await BeginCommandTransactionAsync(command.ActorPrincipalId, cancellationToken);
        await ScalarIntAsync(
            """
            SELECT tagekyc.raw_export_append_subject_consent_withdrawn(
                @verificationSessionId,@policyId,@policyVersion,@expectedRevision,@targetRevision,@decisionRef,@externalConsentArtifactRef);
            """,
            cancellationToken,
            Parameter("verificationSessionId", command.VerificationSessionId),
            Parameter("policyId", command.PolicyId),
            Parameter("policyVersion", command.PolicyVersion),
            Parameter("expectedRevision", command.ExpectedRevision),
            Parameter("targetRevision", command.TargetRevision),
            Parameter("decisionRef", command.DecisionRef),
            Parameter("externalConsentArtifactRef", command.ExternalConsentArtifactRef));

        var snapshot = await ResolveSubjectExportConsentForAuthorizationAsync(
            command.VerificationSessionId,
            command.PolicyId,
            command.PolicyVersion,
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return snapshot;
    }

    public async Task<RawExportSubjectConsentSnapshot> ResolveSubjectExportConsentForAuthorizationAsync(
        Guid verificationSessionId,
        Guid policyId,
        int policyVersion,
        CancellationToken cancellationToken = default)
    {
        if (db.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("RAW_EXPORT_SUBJECT_CONSENT_REQUIRES_AMBIENT_TRANSACTION");
        }

        var rows = await QueryResolverRowsAsync(verificationSessionId, policyId, policyVersion, cancellationToken);
        if (rows.Count == 0)
        {
            throw new InvalidOperationException("RAW_EXPORT_SUBJECT_CONSENT_RESOLVER_EMPTY");
        }

        var first = rows[0];
        var consentRef = first.SubjectConsentRecordId is null || first.Revision is null
            ? null
            : new RawExportSubjectConsentRef(
                first.SubjectConsentRecordId.Value,
                Convert.ToHexString(first.ConsentScopeHash ?? []).ToLowerInvariant(),
                first.Revision.Value);

        var classes = first.State == RawExportSubjectConsentState.Effective
            ? RawExportConsentScopeCodec.CanonicalizeClasses(rows
                .Where(row => row.RawClass is not null)
                .Select(row => Enum.Parse<RawExportRawClass>(row.RawClass!)))
            : [];

        return new RawExportSubjectConsentSnapshot(
            first.State,
            first.Cause,
            consentRef,
            first.VerificationSessionId,
            first.SubjectRef,
            first.PolicyId,
            first.PolicyVersion,
            first.PurposeCode,
            first.RecipientClientApplicationId,
            classes,
            first.ValidFromUtc,
            first.ValidUntilUtc,
            first.EvaluatedAtUtc,
            first.ConsentTextVersion,
            first.ConsentTextContentHash,
            first.ExternalConsentArtifactRef,
            first.DecisionRef);
    }

    private async Task<int> AppendAuthorityAsync(
        RawExportSubjectConsentAuthorityCommand command,
        RawExportSubjectConsentAuthorityEventType eventType,
        CancellationToken cancellationToken)
    {
        await using var transaction = await BeginCommandTransactionAsync(command.ActorPrincipalId, cancellationToken);
        var revision = await ScalarIntAsync(
            """
            SELECT tagekyc.raw_export_append_subject_consent_authority(
                @authorityPrincipalId,@clientApplicationId,@authorityType,@expectedRevision,@eventType,@targetRevision,@decisionRef,@validUntilUtc);
            """,
            cancellationToken,
            Parameter("authorityPrincipalId", command.AuthorityPrincipalId),
            Parameter("clientApplicationId", command.ClientApplicationId),
            Parameter("authorityType", command.AuthorityType.ToString()),
            Parameter("expectedRevision", command.ExpectedRevision),
            Parameter("eventType", eventType.ToString()),
            Parameter("targetRevision", command.TargetRevision),
            Parameter("decisionRef", command.DecisionRef),
            Parameter("validUntilUtc", command.ValidUntilUtc));
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

    private async Task<IReadOnlyList<ResolverRow>> QueryResolverRowsAsync(
        Guid verificationSessionId,
        Guid policyId,
        int policyVersion,
        CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT "State","Cause","SubjectConsentRecordId","ConsentScopeHash","Revision",
                   "VerificationSessionId","SubjectRef","PolicyId","PolicyVersion","PurposeCode",
                   "RecipientClientApplicationId","RawClass","ValidFromUtc","ValidUntilUtc",
                   "EvaluatedAtUtc","ConsentTextVersion","ConsentTextContentHash",
                   "ExternalConsentArtifactRef","DecisionRef"
            FROM tagekyc.raw_export_resolve_subject_consent_for_authorization(
                @verificationSessionId,@policyId,@policyVersion);
            """;
        command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
        command.Parameters.Add(Parameter("verificationSessionId", verificationSessionId));
        command.Parameters.Add(Parameter("policyId", policyId));
        command.Parameters.Add(Parameter("policyVersion", policyVersion));

        var rows = new List<ResolverRow>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new ResolverRow(
                Enum.Parse<RawExportSubjectConsentState>(reader.GetString(0)),
                reader.IsDBNull(1) ? null : Enum.Parse<RawExportSubjectConsentCause>(reader.GetString(1)),
                reader.IsDBNull(2) ? null : reader.GetGuid(2),
                reader.IsDBNull(3) ? null : (byte[])reader.GetValue(3),
                reader.IsDBNull(4) ? null : reader.GetInt32(4),
                reader.GetGuid(5),
                reader.GetString(6),
                reader.GetGuid(7),
                reader.GetInt32(8),
                reader.GetString(9),
                reader.GetGuid(10),
                reader.IsDBNull(11) ? null : reader.GetString(11),
                ReadDateTimeOffset(reader, 12),
                ReadDateTimeOffset(reader, 13),
                ReadDateTimeOffset(reader, 14) ?? throw new InvalidOperationException("RAW_EXPORT_DB_TIME_INVALID"),
                reader.IsDBNull(15) ? null : reader.GetString(15),
                reader.IsDBNull(16) ? null : reader.GetString(16),
                reader.IsDBNull(17) ? null : reader.GetString(17),
                reader.IsDBNull(18) ? null : reader.GetString(18)));
        }

        return rows;
    }

    private static DateTimeOffset? ReadDateTimeOffset(System.Data.Common.DbDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        var value = reader.GetValue(ordinal);
        return value switch
        {
            DateTimeOffset dto => dto,
            DateTime dt => new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc)),
            _ => throw new InvalidOperationException("RAW_EXPORT_DB_TIME_INVALID"),
        };
    }

    private static NpgsqlParameter Parameter(string name, object? value) =>
        new(name, value ?? DBNull.Value);

    private sealed record ResolverRow(
        RawExportSubjectConsentState State,
        RawExportSubjectConsentCause? Cause,
        Guid? SubjectConsentRecordId,
        byte[]? ConsentScopeHash,
        int? Revision,
        Guid VerificationSessionId,
        string SubjectRef,
        Guid PolicyId,
        int PolicyVersion,
        string PurposeCode,
        Guid RecipientClientApplicationId,
        string? RawClass,
        DateTimeOffset? ValidFromUtc,
        DateTimeOffset? ValidUntilUtc,
        DateTimeOffset EvaluatedAtUtc,
        string? ConsentTextVersion,
        string? ConsentTextContentHash,
        string? ExternalConsentArtifactRef,
        string? DecisionRef);
}
