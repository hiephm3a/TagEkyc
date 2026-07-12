using Microsoft.EntityFrameworkCore;
using System.Data;
using TagEkyc.Domain;

namespace TagEkyc.Infrastructure.Persistence;

public sealed class RawExportControlPlaneReadinessException(string code) : InvalidOperationException(code)
{
    public string Code { get; } = code;
}

public sealed class RawExportControlPlaneReadinessValidator(TagEkycDbContext dbContext)
{
    public const string RootAuthorityMissing = "PROD_RAW_EXPORT_ROOT_AUTHORITY_MISSING";

    public const string DevDefaultPrincipalForbidden = "PROD_RAW_EXPORT_ROOT_AUTHORITY_DEV_DEFAULT";

    public const string EventTableMutationPrivilege = "PROD_RAW_EXPORT_CONTROL_PLANE_TABLE_MUTATION_PRIVILEGE";

    public const string FunctionAclInvalid = "PROD_RAW_EXPORT_CONTROL_PLANE_FUNCTION_ACL_INVALID";

    public const string DeploymentRoleInvalid = "PROD_RAW_EXPORT_CONTROL_PLANE_DEPLOYMENT_ROLE_INVALID";

    private static readonly string[] EventTables =
    [
        "raw_export_grants",
        "raw_export_control_authorities",
        "raw_export_fulfillments",
        "raw_export_policy_lifecycle",
    ];

    private static readonly string[] MutationPrivileges =
    [
        "INSERT",
        "UPDATE",
        "DELETE",
    ];

    private static readonly string[] RequiredRootAuthorities =
    [
        RawExportAuthorityType.GrantAdmin.ToString(),
        RawExportAuthorityType.RecorderAuthorityAdmin.ToString(),
        RawExportAuthorityType.ActivationAuthority.ToString(),
    ];

    private static readonly FunctionExpectation[] ExpectedFunctions =
    [
        new("raw_export_current_actor", true, false, false),
        new("enforce_raw_export_control_plane_insert", false, false, false),
        new("raw_export_policy_exists", true, false, false),
        new("raw_export_has_current_authority", true, false, false),
        new("raw_export_append_grant", true, true, false),
        new("raw_export_append_control_authority", true, true, false),
        new("raw_export_bootstrap_global_authority", true, false, true),
        new("raw_export_append_fulfillment", true, true, false),
        new("raw_export_activation_gates_hold", true, false, false),
        new("raw_export_append_lifecycle", true, true, false),
    ];

    public async Task ValidateAsync(CancellationToken cancellationToken)
    {
        foreach (var authorityType in RequiredRootAuthorities)
        {
            var latest = await dbContext.RawExportControlAuthorities.AsNoTracking()
                .Where(row => row.AuthorityType == authorityType &&
                              row.ScopeType == RawExportAuthorityScopeType.Global.ToString() &&
                              row.ScopeId == null &&
                              row.RequirementType == null)
                .GroupBy(row => new { row.PrincipalId, row.AuthorityType, row.ScopeType, row.ScopeId, row.RequirementType })
                .Select(group => group.OrderByDescending(row => row.Revision).First())
                .ToListAsync(cancellationToken);
            var active = latest
                .Where(row => row.EventType == RawExportAuthorityEventType.Granted.ToString())
                .ToArray();

            if (active.Length == 0)
            {
                Throw(RootAuthorityMissing);
            }

            if (active.Any(row => row.PrincipalId == Guid.Empty ||
                                  row.PrincipalId == RawExportControlPlaneConstants.DeploymentPrincipalId))
            {
                Throw(DevDefaultPrincipalForbidden);
            }
        }

        foreach (var table in EventTables)
        {
            foreach (var privilege in MutationPrivileges)
            {
                if (await HasTablePrivilegeAsync(table, privilege, cancellationToken))
                {
                    Throw(EventTableMutationPrivilege);
                }
            }
        }

        await ValidateRolesAsync(cancellationToken);
        await ValidateFunctionAclAsync(cancellationToken);
    }

    private static void Throw(string code) => throw new RawExportControlPlaneReadinessException(code);

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

    private async Task ValidateRolesAsync(CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT rolname, rolcanlogin
            FROM pg_roles
            WHERE rolname IN ('tagekyc_runtime','tagekyc_raw_export_deployer','tagekyc_raw_export_bootstrapper');
            """;

        var roles = new Dictionary<string, bool>(StringComparer.Ordinal);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            roles[reader.GetString(0)] = reader.GetBoolean(1);
        }

        foreach (var role in new[] { "tagekyc_runtime", "tagekyc_raw_export_deployer", "tagekyc_raw_export_bootstrapper" })
        {
            if (!roles.TryGetValue(role, out var canLogin) || canLogin)
            {
                Throw(DeploymentRoleInvalid);
            }
        }

        await using var membershipCommand = connection.CreateCommand();
        membershipCommand.CommandText = """
            SELECT pg_has_role(current_user, 'tagekyc_raw_export_deployer', 'USAGE')
                OR pg_has_role(current_user, 'tagekyc_raw_export_bootstrapper', 'USAGE');
            """;
        if (await membershipCommand.ExecuteScalarAsync(cancellationToken) is true)
        {
            Throw(DeploymentRoleInvalid);
        }
    }

    private async Task ValidateFunctionAclAsync(CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT p.proname,
                   p.prosecdef,
                   owner.rolname,
                   owner.rolcanlogin,
                   COALESCE(array_to_string(p.proconfig, ','), ''),
                   has_function_privilege('public', p.oid, 'EXECUTE'),
                   has_function_privilege('tagekyc_runtime', p.oid, 'EXECUTE'),
                   has_function_privilege('tagekyc_raw_export_bootstrapper', p.oid, 'EXECUTE')
            FROM pg_proc p
            JOIN pg_namespace n ON n.oid = p.pronamespace
            JOIN pg_roles owner ON owner.oid = p.proowner
            WHERE n.nspname = 'tagekyc'
              AND (p.proname LIKE 'raw_export_%' OR p.proname = 'enforce_raw_export_control_plane_insert')
            ORDER BY p.proname;
            """;

        var rows = new Dictionary<string, FunctionInfo>(StringComparer.Ordinal);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows[reader.GetString(0)] = new FunctionInfo(
                reader.GetBoolean(1),
                reader.GetString(2),
                reader.GetBoolean(3),
                reader.GetString(4),
                reader.GetBoolean(5),
                reader.GetBoolean(6),
                reader.GetBoolean(7));
        }

        if (!rows.Keys.Order(StringComparer.Ordinal).SequenceEqual(
                ExpectedFunctions.Select(function => function.Name).Order(StringComparer.Ordinal),
                StringComparer.Ordinal))
        {
            Throw(FunctionAclInvalid);
        }

        foreach (var expected in ExpectedFunctions)
        {
            if (!rows.TryGetValue(expected.Name, out var actual) ||
                actual.SecurityDefiner != expected.SecurityDefiner ||
                actual.Owner != "tagekyc_raw_export_deployer" ||
                actual.OwnerCanLogin ||
                !actual.Config.Contains("search_path=pg_catalog", StringComparison.Ordinal) ||
                actual.PublicExecute ||
                actual.RuntimeExecute != expected.RuntimeExecute ||
                actual.BootstrapperExecute != expected.BootstrapperExecute)
            {
                Throw(FunctionAclInvalid);
            }
        }
    }

    private sealed record FunctionExpectation(
        string Name,
        bool SecurityDefiner,
        bool RuntimeExecute,
        bool BootstrapperExecute);

    private sealed record FunctionInfo(
        bool SecurityDefiner,
        string Owner,
        bool OwnerCanLogin,
        string Config,
        bool PublicExecute,
        bool RuntimeExecute,
        bool BootstrapperExecute);
}
