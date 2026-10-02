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
    public const string PrivilegeInvalid = "PROD_DB_PRIVILEGE_INVALID";

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

        if (!await RelationExistsAsync("public", "__EFMigrationsHistory", cancellationToken))
        {
            Throw(MigrationHistoryMissing);
        }

        if (!await HasSelectPrivilegeAsync("public", "__EFMigrationsHistory", cancellationToken))
            Throw(PrivilegeInvalid);

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

        if (!await RelationExistsAsync("tagekyc", "append_idempotency_records", cancellationToken) ||
            !await RelationExistsAsync("tagekyc", "api_keys", cancellationToken))
        {
            Throw(RequiredTableMissing);
        }

        if (!await HasSelectPrivilegeAsync("tagekyc", "append_idempotency_records", cancellationToken) ||
            !await HasSelectPrivilegeAsync("tagekyc", "api_keys", cancellationToken))
            Throw(PrivilegeInvalid);
    }

    private async Task<bool> RelationExistsAsync(string schema, string table, CancellationToken cancellationToken)
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
                    FROM pg_catalog.pg_class relation
                    JOIN pg_catalog.pg_namespace namespace ON namespace.oid=relation.relnamespace
                    WHERE namespace.nspname = @schema AND relation.relname = @table
                      AND relation.relkind IN ('r','p')
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

    private async Task<bool> HasSelectPrivilegeAsync(
        string schema,
        string table,
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT pg_catalog.has_table_privilege(current_user, pg_catalog.format('%I.%I',@schema,@table), 'SELECT')";
        var schemaParameter = command.CreateParameter();
        schemaParameter.ParameterName = "schema";
        schemaParameter.Value = schema;
        command.Parameters.Add(schemaParameter);
        var tableParameter = command.CreateParameter();
        tableParameter.ParameterName = "table";
        tableParameter.Value = table;
        command.Parameters.Add(tableParameter);
        return await command.ExecuteScalarAsync(cancellationToken) is true;
    }

    private static void Throw(string code) => throw new PostgresProductionReadinessException(code);
}
