using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace TagEkyc.Infrastructure.Persistence;

public sealed class RawExportAuthorizationReadinessException(string code) : InvalidOperationException(code)
{
    public string Code { get; } = code;
}

public sealed class RawExportAuthorizationReadinessValidator(TagEkycDbContext dbContext)
{
    public const string TableMutationPrivilege = "PROD_RAW_EXPORT_AUTHORIZATION_TABLE_MUTATION_PRIVILEGE";

    private static readonly string[] Tables =
    [
        "raw_export_authorization_idempotency",
        "raw_export_authorization_decisions",
        "raw_export_decision_eligibility_causes",
        "raw_export_decision_fulfillment_refs",
        "raw_export_decision_classes",
        "raw_export_authorization_permits",
        "raw_export_permit_classes",
    ];

    private static readonly string[] MutationPrivileges = ["INSERT", "UPDATE", "DELETE", "TRUNCATE"];

    public async Task ValidateAsync(CancellationToken cancellationToken)
    {
        foreach (var table in Tables)
        {
            if (!await HasPrivilegeAsync("tagekyc_runtime", table, "SELECT", cancellationToken) ||
                await HasAnyPrivilegeAsync("public", table, cancellationToken))
            {
                throw new RawExportAuthorizationReadinessException(TableMutationPrivilege);
            }

            foreach (var privilege in MutationPrivileges)
            {
                if (await HasPrivilegeAsync("tagekyc_runtime", table, privilege, cancellationToken))
                {
                    throw new RawExportAuthorizationReadinessException(TableMutationPrivilege);
                }
            }
        }
    }

    private async Task<bool> HasAnyPrivilegeAsync(string role, string table, CancellationToken cancellationToken)
    {
        foreach (var privilege in MutationPrivileges.Prepend("SELECT"))
        {
            if (await HasPrivilegeAsync(role, table, privilege, cancellationToken)) return true;
        }
        return false;
    }

    private async Task<bool> HasPrivilegeAsync(string role, string table, string privilege, CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT has_table_privilege(@role, @table, @privilege)";
        command.Parameters.Add(new NpgsqlParameter("role", role));
        command.Parameters.Add(new NpgsqlParameter("table", $"tagekyc.{table}"));
        command.Parameters.Add(new NpgsqlParameter("privilege", privilege));
        return await command.ExecuteScalarAsync(cancellationToken) is true;
    }
}
