using Npgsql;
using NpgsqlTypes;

namespace TagEkyc.Infrastructure.RawExport;

// The existing role-specific scope supplies the qualified data source. No new
// login, provider access, lease or Client credential is introduced by readback.
internal sealed class RawSourceRetentionContinuationRepository(NpgsqlDataSource source)
    : IRawSourceRetentionContinuationRepository
{
    private static readonly string[] Columns =
    [
        "SourceArtifactId", "CustodyPrincipalId", "ClientApplicationId", "VerificationSessionId",
        "RuntimeBindingId", "RetentionAuthorityId", "RetentionAuthorityRevision", "CustodyState",
        "ReservationRevision", "Fence", "AttemptId", "EncryptionAttemptRevision", "AttemptKeyReservationId",
        "ObjectCustodyId", "ObjectState", "ObjectStateRevision", "SourcePublicationId", "PublicationRevision",
        "PublicationState", "CleanupDisposition", "R2TerminalIntentCode", "R2TerminalIntentDisposition",
        "R2TerminalIntentAtUtc", "R2TerminationDisposition", "R2TerminatedAtUtc", "R2TerminalOutcomeCode",
    ];

    public async Task<RawSourceRetentionContinuation?> ReadAsync(Guid sourceArtifactId, CancellationToken cancellationToken)
    {
        await using var connection = await source.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new NpgsqlCommand(
            "SELECT * FROM tagekyc.raw_export_read_retained_source_continuation(@source)", connection);
        command.Parameters.AddWithValue("source", sourceArtifactId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (reader.FieldCount != Columns.Length
            || !Columns.SequenceEqual(Enumerable.Range(0, reader.FieldCount).Select(reader.GetName), StringComparer.Ordinal))
            throw InvalidResult();
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            if (await reader.NextResultAsync(cancellationToken).ConfigureAwait(false)) throw InvalidResult();
            return null;
        }
        var result = new RawSourceRetentionContinuation(
            reader.GetGuid(0), reader.GetGuid(1), reader.GetGuid(2), reader.GetGuid(3), reader.GetGuid(4), reader.GetGuid(5),
            reader.GetInt64(6), reader.GetString(7), reader.GetInt64(8), reader.GetInt64(9), reader.GetGuid(10),
            reader.GetInt64(11), reader.GetGuid(12), Value<Guid>(reader,13), Text(reader,14), Value<long>(reader,15),
            Value<Guid>(reader,16), Value<long>(reader,17), Text(reader,18), Text(reader,19), Text(reader,20),
            Text(reader,21), Value<DateTimeOffset>(reader,22), Text(reader,23), Value<DateTimeOffset>(reader,24), Text(reader,25));
        if (result.SourceArtifactId != sourceArtifactId
            || new[] { result.SourceArtifactId, result.CustodyPrincipalId, result.ClientApplicationId,
                result.VerificationSessionId, result.RuntimeBindingId, result.RetentionAuthorityId,
                result.AttemptId, result.AttemptKeyReservationId }.Contains(Guid.Empty)
            || result.RetentionAuthorityRevision < 1 || result.ReservationRevision < 1
            || result.EncryptionAttemptRevision < 1 || result.Fence < 1
            || !AllOrNone(result.ObjectCustodyId, result.ObjectState, result.ObjectStateRevision)
            || !AllOrNone(result.SourcePublicationId, result.PublicationRevision, result.PublicationState, result.CleanupDisposition)
            || !AllOrNone(result.R2TerminalIntentCode, result.R2TerminalIntentDisposition, result.R2TerminalIntentAtUtc)
            || !AllOrNone(result.R2TerminationDisposition, result.R2TerminatedAtUtc))
            throw InvalidResult();
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false)
            || await reader.NextResultAsync(cancellationToken).ConfigureAwait(false)) throw InvalidResult();
        return result;
    }

    public async Task<IReadOnlyList<Guid>> ScanAsync(Guid? afterSourceArtifactId, int limit, CancellationToken cancellationToken)
    {
        // SQL owns the same bound and typed denial for every caller. Passing the
        // requested bound through keeps direct and hosted behavior identical.
        await using var connection = await source.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new NpgsqlCommand(
            "SELECT * FROM tagekyc.raw_export_list_retained_source_continuations(@after,@limit)", connection);
        command.Parameters.Add(new NpgsqlParameter("after", NpgsqlDbType.Uuid) { Value = (object?)afterSourceArtifactId ?? DBNull.Value });
        command.Parameters.AddWithValue("limit", limit);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (reader.FieldCount != 1 || reader.GetName(0) != "SourceArtifactId") throw InvalidResult();
        var results = new List<Guid>();
        var cursor = afterSourceArtifactId;
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var id = reader.GetGuid(0);
            // UUID textual ordinal order is PostgreSQL UUID byte order; do not
            // assume a platform-specific little-endian Guid byte-array ordering.
            if (id == Guid.Empty || results.Count >= limit || results.Count >= 100
                || (cursor is Guid previous && StringComparer.Ordinal.Compare(id.ToString("N"), previous.ToString("N")) <= 0))
                throw InvalidResult();
            results.Add(id);
            cursor = id;
        }
        if (await reader.NextResultAsync(cancellationToken).ConfigureAwait(false)) throw InvalidResult();
        return results.AsReadOnly();
    }

    private static T? Value<T>(NpgsqlDataReader reader, int ordinal) where T : struct =>
        reader.IsDBNull(ordinal) ? null : reader.GetFieldValue<T>(ordinal);
    private static string? Text(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    private static bool AllOrNone(params object?[] values) => values.All(value => value is null) || values.All(value => value is not null);
    private static InvalidOperationException InvalidResult() => new("A3_CONTINUATION_RESULT_INVALID");
}
