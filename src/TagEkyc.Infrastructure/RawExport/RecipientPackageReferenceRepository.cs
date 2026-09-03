using System.Data;
using Npgsql;

namespace TagEkyc.Infrastructure.RawExport;

internal sealed class RecipientPackageReferenceRepository(IRecipientPackageReferenceConnectionFactory connections)
{
    internal async Task<IReadOnlyList<RecipientPackageReferenceRow>> ListAsync(
        Guid owner,
        DateTimeOffset? boundaryTime,
        Guid? boundaryId,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new NpgsqlCommand(
            "SELECT * FROM tagekyc.raw_export_list_recipient_package_references(@owner,@time,@id,@size)", connection)
        { CommandTimeout = 30 };
        command.Parameters.AddWithValue("owner", owner);
        command.Parameters.AddWithValue("time", boundaryTime is null ? DBNull.Value : boundaryTime.Value);
        command.Parameters.AddWithValue("id", boundaryId is null ? DBNull.Value : boundaryId.Value);
        command.Parameters.AddWithValue("size", pageSize);
        var rows = new List<RecipientPackageReferenceRow>(pageSize + 1);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            rows.Add(new(reader.GetGuid(reader.GetOrdinal("PackageId")),
                reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("FinalizedAtUtc"))));
        return rows;
    }
}
