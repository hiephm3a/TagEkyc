using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NpgsqlTypes;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.Infrastructure.RawExport;

internal sealed class RawExportR3Repository(TagEkycDbContext db)
{
    internal async Task SetActorLocalAsync(Guid actorPrincipalId, CancellationToken cancellationToken)
    {
        await using var command = CreateCommand();
        command.CommandText = "SELECT pg_catalog.set_config('tagekyc.actor_principal_id',@actor,true)";
        command.Parameters.AddWithValue("actor", NpgsqlDbType.Text, actorPrincipalId.ToString("D"));
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    internal async Task<RawExportR3StageResult> StageAsync(
        RawExportR3StageCommand commandValue,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand();
        command.CommandText = """
            SELECT *
            FROM tagekyc.raw_export_stage_verified_source_ciphertext(
                @attempt,@object,@reservation_revision,@attempt_revision,@fence,@object_revision)
            """;
        command.Parameters.AddWithValue("attempt", NpgsqlDbType.Uuid, commandValue.AttemptId);
        command.Parameters.AddWithValue("object", NpgsqlDbType.Uuid, commandValue.ObjectCustodyId);
        command.Parameters.AddWithValue("reservation_revision", NpgsqlDbType.Bigint, commandValue.ExpectedReservationRevision);
        command.Parameters.AddWithValue("attempt_revision", NpgsqlDbType.Bigint, commandValue.ExpectedEncryptionAttemptRevision);
        command.Parameters.AddWithValue("fence", NpgsqlDbType.Bigint, commandValue.ExpectedFence);
        command.Parameters.AddWithValue("object_revision", NpgsqlDbType.Bigint, commandValue.ExpectedObjectStateRevision);

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException("RAW_EXPORT_R3_EMPTY_STAGE_RESULT");

        var disposition = reader.GetString(reader.GetOrdinal("Outcome")) switch
        {
            "Staged" => RawExportR3StageDisposition.Staged,
            "ExistingMatch" => RawExportR3StageDisposition.ExistingMatch,
            "NotFound" => RawExportR3StageDisposition.NotFound,
            "StateConflict" => RawExportR3StageDisposition.StateConflict,
            "SourceRetentionNotAuthorized" => RawExportR3StageDisposition.SourceRetentionNotAuthorized,
            _ => throw new InvalidOperationException("RAW_EXPORT_R3_UNKNOWN_STAGE_OUTCOME"),
        };

        var result = new RawExportR3StageResult(
            disposition,
            NullableGuid(reader, "SourceArtifactId"),
            NullableGuid(reader, "AttemptId"),
            NullableGuid(reader, "ObjectCustodyId"),
            NullableInt(reader, "StagedCiphertextFingerprintSchemaVersion"),
            NullableBytes(reader, "StagedCiphertextFingerprint"),
            NullableLong(reader, "ReservationRevision"),
            NullableLong(reader, "Fence"),
            NullableTimestamp(reader, "StagedAtUtc"));
        ValidateResultShape(result);
        return result;
    }

    private NpgsqlCommand CreateCommand()
    {
        var connection = db.Database.GetDbConnection() as NpgsqlConnection
            ?? throw new InvalidOperationException("RAW_EXPORT_R3_POSTGRES_REQUIRED");
        if (connection.State != ConnectionState.Open)
            throw new InvalidOperationException("RAW_EXPORT_R3_CONNECTION_NOT_OPEN");
        var command = connection.CreateCommand();
        command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction() as NpgsqlTransaction;
        return command;
    }

    private static void ValidateResultShape(RawExportR3StageResult result)
    {
        var complete = result.SourceArtifactId.HasValue
            && result.AttemptId.HasValue
            && result.ObjectCustodyId.HasValue
            && result.StagedCiphertextFingerprintSchemaVersion == RawExportR3StagedCiphertextFingerprintCodec.SchemaVersion
            && result.StagedCiphertextFingerprint is { Length: 32 }
            && result.ReservationRevision >= 2
            && result.Fence >= 1
            && result.StagedAtUtc.HasValue;
        if (result.Disposition is RawExportR3StageDisposition.Staged or RawExportR3StageDisposition.ExistingMatch)
        {
            if (!complete)
                throw new InvalidOperationException("RAW_EXPORT_R3_SUCCESS_SHAPE_INVALID");
            return;
        }

        if (result.SourceArtifactId.HasValue || result.AttemptId.HasValue || result.ObjectCustodyId.HasValue
            || result.StagedCiphertextFingerprintSchemaVersion.HasValue || result.StagedCiphertextFingerprint is not null
            || result.ReservationRevision.HasValue || result.Fence.HasValue || result.StagedAtUtc.HasValue)
            throw new InvalidOperationException("RAW_EXPORT_R3_FAILURE_SHAPE_INVALID");
    }

    private static Guid? NullableGuid(NpgsqlDataReader reader, string name) { var i = reader.GetOrdinal(name); return reader.IsDBNull(i) ? null : reader.GetGuid(i); }
    private static int? NullableInt(NpgsqlDataReader reader, string name) { var i = reader.GetOrdinal(name); return reader.IsDBNull(i) ? null : reader.GetInt32(i); }
    private static long? NullableLong(NpgsqlDataReader reader, string name) { var i = reader.GetOrdinal(name); return reader.IsDBNull(i) ? null : reader.GetInt64(i); }
    private static byte[]? NullableBytes(NpgsqlDataReader reader, string name) { var i = reader.GetOrdinal(name); return reader.IsDBNull(i) ? null : (byte[])reader[i]; }
    private static DateTimeOffset? NullableTimestamp(NpgsqlDataReader reader, string name) { var i = reader.GetOrdinal(name); return reader.IsDBNull(i) ? null : reader.GetFieldValue<DateTimeOffset>(i); }
}
