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
    public const string FunctionAclInvalid = "PROD_RAW_EXPORT_AUTHORIZATION_FUNCTION_ACL_INVALID";
    public const string TableMutationPrivilege = "PROD_RAW_EXPORT_AUTHORIZATION_TABLE_MUTATION_PRIVILEGE";

    private static readonly IReadOnlyDictionary<string,
        (string Arguments, string Result, bool SecurityDefiner, bool RuntimeExecute)> Functions =
        new Dictionary<string, (string Arguments, string Result, bool SecurityDefiner, bool RuntimeExecute)>(
            StringComparer.Ordinal)
        {
            ["raw_export_lock_verification_session_for_authorization"] =
                ("uuid", "TABLE(\"ClientApplicationId\" uuid, \"SubjectRef\" text, \"State\" text)", true, true),
            ["raw_export_claim_or_read_authorization_idempotency"] =
                ("uuid, uuid, uuid, text, bytea, uuid",
                    "TABLE(outcome text, export_decision_id uuid)", true, true),
            ["raw_export_persist_authorization_decision"] = ("jsonb", "uuid", true, true),
            ["enforce_raw_export_authorization_insert"] = ("", "trigger", false, false),
            ["enforce_raw_export_decision_child_same_transaction"] = ("", "trigger", false, false),
            ["enforce_raw_export_permit_child_same_transaction"] = ("", "trigger", false, false),
            ["enforce_raw_export_permit_has_classes"] = ("", "trigger", false, false),
        };

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
        if (!await HasExpectedFunctionManifestAsync(cancellationToken))
        {
            throw new RawExportAuthorizationReadinessException(FunctionAclInvalid);
        }

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

    private async Task<bool> HasExpectedFunctionManifestAsync(CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT p.proname,
                   pg_catalog.oidvectortypes(p.proargtypes),
                   pg_catalog.pg_get_function_result(p.oid),
                   p.prosecdef,
                   owner.rolname,
                   p.proconfig = ARRAY['search_path=pg_catalog']::text[],
                   pg_catalog.has_function_privilege('public', p.oid, 'EXECUTE'),
                   pg_catalog.has_function_privilege('tagekyc_runtime', p.oid, 'EXECUTE')
            FROM pg_catalog.pg_proc p
            JOIN pg_catalog.pg_namespace n ON n.oid = p.pronamespace
            JOIN pg_catalog.pg_roles owner ON owner.oid = p.proowner
            WHERE n.nspname = 'tagekyc' AND p.proname = ANY(@functions)
            ORDER BY p.proname;
            """;
        command.Parameters.Add(new NpgsqlParameter("functions", Functions.Keys.ToArray()));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var found = new HashSet<string>(StringComparer.Ordinal);
        while (await reader.ReadAsync(cancellationToken))
        {
            var name = reader.GetString(0);
            if (!Functions.TryGetValue(name, out var expected) || !found.Add(name) ||
                reader.GetString(1) != expected.Arguments || reader.GetString(2) != expected.Result ||
                reader.GetBoolean(3) != expected.SecurityDefiner ||
                reader.GetString(4) != "tagekyc_raw_export_deployer" ||
                !reader.GetBoolean(5) || reader.GetBoolean(6) ||
                reader.GetBoolean(7) != expected.RuntimeExecute)
            {
                return false;
            }
        }

        return found.SetEquals(Functions.Keys);
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
