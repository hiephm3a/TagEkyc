using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.Infrastructure.RawExport;

internal sealed record ProvisionalObjectMutationResult(
    string OutcomeCode,
    Guid ObjectCustodyId,
    string ObjectState,
    long StateRevision);

internal sealed record ProvisionalObjectBeginResult(
    ProvisionalObjectMutationResult Mutation,
    string ObjectKey,
    Guid ProvisionalObjectIdentity,
    byte[] ObjectBindingDigest,
    string RawClass,
    long ClaimedPlaintextLength,
    DateTimeOffset OwnershipLeaseExpiresAtUtc,
    DateTimeOffset EffectivePlaintextRetentionExpiresAtUtc,
    DateTimeOffset ReservationExpiresAtUtc,
    DateTimeOffset ProjectionAtUtc);

internal sealed class ProvisionalObjectCustodyRepository(TagEkycDbContext db)
{
    internal static DateTimeOffset ComputeWriterDeadline(ProvisionalObjectBeginResult begin)
    {
        var operationLimit = begin.ProjectionAtUtc + ProvisionalObjectCustodyOptions.FixedOperationTimeout;
        return new[]
        {
            operationLimit,
            begin.OwnershipLeaseExpiresAtUtc,
            begin.EffectivePlaintextRetentionExpiresAtUtc,
            begin.ReservationExpiresAtUtc,
        }.Min();
    }

    internal Task<ProvisionalObjectBeginResult> BeginAsync(
        Guid attemptId, long revision, long fence, int maximumPerSource, CancellationToken cancellationToken) =>
        ReadOneAsync<ProvisionalObjectBeginResult>("""
            SELECT * FROM tagekyc.raw_export_begin_provisional_object_custody(@attempt,@revision,@fence,@maximum)
            """,
            command =>
            {
                command.Parameters.AddWithValue("attempt", attemptId);
                command.Parameters.AddWithValue("revision", revision);
                command.Parameters.AddWithValue("fence", fence);
                command.Parameters.AddWithValue("maximum", maximumPerSource);
            },
            reader => new(
                ReadMutation(reader),
                reader.GetString(reader.GetOrdinal("ObjectKey")),
                reader.GetGuid(reader.GetOrdinal("ProvisionalObjectIdentity")),
                (byte[])reader[reader.GetOrdinal("ObjectBindingDigest")],
                reader.GetString(reader.GetOrdinal("RawClass")),
                reader.GetInt64(reader.GetOrdinal("ClaimedPlaintextLength")),
                reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("OwnershipLeaseExpiresAtUtc")),
                reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("EffectivePlaintextRetentionExpiresAtUtc")),
                reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("ReservationExpiresAtUtc")),
                reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("ProjectionAtUtc"))),
            cancellationToken);

    internal Task<ProvisionalObjectMutationResult> ArmAsync(
        Guid id, long revision, Guid operationId, CancellationToken cancellationToken) =>
        MutateAsync("raw_export_arm_provisional_object_put", id, revision, cancellationToken,
            ("operation", operationId));

    internal Task<ProvisionalObjectMutationResult> RecordNotArmedAsync(
        Guid id, long revision, byte[] evidence, CancellationToken cancellationToken) =>
        MutateAsync("raw_export_record_provisional_object_not_armed", id, revision, cancellationToken,
            ("evidence", evidence));

    internal Task<ProvisionalObjectMutationResult> RecordPutResultAsync(
        Guid id, long revision, Guid operationId, string kind, int? status, long? length,
        byte[]? digest, byte[]? receipt, CancellationToken cancellationToken) =>
        MutateAsync("raw_export_record_provisional_object_put_result", id, revision, cancellationToken,
            ("operation", operationId), ("kind", kind), ("status", status), ("length", length),
            ("digest", digest), ("receipt", receipt));

    private async Task<ProvisionalObjectMutationResult> MutateAsync(
        string function, Guid id, long revision, CancellationToken cancellationToken,
        params (string Name, object? Value)[] values)
    {
        var parameters = new List<string> { "@id", "@revision" };
        await using var command = await CreateCommandAsync(cancellationToken).ConfigureAwait(false);
        command.CommandText = string.Empty;
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("revision", revision);
        foreach (var (name, value) in values)
        {
            parameters.Add("@" + name);
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }
        command.CommandText = $"SELECT * FROM tagekyc.{function}({string.Join(',', parameters)})";
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException("RAW_EXPORT_PROVISIONAL_OBJECT_EMPTY_RESULT");
        return ReadMutation(reader);
    }

    private async Task<T> ReadOneAsync<T>(string sql, Action<NpgsqlCommand> bind,
        Func<NpgsqlDataReader, T> map, CancellationToken cancellationToken)
    {
        await using var command = await CreateCommandAsync(cancellationToken).ConfigureAwait(false);
        command.CommandText = sql;
        bind(command);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException("RAW_EXPORT_PROVISIONAL_OBJECT_EMPTY_RESULT");
        return map(reader);
    }

    private async Task<NpgsqlCommand> CreateCommandAsync(CancellationToken cancellationToken)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection.CreateCommand();
    }

    private static ProvisionalObjectMutationResult ReadMutation(NpgsqlDataReader reader) => new(
        reader.GetString(reader.GetOrdinal("OutcomeCode")),
        reader.GetGuid(reader.GetOrdinal("ObjectCustodyId")),
        reader.GetString(reader.GetOrdinal("ObjectState")),
        reader.GetInt64(reader.GetOrdinal("StateRevision")));
}
