using Microsoft.EntityFrameworkCore;
using Npgsql;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.Infrastructure.RawExport;

internal sealed record AttemptKeyRecoveryContext(string PreparationDisposition, Guid AttemptId, byte[] ContextFingerprint,
    Guid? PreparationId, long Fence, ProviderOperationToken? ProviderOperationToken, string? ProviderOperationState,
    string? ProviderCleanupReference, Guid? AbandonRequestEventId, string? AbandonmentProviderOperationToken);

internal sealed class AttemptKeyRecoveryContextReader(TagEkycDbContext db)
{
    internal async Task<AttemptKeyRecoveryContext?> ReadAsync(Guid reservationId, CancellationToken cancellationToken)
    {
        await db.Database.OpenConnectionAsync(cancellationToken);
        await using var command=(NpgsqlCommand)db.Database.GetDbConnection().CreateCommand();
        command.CommandText="SELECT * FROM tagekyc.raw_export_read_current_attempt_key_recovery_context(@r)";
        command.Parameters.AddWithValue("r",reservationId);
        await using var reader=await command.ExecuteReaderAsync(cancellationToken);
        if(!await reader.ReadAsync(cancellationToken)) return null;
        return new(reader.GetString(0),reader.GetGuid(1),(byte[])reader[2],reader.IsDBNull(3)?null:reader.GetGuid(3),reader.GetInt64(4),reader.IsDBNull(6)?null:new ProviderOperationToken(reader.GetString(6)),reader.IsDBNull(15)?null:reader.GetString(15),reader.IsDBNull(28)?null:reader.GetString(28),reader.IsDBNull(35)?null:reader.GetGuid(35),reader.IsDBNull(36)?null:reader.GetString(36));
    }
}
