using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NpgsqlTypes;
using TagEkyc.Domain;

namespace TagEkyc.Infrastructure.Persistence;

public sealed class EfRawExportAuthorizationRepository(TagEkycDbContext db)
{
    private static readonly JsonSerializerOptions PayloadJsonOptions = new();

    public async Task<Guid> PersistAuthorizationDecisionAsync(
        RawExportAuthorizationPersistencePayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        if (db.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("RAW_EXPORT_AUTHORIZATION_REQUIRES_AMBIENT_TRANSACTION");
        }

        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = new NpgsqlCommand(
            "SELECT tagekyc.raw_export_persist_authorization_decision(@payload);",
            (NpgsqlConnection)connection,
            (NpgsqlTransaction)db.Database.CurrentTransaction.GetDbTransaction());
        command.Parameters.Add("payload", NpgsqlDbType.Jsonb).Value =
            JsonSerializer.Serialize(payload, PayloadJsonOptions);

        var persisted = (Guid)(await command.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("RAW_EXPORT_AUTHORIZATION_PERSIST_RETURN_MISSING"));
        if (persisted != payload.ExportDecisionId)
        {
            throw new InvalidOperationException("RAW_EXPORT_AUTHORIZATION_PERSIST_ID_MISMATCH");
        }

        return persisted;
    }
}
