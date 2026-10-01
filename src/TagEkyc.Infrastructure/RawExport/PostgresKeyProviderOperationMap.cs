using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.Infrastructure.RawExport;

internal sealed class PostgresKeyProviderOperationMap(TagEkycDbContext db)
{
    internal async Task<string> RecordWrappedAsync(Guid providerOperationId, Guid reservationId, Guid preparationId,
        long fence, ProviderOperationToken token, KekWrappedMaterial wrapped, CancellationToken cancellationToken)
    {
        if (!DurableKeyText.IsValid(wrapped.SuiteId)
            || !DurableKeyText.IsValid(wrapped.Receipt)
            || !DurableKeyText.IsValid(wrapped.ProviderResourceReference))
        {
            return "ShapeInvalid";
        }
        await using var command = (NpgsqlCommand)db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT tagekyc.raw_export_record_key_provider_wrapped_result(@op,@r,@p,@f,@t,@rid,@rv,@ct,@n,@tag,@opaque,@sid,@sv,@receipt,@ref)";
        Add(command,"op",providerOperationId); Add(command,"r",reservationId); Add(command,"p",preparationId); Add(command,"f",fence);
        Add(command,"t",token.Value); Add(command,"rid",wrapped.RepresentationId); Add(command,"rv",wrapped.RepresentationVersion);
        var legacy = wrapped as LegacyAesGcmWrappedMaterial;
        var opaque = wrapped as OpaqueProviderWrappedMaterial;
        AddNullable(command,"ct",NpgsqlDbType.Bytea,legacy?.Ciphertext);
        AddNullable(command,"n",NpgsqlDbType.Bytea,legacy?.Nonce);
        AddNullable(command,"tag",NpgsqlDbType.Bytea,legacy?.Tag);
        AddNullable(command,"opaque",NpgsqlDbType.Bytea,opaque?.OpaquePayload);
        Add(command,"sid",wrapped.SuiteId); Add(command,"sv",wrapped.SuiteVersion); Add(command,"receipt",wrapped.Receipt); Add(command,"ref",wrapped.ProviderResourceReference);
        await db.Database.OpenConnectionAsync(cancellationToken);
        return (string)(await command.ExecuteScalarAsync(cancellationToken) ?? "StateConflict");
    }

    internal async Task<string> ActivateAsync(Guid reservationId, Guid preparationId, long fence, CancellationToken cancellationToken)
    {
        await using var command = (NpgsqlCommand)db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT tagekyc.raw_export_activate_attempt_key_reservation(@r,@p,@f)";
        Add(command,"r",reservationId); Add(command,"p",preparationId); Add(command,"f",fence);
        await db.Database.OpenConnectionAsync(cancellationToken);
        return (string)(await command.ExecuteScalarAsync(cancellationToken) ?? "StateConflict");
    }

    internal static void Add(NpgsqlCommand command, string name, object value) => command.Parameters.AddWithValue(name,value);
    private static void AddNullable(NpgsqlCommand command, string name, NpgsqlDbType type, object? value) =>
        command.Parameters.Add(new NpgsqlParameter(name,type){Value=value ?? DBNull.Value});
}
