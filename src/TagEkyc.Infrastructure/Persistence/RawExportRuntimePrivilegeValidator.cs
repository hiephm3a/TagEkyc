using System.Data;
using Microsoft.EntityFrameworkCore;

namespace TagEkyc.Infrastructure.Persistence;

public sealed class RawExportRuntimePrivilegeException(string code) : InvalidOperationException(code)
{
    public string Code { get; } = code;
}

public sealed class RawExportRuntimePrivilegeValidator(TagEkycDbContext dbContext)
{
    public const string RuleTableMutationPrivilege = "PROD_RAW_EXPORT_RULE_TABLE_MUTATION_PRIVILEGE";

    private static readonly string[] RuleTables =
    [
        "raw_export_requirement_rule_sets",
        "raw_export_requirement_rules",
    ];

    private static readonly string[] MutationPrivileges =
    [
        "INSERT",
        "UPDATE",
        "DELETE",
    ];

    public async Task ValidateAsync(CancellationToken cancellationToken)
    {
        foreach (var table in RuleTables)
        {
            foreach (var privilege in MutationPrivileges)
            {
                if (await HasTablePrivilegeAsync(table, privilege, cancellationToken))
                {
                    Throw(RuleTableMutationPrivilege);
                }
            }
        }
    }

    private async Task<bool> HasTablePrivilegeAsync(
        string table,
        string privilege,
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT has_table_privilege(current_user, @table, @privilege)";

        var tableParameter = command.CreateParameter();
        tableParameter.ParameterName = "table";
        tableParameter.Value = $"tagekyc.{table}";
        command.Parameters.Add(tableParameter);

        var privilegeParameter = command.CreateParameter();
        privilegeParameter.ParameterName = "privilege";
        privilegeParameter.Value = privilege;
        command.Parameters.Add(privilegeParameter);

        return await command.ExecuteScalarAsync(cancellationToken) is bool hasPrivilege && hasPrivilege;
    }

    private static void Throw(string code) => throw new RawExportRuntimePrivilegeException(code);
}
