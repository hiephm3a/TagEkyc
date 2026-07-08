using System.Data;
using Microsoft.EntityFrameworkCore;

namespace TagEkyc.Infrastructure.Persistence;

public sealed class PostgresProductionReadinessException(string code) : InvalidOperationException(code)
{
    public string Code { get; } = code;
}

public sealed class PostgresProductionReadinessValidator(TagEkycDbContext dbContext)
{
    public const string ProviderInvalid = "PROD_DB_PROVIDER_INVALID";
    public const string Unreachable = "PROD_DB_UNREACHABLE";
    public const string MigrationHistoryMissing = "PROD_DB_MIGRATION_HISTORY_MISSING";
    public const string MigrationsPending = "PROD_DB_MIGRATIONS_PENDING";
    public const string RequiredTableMissing = "PROD_DB_REQUIRED_TABLE_MISSING";

    private const string NpgsqlProviderName = "Npgsql.EntityFrameworkCore.PostgreSQL";

    public async Task ValidateAsync(CancellationToken cancellationToken)
    {
        string? providerName;
        try
        {
            providerName = dbContext.Database.ProviderName;
        }
        catch
        {
            Throw(ProviderInvalid);
            return;
        }

        if (!string.Equals(providerName, NpgsqlProviderName, StringComparison.Ordinal))
        {
            Throw(ProviderInvalid);
        }

        try
        {
            if (!await dbContext.Database.CanConnectAsync(cancellationToken))
            {
                Throw(Unreachable);
            }
        }
        catch (PostgresProductionReadinessException)
        {
            throw;
        }
        catch
        {
            Throw(Unreachable);
        }

        if (!await TableExistsAsync("public", "__EFMigrationsHistory", cancellationToken))
        {
            Throw(MigrationHistoryMissing);
        }

        try
        {
            var pending = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
            if (pending.Any())
            {
                Throw(MigrationsPending);
            }
        }
        catch (PostgresProductionReadinessException)
        {
            throw;
        }
        catch
        {
            Throw(MigrationsPending);
        }

        if (!await TableExistsAsync("tagekyc", "append_idempotency_records", cancellationToken))
        {
            Throw(RequiredTableMissing);
        }
    }

    private async Task<bool> TableExistsAsync(string schema, string table, CancellationToken cancellationToken)
    {
        try
        {
            var connection = dbContext.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT EXISTS(
                    SELECT 1
                    FROM information_schema.tables
                    WHERE table_schema = @schema AND table_name = @table
                )
                """;
            var schemaParameter = command.CreateParameter();
            schemaParameter.ParameterName = "schema";
            schemaParameter.Value = schema;
            command.Parameters.Add(schemaParameter);
            var tableParameter = command.CreateParameter();
            tableParameter.ParameterName = "table";
            tableParameter.Value = table;
            command.Parameters.Add(tableParameter);

            return await command.ExecuteScalarAsync(cancellationToken) is bool exists && exists;
        }
        catch (PostgresProductionReadinessException)
        {
            throw;
        }
        catch
        {
            Throw(Unreachable);
            return false;
        }
    }

    private static void Throw(string code) => throw new PostgresProductionReadinessException(code);
}
