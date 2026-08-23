using System.Data;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NpgsqlTypes;
using TagEkyc.Api;
using TagEkyc.Application.Ports;
using TagEkyc.Domain;
using TagEkyc.Infrastructure.Persistence;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88B1E3ResolverReadBoundaryTests(PostgresPersistenceFixture postgres)
    : IAsyncLifetime
{
    private const string Migration = "20260724015546_Tip88B1E3ResolverReadBoundary";
    private const string PreviousMigration = "20260723052003_Tip88B33RawExportAuthorizationPersistFunction";
    private const string ExpectedModelSnapshotSha256 =
        "5F8653C3D679DBA8EC3D933192BCB4B61E180BB3243953DCED60AAD4E87E8E3C";
    private const string EligibilityFunction =
        "tagekyc.raw_export_read_authorization_eligibility_inputs(uuid,uuid,integer)";
    private const string PolicyFunction =
        "tagekyc.raw_export_read_authorization_policy_inputs(uuid,uuid,integer)";
    private const string RootHealthFunction = "tagekyc.raw_export_control_plane_root_health()";
    private static readonly Guid B34AdminPrincipal =
        Guid.Parse("88b34000-0000-5000-8000-000000000001");
    private static readonly Guid B34RecorderPrincipal =
        Guid.Parse("88b34000-0000-5000-8000-000000000002");

    private static readonly string[] E3ProtectedRuntimeDirectAccessTables =
    [
        "raw_export_policy_versions",
        "raw_export_policy_allowed_classes",
        "raw_export_policy_requirements",
        "raw_export_policy_closures",
        "raw_export_requirement_rule_sets",
        "raw_export_grants",
        "raw_export_fulfillments",
        "raw_export_policy_lifecycle",
        "verification_sessions",
        "raw_export_subject_consent_authorities",
        "raw_export_subject_consent_events",
        "raw_export_subject_consent_classes",
        "raw_export_requirement_rules",
        "raw_export_control_authorities",
    ];

    private static readonly string[] E3ForbiddenRuntimeTablePrivileges =
        ["SELECT", "INSERT", "UPDATE", "DELETE", "TRUNCATE", "REFERENCES", "TRIGGER"];

    private static readonly string[] E3DeployerBackingReadTables =
    [
        "raw_export_control_authorities",
        "raw_export_fulfillments",
        "raw_export_grants",
        "raw_export_policy_allowed_classes",
        "raw_export_policy_closures",
        "raw_export_policy_lifecycle",
        "raw_export_policy_requirements",
        "raw_export_policy_versions",
        "raw_export_requirement_rule_sets",
    ];

    private static readonly FunctionManifest[] Functions =
    [
        new(
            "raw_export_read_authorization_eligibility_inputs",
            EligibilityFunction,
            48,
            "TABLE(\"PolicyId\" uuid, \"PolicyVersion\" integer, \"EvaluatedAtUtc\" timestamp with time zone, \"PolicyExists\" boolean, \"BoundRuleSetVersion\" integer, \"CurrentRuleSetVersion\" integer, \"ClosureType\" text, \"GrantPrincipalId\" uuid, \"GrantPolicyId\" uuid, \"GrantPolicyVersion\" integer, \"GrantRevision\" integer, \"GrantEventType\" text, \"LifecyclePolicyId\" uuid, \"LifecyclePolicyVersion\" integer, \"LifecycleRevision\" integer, \"LifecycleEventType\" text, \"RequirementOrdinal\" integer, \"RequirementType\" text, \"FulfillmentEventId\" uuid, \"FulfillmentRevision\" integer, \"FulfillmentEventType\" text, \"ArtifactRef\" text, \"ArtifactVersion\" text, \"ValidFromUtc\" timestamp with time zone, \"ValidUntilUtc\" timestamp with time zone)",
            "b3852ce11556bad68ec4bb6f3ff0b8074fb9eb37e05dda45c2eafcb79c883fcf"),
        new(
            "raw_export_read_authorization_policy_inputs",
            PolicyFunction,
            43,
            "TABLE(\"PolicyId\" uuid, \"PolicyVersion\" integer, \"EvaluatedAtUtc\" timestamp with time zone, \"PolicyExists\" boolean, \"PermitTtlSeconds\" integer, \"ClosureType\" text, \"ClassOrdinal\" integer, \"RawClass\" text)",
            "9476121061bff3df52c7ae522b27f4f7b2878f70c99f5029883d180f47a39712"),
        new(
            "raw_export_control_plane_root_health",
            RootHealthFunction,
            36,
            "TABLE(\"IsHealthy\" boolean, \"StatusCode\" text)",
            "364393e456b50ec7127dd32261ee45858b2a29753450e27f914afe9f4b49bf46"),
    ];

    public Task InitializeAsync() => postgres.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task F1_runtime_has_zero_of_seven_privileges_on_all_fourteen_tables()
    {
        Assert.Equal(14, E3ProtectedRuntimeDirectAccessTables.Length);
        Assert.Equal(
            ["SELECT", "INSERT", "UPDATE", "DELETE", "TRUNCATE", "REFERENCES", "TRIGGER"],
            E3ForbiddenRuntimeTablePrivileges);

        await using var connection = await OpenAdminAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT t.table_name, p.privilege_name,
                   has_table_privilege(
                       'tagekyc_runtime',
                       'tagekyc.' || t.table_name,
                       p.privilege_name)
            FROM unnest(@tables) WITH ORDINALITY AS t(table_name, table_ordinal)
            CROSS JOIN unnest(@privileges) WITH ORDINALITY AS p(privilege_name, privilege_ordinal)
            ORDER BY t.table_ordinal, p.privilege_ordinal;
            """,
            connection);
        command.Parameters.AddWithValue("tables", E3ProtectedRuntimeDirectAccessTables);
        command.Parameters.AddWithValue("privileges", E3ForbiddenRuntimeTablePrivileges);

        var cells = new List<(string Table, string Privilege, bool HasPrivilege)>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            cells.Add((reader.GetString(0), reader.GetString(1), reader.GetBoolean(2)));
        }

        Assert.Equal(98, cells.Count);
        Assert.All(cells, cell => Assert.False(
            cell.HasPrivilege,
            $"tagekyc_runtime unexpectedly has {cell.Privilege} on {cell.Table}."));
    }

    [Fact]
    public async Task F1_runtime_direct_select_and_mutations_fail_42501_on_all_fourteen_tables()
    {
        var firstColumns = await ReadFirstColumnsAsync();
        await using var login = await CreateRuntimeLoginAsync();
        await using var connection = new NpgsqlConnection(login.ConnectionString);
        await connection.OpenAsync();

        foreach (var table in E3ProtectedRuntimeDirectAccessTables)
        {
            var qualified = $"tagekyc.{Quote(table)}";
            var column = Quote(firstColumns[table]);
            string[] statements =
            [
                $"SELECT * FROM {qualified} LIMIT 0;",
                $"INSERT INTO {qualified} DEFAULT VALUES;",
                $"UPDATE {qualified} SET {column} = {column} WHERE FALSE;",
                $"DELETE FROM {qualified} WHERE FALSE;",
                $"TRUNCATE TABLE {qualified};",
            ];
            foreach (var statement in statements)
            {
                await AssertSqlStateAsync(connection, statement, PostgresErrorCodes.InsufficientPrivilege);
            }
        }
    }

    [Fact]
    public async Task F1_each_forbidden_privilege_cell_turns_readiness_red()
    {
        await using var db = postgres.CreateDbContext();
        await BootstrapHealthyRootsAsync(db);
        await using var admin = await OpenAdminAsync();

        foreach (var table in E3ProtectedRuntimeDirectAccessTables)
        {
            foreach (var privilege in E3ForbiddenRuntimeTablePrivileges)
            {
                await ExecuteAsync(
                    admin,
                    $"GRANT {privilege} ON tagekyc.{Quote(table)} TO tagekyc_runtime;");
                try
                {
                    var exception = await Assert.ThrowsAsync<RawExportControlPlaneReadinessException>(
                        () => ValidateAsRuntimeAsync());
                    Assert.Equal(
                        RawExportControlPlaneReadinessValidator.ForbiddenTablePrivilege,
                        exception.Code);
                }
                finally
                {
                    await ExecuteAsync(
                        admin,
                        $"REVOKE {privilege} ON tagekyc.{Quote(table)} FROM tagekyc_runtime;");
                }
            }
        }
    }

    [Fact]
    public async Task F1_application_login_extra_membership_turns_readiness_role_invalid()
    {
        var extraRole = $"e3_extra_reader_{Guid.NewGuid():N}";
        await using (var db = postgres.CreateDbContext())
        {
            await BootstrapHealthyRootsAsync(db);
        }
        await using var admin = await OpenAdminAsync();
        await ExecuteAsync(
            admin,
            $"""
            CREATE ROLE {Quote(extraRole)} NOLOGIN;
            GRANT SELECT ON tagekyc.raw_export_policy_versions TO {Quote(extraRole)};
            """);
        try
        {
            await using (var login = await CreateRuntimeLoginAsync())
            {
                await ExecuteAsync(admin, $"GRANT {Quote(extraRole)} TO {Quote(login.Role)};");
                await using var db = CreateDbContext(login.ConnectionString);
                var exception = await Assert.ThrowsAsync<RawExportControlPlaneReadinessException>(
                    () => new RawExportControlPlaneReadinessValidator(db)
                        .ValidateAsync(CancellationToken.None));
                Assert.Equal(
                    RawExportControlPlaneReadinessValidator.DeploymentRoleInvalid,
                    exception.Code);
            }
        }
        finally
        {
            await ExecuteAsync(
                admin,
                $"""
                REVOKE SELECT ON tagekyc.raw_export_policy_versions FROM {Quote(extraRole)};
                DROP ROLE {Quote(extraRole)};
                """);
        }
    }

    [Fact]
    public async Task F1_forbidden_privilege_code_maps_to_http_503()
    {
        await using var db = postgres.CreateDbContext();
        await BootstrapHealthyRootsAsync(db);
        await db.Database.ExecuteSqlRawAsync(
            "GRANT SELECT ON tagekyc.raw_export_policy_versions TO tagekyc_runtime;");
        try
        {
            var (status, body) = await ReadinessAsync();
            Assert.Equal(HttpStatusCode.ServiceUnavailable, status);
            Assert.Contains(
                RawExportControlPlaneReadinessValidator.ForbiddenTablePrivilege,
                body,
                StringComparison.Ordinal);
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync(
                "REVOKE SELECT ON tagekyc.raw_export_policy_versions FROM tagekyc_runtime;");
        }
    }

    [Fact]
    public async Task R1_active_set_role_from_elevated_session_is_rejected_first()
    {
        await using var login = await CreateRuntimeLoginAsync();
        await using var db = postgres.CreateDbContext();
        await db.Database.OpenConnectionAsync();
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        await ExecuteAsync(connection, $"SET ROLE {Quote(login.Role)};");
        try
        {
            var exception = await Assert.ThrowsAsync<RawExportControlPlaneReadinessException>(
                () => new RawExportControlPlaneReadinessValidator(db)
                    .ValidateAsync(CancellationToken.None));
            Assert.Equal(
                RawExportControlPlaneReadinessValidator.DeploymentRoleInvalid,
                exception.Code);
        }
        finally
        {
            await ExecuteAsync(connection, "RESET ROLE;");
        }
    }

    [Fact]
    public void R1_server_version_control_seam_rejects_pre_16()
    {
        var method = typeof(RawExportControlPlaneReadinessValidator).GetMethod(
            "ValidateServerVersion",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var target = Assert.Throws<TargetInvocationException>(
            () => method!.Invoke(null, [159999]));
        var exception = Assert.IsType<RawExportControlPlaneReadinessException>(
            target.InnerException);
        Assert.Equal(
            RawExportControlPlaneReadinessValidator.DeploymentRoleInvalid,
            exception.Code);

        method!.Invoke(null, [160000]);
    }

    [Fact]
    public async Task R1_isolated_role_manifests_edges_and_cycle_handling_are_exact()
    {
        await using var isolated = await IsolatedPostgres.CreateAsync();
        await using (var setup = CreateDbContext(isolated.ConnectionString))
        {
            await BootstrapHealthyRootsAsync(setup);
        }
        await using var login = await CreateRuntimeLoginAsync(isolated.ConnectionString);
        await using var application = CreateDbContext(login.ConnectionString);
        await application.Database.OpenConnectionAsync();
        await using var admin = new NpgsqlConnection(isolated.ConnectionString);
        await admin.OpenAsync();
        await AssertReadinessCodeAsync(application, expectedCode: null);

        var capabilityRoles = new[]
        {
            "tagekyc_runtime",
            "tagekyc_raw_export_deployer",
            "tagekyc_raw_export_bootstrapper",
        };
        foreach (var role in capabilityRoles)
        {
            var renamed = $"{role}_e3_probe";
            await ExecuteAsync(admin, $"ALTER ROLE {role} RENAME TO {renamed};");
            try
            {
                await AssertReadinessCodeAsync(
                    application,
                    RawExportControlPlaneReadinessValidator.DeploymentRoleInvalid);
            }
            finally
            {
                await ExecuteAsync(admin, $"ALTER ROLE {renamed} RENAME TO {role};");
            }
        }

        var invalidCapabilityAttributes = new[]
        {
            "LOGIN",
            "NOINHERIT",
            "SUPERUSER",
            "CREATEDB",
            "CREATEROLE",
            "REPLICATION",
            "BYPASSRLS",
        };
        foreach (var role in capabilityRoles)
        {
            foreach (var invalidAttribute in invalidCapabilityAttributes)
            {
                await ExecuteAsync(admin, $"ALTER ROLE {role} {invalidAttribute};");
                try
                {
                    await AssertReadinessCodeAsync(
                        application,
                        RawExportControlPlaneReadinessValidator.DeploymentRoleInvalid);
                }
                finally
                {
                    await ExecuteAsync(
                        admin,
                        $"ALTER ROLE {role} NOLOGIN INHERIT NOSUPERUSER NOCREATEDB " +
                        "NOCREATEROLE NOREPLICATION NOBYPASSRLS;");
                }
            }
        }

        var invalidApplicationAttributes = new[]
        {
            "NOLOGIN",
            "NOINHERIT",
            "CREATEDB",
            "CREATEROLE",
            "REPLICATION",
            "BYPASSRLS",
        };
        foreach (var invalidAttribute in invalidApplicationAttributes)
        {
            await ExecuteAsync(admin, $"ALTER ROLE {Quote(login.Role)} {invalidAttribute};");
            try
            {
                await AssertReadinessCodeAsync(
                    application,
                    RawExportControlPlaneReadinessValidator.DeploymentRoleInvalid);
            }
            finally
            {
                await ExecuteAsync(
                    admin,
                    $"ALTER ROLE {Quote(login.Role)} LOGIN INHERIT NOSUPERUSER NOCREATEDB " +
                    "NOCREATEROLE NOREPLICATION NOBYPASSRLS;");
            }
        }

        await ExecuteAsync(admin, $"ALTER ROLE {Quote(login.Role)} SUPERUSER;");
        try
        {
            await AssertReadinessCodeAsync(
                application,
                RawExportControlPlaneReadinessValidator.EventTableMutationPrivilege);
        }
        finally
        {
            await ExecuteAsync(admin, $"ALTER ROLE {Quote(login.Role)} NOSUPERUSER;");
        }

        var safeGrant =
            $"GRANT tagekyc_runtime TO {Quote(login.Role)} " +
            "WITH ADMIN FALSE, INHERIT TRUE, SET FALSE;";
        var edgeMutations = new[]
        {
            $"GRANT tagekyc_runtime TO {Quote(login.Role)} " +
            "WITH ADMIN TRUE, INHERIT TRUE, SET FALSE;",
            $"GRANT tagekyc_runtime TO {Quote(login.Role)} " +
            "WITH ADMIN FALSE, INHERIT FALSE, SET FALSE;",
            $"GRANT tagekyc_runtime TO {Quote(login.Role)} " +
            "WITH ADMIN FALSE, INHERIT TRUE, SET TRUE;",
        };
        foreach (var mutation in edgeMutations)
        {
            await ExecuteAsync(admin, mutation);
            try
            {
                await AssertReadinessCodeAsync(
                    application,
                    RawExportControlPlaneReadinessValidator.DeploymentRoleInvalid);
            }
            finally
            {
                await ExecuteAsync(admin, safeGrant);
            }
        }

        var alternateGrantor = $"e3_membership_grantor_{Guid.NewGuid():N}";
        await ExecuteAsync(
            admin,
            $"""
            CREATE ROLE {Quote(alternateGrantor)} NOLOGIN;
            GRANT tagekyc_runtime TO {Quote(alternateGrantor)}
                WITH ADMIN TRUE, INHERIT TRUE, SET FALSE;
            SET ROLE {Quote(alternateGrantor)};
            GRANT tagekyc_runtime TO {Quote(login.Role)}
                WITH ADMIN FALSE, INHERIT TRUE, SET FALSE;
            RESET ROLE;
            """);
        try
        {
            Assert.Equal(
                2L,
                Convert.ToInt64(await ScalarAsync(
                    admin,
                    $"""
                    SELECT count(*)
                    FROM pg_auth_members AS membership
                    WHERE membership.member = '{login.Role}'::regrole::oid
                      AND membership.roleid = 'tagekyc_runtime'::regrole::oid;
                    """)));
            await AssertReadinessCodeAsync(
                application,
                RawExportControlPlaneReadinessValidator.DeploymentRoleInvalid);
        }
        finally
        {
            await ExecuteAsync(
                admin,
                $"""
                SET ROLE {Quote(alternateGrantor)};
                REVOKE tagekyc_runtime FROM {Quote(login.Role)};
                RESET ROLE;
                REVOKE tagekyc_runtime FROM {Quote(alternateGrantor)};
                DROP ROLE {Quote(alternateGrantor)};
                """);
        }

        var bridge = $"e3_membership_bridge_{Guid.NewGuid():N}";
        await ExecuteAsync(
            admin,
            $"""
            CREATE ROLE {Quote(bridge)} NOLOGIN;
            REVOKE tagekyc_runtime FROM {Quote(login.Role)};
            GRANT tagekyc_runtime TO {Quote(bridge)}
                WITH ADMIN FALSE, INHERIT TRUE, SET FALSE;
            GRANT {Quote(bridge)} TO {Quote(login.Role)}
                WITH ADMIN FALSE, INHERIT TRUE, SET FALSE;
            """);
        try
        {
            await AssertReadinessCodeAsync(
                application,
                RawExportControlPlaneReadinessValidator.DeploymentRoleInvalid);
        }
        finally
        {
            await ExecuteAsync(
                admin,
                $"""
                REVOKE {Quote(bridge)} FROM {Quote(login.Role)};
                REVOKE tagekyc_runtime FROM {Quote(bridge)};
                DROP ROLE {Quote(bridge)};
                {safeGrant}
                """);
        }

        var runtimeOutgoing = $"e3_runtime_outgoing_{Guid.NewGuid():N}";
        await ExecuteAsync(
            admin,
            $"""
            CREATE ROLE {Quote(runtimeOutgoing)} NOLOGIN;
            GRANT {Quote(runtimeOutgoing)} TO tagekyc_runtime
                WITH ADMIN FALSE, INHERIT TRUE, SET FALSE;
            """);
        try
        {
            await AssertReadinessCodeAsync(
                application,
                RawExportControlPlaneReadinessValidator.DeploymentRoleInvalid);
        }
        finally
        {
            await ExecuteAsync(
                admin,
                $"""
                REVOKE {Quote(runtimeOutgoing)} FROM tagekyc_runtime;
                DROP ROLE {Quote(runtimeOutgoing)};
                """);
        }

        var cycleA = $"e3_cycle_a_{Guid.NewGuid():N}";
        var cycleB = $"e3_cycle_b_{Guid.NewGuid():N}";
        await ExecuteAsync(
            admin,
            $"""
            CREATE ROLE {Quote(cycleA)} NOLOGIN;
            CREATE ROLE {Quote(cycleB)} NOLOGIN;
            GRANT {Quote(cycleA)} TO {Quote(cycleB)};
            """);
        try
        {
            var cycle = await Assert.ThrowsAsync<PostgresException>(
                () => ExecuteAsync(
                    admin,
                    $"GRANT {Quote(cycleB)} TO {Quote(cycleA)};"));
            Assert.Equal("0LP01", cycle.SqlState);
        }
        finally
        {
            await ExecuteAsync(
                admin,
                $"""
                REVOKE {Quote(cycleA)} FROM {Quote(cycleB)};
                DROP ROLE {Quote(cycleB)};
                DROP ROLE {Quote(cycleA)};
                """);
        }

        await AssertReadinessCodeAsync(application, expectedCode: null);
    }

    [Fact]
    public async Task R2_each_deployer_backing_read_is_required_and_capability_drift_is_rejected()
    {
        await using (var setup = postgres.CreateDbContext())
        {
            await BootstrapHealthyRootsAsync(setup);
        }

        await using var login = await CreateRuntimeLoginAsync();
        await using var admin = await OpenAdminAsync();
        foreach (var table in E3DeployerBackingReadTables)
        {
            await ExecuteAsync(
                admin,
                $"REVOKE SELECT ON tagekyc.{Quote(table)} FROM tagekyc_raw_export_deployer;");
            try
            {
                Assert.False(Convert.ToBoolean(await ScalarAsync(
                    admin,
                    $"SELECT has_table_privilege('tagekyc_raw_export_deployer'," +
                    $"'tagekyc.{table}','SELECT');")));
                await AssertReadinessCodeAsync(
                    login.ConnectionString,
                    RawExportControlPlaneReadinessValidator.FunctionAclInvalid);
            }
            finally
            {
                await ExecuteAsync(
                    admin,
                    $"GRANT SELECT ON tagekyc.{Quote(table)} TO tagekyc_raw_export_deployer;");
            }

            Assert.True(Convert.ToBoolean(await ScalarAsync(
                admin,
                $"SELECT has_table_privilege('tagekyc_raw_export_deployer'," +
                $"'tagekyc.{table}','SELECT');")));
        }

        await ExecuteAsync(
            admin,
            "GRANT UPDATE ON tagekyc.raw_export_policy_allowed_classes " +
            "TO tagekyc_raw_export_deployer;");
        try
        {
            await AssertReadinessCodeAsync(
                login.ConnectionString,
                RawExportControlPlaneReadinessValidator.FunctionAclInvalid);
        }
        finally
        {
            await ExecuteAsync(
                admin,
                "REVOKE UPDATE ON tagekyc.raw_export_policy_allowed_classes " +
                "FROM tagekyc_raw_export_deployer;");
        }

        await AssertReadinessCodeAsync(login.ConnectionString, expectedCode: null);
    }

    [Fact]
    public async Task R3_alternate_function_acl_grantor_turns_readiness_red_in_isolated_cluster()
    {
        await using var isolated = await IsolatedPostgres.CreateAsync();
        await using (var setup = CreateDbContext(isolated.ConnectionString))
        {
            await BootstrapHealthyRootsAsync(setup);
        }
        await using var login = await CreateRuntimeLoginAsync(isolated.ConnectionString);
        await AssertReadinessCodeAsync(login.ConnectionString, expectedCode: null);

        var grantor = $"e3_alt_grantor_{Guid.NewGuid():N}";
        await using var admin = new NpgsqlConnection(isolated.ConnectionString);
        await admin.OpenAsync();
        await ExecuteAsync(
            admin,
            $"""
            CREATE ROLE {Quote(grantor)} NOLOGIN;
            GRANT USAGE ON SCHEMA tagekyc TO {Quote(grantor)};
            GRANT EXECUTE ON FUNCTION {RootHealthFunction}
                TO {Quote(grantor)} WITH GRANT OPTION;
            SET ROLE {Quote(grantor)};
            GRANT EXECUTE ON FUNCTION {RootHealthFunction} TO tagekyc_runtime;
            RESET ROLE;
            """);
        try
        {
            Assert.Equal(
                2L,
                Convert.ToInt64(await ScalarAsync(
                    admin,
                    $"""
                    SELECT count(*)
                    FROM pg_proc AS function
                    CROSS JOIN LATERAL aclexplode(function.proacl) AS acl
                    WHERE function.oid = '{RootHealthFunction}'::regprocedure
                      AND acl.grantee = 'tagekyc_runtime'::regrole::oid;
                    """)));
            Assert.Equal(
                1L,
                Convert.ToInt64(await ScalarAsync(
                    admin,
                    $"""
                    SELECT count(*)
                    FROM pg_proc AS function
                    CROSS JOIN LATERAL aclexplode(function.proacl) AS acl
                    WHERE function.oid = '{RootHealthFunction}'::regprocedure
                      AND acl.grantee = 'tagekyc_runtime'::regrole::oid
                      AND acl.grantor = '{grantor}'::regrole::oid;
                    """)));
            await AssertReadinessCodeAsync(
                login.ConnectionString,
                RawExportControlPlaneReadinessValidator.FunctionAclInvalid);
        }
        finally
        {
            await ExecuteAsync(
                admin,
                $"""
                REVOKE EXECUTE ON FUNCTION {RootHealthFunction}
                    FROM {Quote(grantor)} CASCADE;
                REVOKE USAGE ON SCHEMA tagekyc FROM {Quote(grantor)};
                DROP ROLE {Quote(grantor)};
                """);
        }

        Assert.Equal(
            1L,
            Convert.ToInt64(await ScalarAsync(
                admin,
                $"""
                SELECT count(*)
                FROM pg_proc AS function
                CROSS JOIN LATERAL aclexplode(function.proacl) AS acl
                WHERE function.oid = '{RootHealthFunction}'::regprocedure
                  AND acl.grantee = 'tagekyc_runtime'::regrole::oid;
                """)));
        await AssertReadinessCodeAsync(login.ConnectionString, expectedCode: null);
    }

    [Fact]
    public async Task R3_default_acl_grantee_and_alternate_grantor_each_abort_e3_apply()
    {
        await using var isolated = await IsolatedPostgres.CreateAsync();
        await using var db = CreateDbContext(isolated.ConnectionString);
        var migrator = db.GetService<IMigrator>();
        var cases = new[]
        {
            (Target: $"e3_default_acl_{Guid.NewGuid():N}", CreateRole: true),
            (Target: "tagekyc_runtime", CreateRole: false),
        };

        foreach (var testCase in cases)
        {
            await migrator.MigrateAsync(PreviousMigration);
            await using var admin = new NpgsqlConnection(isolated.ConnectionString);
            await admin.OpenAsync();
            var beforeDefaultAcl = Convert.ToString(await ScalarAsync(
                admin,
                """
                SELECT COALESCE(string_agg(
                    defaclrole::regrole::text || '|' ||
                    COALESCE(namespace.nspname, '') || '|' ||
                    defaclobjtype::text || '|' || defaclacl::text,
                    E'\n' ORDER BY defaclrole, namespace.nspname, defaclobjtype), '')
                FROM pg_default_acl
                LEFT JOIN pg_namespace AS namespace
                  ON namespace.oid = defaclnamespace
                WHERE defaclrole = 'tagekyc'::regrole::oid
                  AND namespace.nspname = 'tagekyc';
                """)) ?? string.Empty;

            if (testCase.CreateRole)
            {
                await ExecuteAsync(admin, $"CREATE ROLE {Quote(testCase.Target)} NOLOGIN;");
            }

            await ExecuteAsync(
                admin,
                $"""
                ALTER DEFAULT PRIVILEGES FOR ROLE tagekyc IN SCHEMA tagekyc
                    GRANT EXECUTE ON FUNCTIONS TO {Quote(testCase.Target)};
                CREATE FUNCTION tagekyc.e3_default_acl_probe()
                    RETURNS integer
                    LANGUAGE sql
                    AS 'SELECT 1';
                """);
            try
            {
                Assert.Equal(
                    1L,
                    Convert.ToInt64(await ScalarAsync(
                        admin,
                        $"""
                        SELECT count(*)
                        FROM pg_proc AS function
                        CROSS JOIN LATERAL aclexplode(function.proacl) AS acl
                        WHERE function.oid =
                                  'tagekyc.e3_default_acl_probe()'::regprocedure
                          AND acl.grantee = '{testCase.Target}'::regrole::oid
                          AND acl.grantor = 'tagekyc'::regrole::oid
                          AND acl.privilege_type = 'EXECUTE';
                        """)));
                await ExecuteAsync(
                    admin,
                    "DROP FUNCTION tagekyc.e3_default_acl_probe();");

                var apply = await Assert.ThrowsAsync<PostgresException>(
                    () => migrator.MigrateAsync(Migration));
                Assert.Equal(PostgresErrorCodes.RaiseException, apply.SqlState);
                Assert.Equal("TIP88B1E3_FUNCTION_ACL_INVALID", apply.MessageText);
                Assert.Equal(PreviousMigration, await CurrentMigrationAsync(db));
                Assert.Equal(
                    0L,
                    Convert.ToInt64(await ScalarAsync(
                        admin,
                        """
                        SELECT count(*)
                        FROM pg_proc AS function
                        JOIN pg_namespace AS namespace
                          ON namespace.oid = function.pronamespace
                        WHERE namespace.nspname = 'tagekyc'
                          AND function.proname IN (
                              'raw_export_read_authorization_eligibility_inputs',
                              'raw_export_read_authorization_policy_inputs',
                              'raw_export_control_plane_root_health');
                        """)));
            }
            finally
            {
                await ExecuteAsync(
                    admin,
                    "DROP FUNCTION IF EXISTS tagekyc.e3_default_acl_probe();");
                await ExecuteAsync(
                    admin,
                    $"""
                    ALTER DEFAULT PRIVILEGES FOR ROLE tagekyc IN SCHEMA tagekyc
                        REVOKE EXECUTE ON FUNCTIONS FROM {Quote(testCase.Target)};
                    """);
                if (testCase.CreateRole)
                {
                    await ExecuteAsync(admin, $"DROP ROLE {Quote(testCase.Target)};");
                }
            }

            Assert.Equal(
                beforeDefaultAcl,
                Convert.ToString(await ScalarAsync(
                    admin,
                    """
                    SELECT COALESCE(string_agg(
                        defaclrole::regrole::text || '|' ||
                        COALESCE(namespace.nspname, '') || '|' ||
                        defaclobjtype::text || '|' || defaclacl::text,
                        E'\n' ORDER BY defaclrole, namespace.nspname, defaclobjtype), '')
                    FROM pg_default_acl
                    LEFT JOIN pg_namespace AS namespace
                      ON namespace.oid = defaclnamespace
                    WHERE defaclrole = 'tagekyc'::regrole::oid
                      AND namespace.nspname = 'tagekyc';
                    """)) ?? string.Empty);
            await migrator.MigrateAsync(Migration);
            Assert.Equal(Migration, await CurrentMigrationAsync(db));
        }
    }

    [Fact]
    public async Task R4_every_invalid_fulfillment_cell_fails_in_materialization_with_zero_authorization_residue()
    {
        var cases = new[]
        {
            FulfillmentCase("null-event-id", "NULL::uuid"),
            FulfillmentCase(
                "empty-event-id",
                "'00000000-0000-0000-0000-000000000000'::uuid"),
            FulfillmentCase("null-revision", revision: "NULL::integer"),
            FulfillmentCase("zero-revision", revision: "0"),
            FulfillmentCase("negative-revision", revision: "-1"),
            FulfillmentCase("null-event-type", eventType: "NULL::text"),
            FulfillmentCase("unknown-event-type", eventType: "'Unknown'::text"),
            FulfillmentCase("accepted-null-artifact-ref", artifactRef: "NULL::text"),
            FulfillmentCase("accepted-blank-artifact-ref", artifactRef: "'   '::text"),
            FulfillmentCase("accepted-null-artifact-version", artifactVersion: "NULL::text"),
            FulfillmentCase("accepted-blank-artifact-version", artifactVersion: "'   '::text"),
            FulfillmentCase("accepted-null-valid-from", validFrom: "NULL::timestamptz"),
            FulfillmentCase(
                "accepted-invalid-end-ordering",
                validFrom: "transaction_timestamp()",
                validUntil: "transaction_timestamp()"),
            FulfillmentCase(
                "withdrawn-artifact-ref-present",
                eventType: "'Withdrawn'::text",
                artifactRef: "'artifact:r4'::text",
                artifactVersion: "NULL::text",
                validFrom: "NULL::timestamptz"),
            FulfillmentCase(
                "withdrawn-artifact-version-present",
                eventType: "'Withdrawn'::text",
                artifactRef: "NULL::text",
                artifactVersion: "'v1'::text",
                validFrom: "NULL::timestamptz"),
            FulfillmentCase(
                "withdrawn-valid-from-present",
                eventType: "'Withdrawn'::text",
                artifactRef: "NULL::text",
                artifactVersion: "NULL::text",
                validFrom: "transaction_timestamp()"),
            FulfillmentCase(
                "withdrawn-valid-until-present",
                eventType: "'Withdrawn'::text",
                artifactRef: "NULL::text",
                artifactVersion: "NULL::text",
                validFrom: "NULL::timestamptz",
                validUntil: "transaction_timestamp()"),
        };

        await using var admin = await OpenAdminAsync();
        var originalDefinition = (string)(await ScalarAsync(
            admin,
            $"SELECT pg_catalog.pg_get_functiondef('{EligibilityFunction}'::regprocedure);")
            ?? throw new InvalidOperationException("E3 eligibility function definition missing."));
        var authorizationCounts = await ReadAuthorizationTableCountsAsync(admin);

        try
        {
            foreach (var shape in cases)
            {
                await ExecuteAsync(admin, BuildInvalidFulfillmentProjectionFunction(shape));
                var policyId = Guid.NewGuid();
                Guid sessionId;
                await using (var setup = postgres.CreateDbContext())
                {
                    sessionId = await Tip88B34AuthorizationEngineTests.SeedCompletedSessionAsync(
                        setup,
                        Tip88B34AuthorizationEngineTests.ClientApplicationId,
                        $"subject:r4:{shape.Name}");
                    await Tip88B34AuthorizationEngineTests.SeedActivePolicyAsync(
                        setup,
                        policyId,
                        [RawExportRawClass.LiveSelfieImage],
                        300,
                        DateTimeOffset.UtcNow.AddMinutes(5));
                    await Tip88B34AuthorizationEngineTests.SeedEffectiveConsentAsync(
                        setup,
                        sessionId,
                        policyId,
                        [RawExportRawClass.LiveSelfieImage],
                        DateTimeOffset.UtcNow.AddMinutes(4));
                }

                await using var db = postgres.CreateDbContext();
                var exception = await Assert.ThrowsAsync<RawExportAuthorizationException>(
                    () => Tip88B34AuthorizationEngineTests.CreateRepository(db)
                        .AuthorizeExportAsync(
                            Tip88B34AuthorizationEngineTests.Command(
                                sessionId,
                                policyId,
                                $"r4-{shape.Name}")));
                Assert.Equal("RAW_EXPORT_AUTHORIZATION_INVARIANT_FAILURE", exception.Code);
                Assert.Equal(
                    authorizationCounts,
                    await ReadAuthorizationTableCountsAsync(admin));
            }
        }
        finally
        {
            await ExecuteAsync(admin, originalDefinition);
        }

        Assert.Equal(
            originalDefinition,
            (string)(await ScalarAsync(
                admin,
                $"SELECT pg_catalog.pg_get_functiondef('{EligibilityFunction}'::regprocedure);")
                ?? string.Empty));
    }

    [Fact]
    public async Task R5_input_immediate_two_runtime_calls_and_successful_return_deferred_mode_are_proven()
    {
        var fixture = await SeedConstraintModeFixtureAsync(3);

        await using (var login = await CreateRuntimeLoginAsync())
        await using (var runtime = new NpgsqlConnection(login.ConnectionString))
        {
            await runtime.OpenAsync();
            await using var transaction = await runtime.BeginTransactionAsync();
            await SetActorAsync(runtime, transaction, fixture.RecorderPrincipalId.ToString("D"));
            await ExecuteAsync(
                runtime,
                "SET CONSTRAINTS tagekyc.tr_raw_export_subject_consent_granted_classes_required IMMEDIATE;");
            Assert.Equal(
                1,
                await CallGrantedConsentFunctionAsync(
                    runtime,
                    transaction,
                    fixture.Sessions[0].SessionId,
                    fixture.PolicyId,
                    "r5-runtime-first"));
            Assert.Equal(
                1,
                await CallGrantedConsentFunctionAsync(
                    runtime,
                    transaction,
                    fixture.Sessions[1].SessionId,
                    fixture.PolicyId,
                    "r5-runtime-second"));
            await transaction.CommitAsync();
        }

        await using var admin = await OpenAdminAsync();
        await ExecuteAsync(admin, "SET ROLE tagekyc_raw_export_deployer;");
        await using var probeTransaction = await admin.BeginTransactionAsync();
        await SetActorAsync(
            admin,
            probeTransaction,
            fixture.RecorderPrincipalId.ToString("D"));
        await ExecuteAsync(
            admin,
            "SET CONSTRAINTS tagekyc.tr_raw_export_subject_consent_granted_classes_required IMMEDIATE;");
        Assert.Equal(
            1,
            await CallGrantedConsentFunctionAsync(
                admin,
                probeTransaction,
                fixture.Sessions[2].SessionId,
                fixture.PolicyId,
                "r5-return-mode-source"));
        await InsertGrantedConsentParentAndClassAsync(
            admin,
            probeTransaction,
            fixture.ProbeSession,
            fixture.PolicyId,
            fixture.RecorderPrincipalId);
        await ExecuteAsync(
            admin,
            """
            SET CONSTRAINTS tagekyc.tr_raw_export_subject_consent_granted_classes_required IMMEDIATE;
            SET CONSTRAINTS tagekyc.tr_raw_export_subject_consent_granted_classes_required DEFERRED;
            """);
        await probeTransaction.CommitAsync();
        await ExecuteAsync(admin, "RESET ROLE;");
    }

    [Fact]
    public async Task R5_entry_normalization_mutation_turns_input_immediate_gate_red()
    {
        var fixture = await SeedConstraintModeFixtureAsync(1);
        await using var admin = await OpenAdminAsync();
        var originalDefinition = await ReadFunctionDefinitionAsync(
            admin,
            "tagekyc.raw_export_append_subject_consent_granted(uuid,uuid,integer,text[],text,text,text,text,timestamptz)");
        var entryPattern = new System.Text.RegularExpressions.Regex(
            @"(?m)^\s*SET CONSTRAINTS\s+tagekyc\.tr_raw_export_subject_consent_granted_classes_required\s+DEFERRED;\s*");
        Assert.Equal(2, entryPattern.Matches(originalDefinition).Count);
        var mutatedDefinition = entryPattern.Replace(originalDefinition, string.Empty, 1);
        Assert.NotEqual(originalDefinition, mutatedDefinition);

        try
        {
            await ExecuteAsync(admin, mutatedDefinition);
            await using var login = await CreateRuntimeLoginAsync();
            await using var runtime = new NpgsqlConnection(login.ConnectionString);
            await runtime.OpenAsync();
            await using var transaction = await runtime.BeginTransactionAsync();
            await SetActorAsync(runtime, transaction, fixture.RecorderPrincipalId.ToString("D"));
            await ExecuteAsync(
                runtime,
                "SET CONSTRAINTS tagekyc.tr_raw_export_subject_consent_granted_classes_required IMMEDIATE;");
            var exception = await Assert.ThrowsAsync<PostgresException>(
                () => CallGrantedConsentFunctionAsync(
                    runtime,
                    transaction,
                    fixture.Sessions[0].SessionId,
                    fixture.PolicyId,
                    "r5-entry-mutation"));
            Assert.Equal(PostgresErrorCodes.RaiseException, exception.SqlState);
            Assert.Equal("RAW_EXPORT_SUBJECT_CONSENT_CLASSES_REQUIRED", exception.MessageText);
            await transaction.RollbackAsync();
        }
        finally
        {
            await ExecuteAsync(admin, originalDefinition);
        }

        Assert.Equal(
            originalDefinition,
            await ReadFunctionDefinitionAsync(
                admin,
                "tagekyc.raw_export_append_subject_consent_granted(uuid,uuid,integer,text[],text,text,text,text,timestamptz)"));
    }

    [Fact]
    public async Task R5_successful_return_restore_mutation_turns_second_record_gate_red()
    {
        var fixture = await SeedConstraintModeFixtureAsync(1);
        await using var admin = await OpenAdminAsync();
        var signature =
            "tagekyc.raw_export_append_subject_consent_granted(uuid,uuid,integer,text[],text,text,text,text,timestamptz)";
        var originalDefinition = await ReadFunctionDefinitionAsync(admin, signature);
        var returnRestorePattern = new System.Text.RegularExpressions.Regex(
            @"(?m)^\s*SET CONSTRAINTS\s+tagekyc\.tr_raw_export_subject_consent_granted_classes_required\s+DEFERRED;\s*(?=RETURN next_revision;)");
        Assert.Single(returnRestorePattern.Matches(originalDefinition));
        var mutatedDefinition =
            returnRestorePattern.Replace(originalDefinition, string.Empty, 1);
        Assert.NotEqual(originalDefinition, mutatedDefinition);

        try
        {
            await ExecuteAsync(admin, mutatedDefinition);
            await ExecuteAsync(admin, "SET ROLE tagekyc_raw_export_deployer;");
            await using var transaction = await admin.BeginTransactionAsync();
            await SetActorAsync(admin, transaction, fixture.RecorderPrincipalId.ToString("D"));
            Assert.Equal(
                1,
                await CallGrantedConsentFunctionAsync(
                    admin,
                    transaction,
                    fixture.Sessions[0].SessionId,
                    fixture.PolicyId,
                    "r5-return-mutation"));
            var exception = await Assert.ThrowsAsync<PostgresException>(
                () => InsertGrantedConsentParentOnlyAsync(
                    admin,
                    transaction,
                    fixture.ProbeSession,
                    fixture.PolicyId,
                    fixture.RecorderPrincipalId));
            Assert.Equal(PostgresErrorCodes.RaiseException, exception.SqlState);
            Assert.Equal("RAW_EXPORT_SUBJECT_CONSENT_CLASSES_REQUIRED", exception.MessageText);
            await transaction.RollbackAsync();
            await ExecuteAsync(admin, "RESET ROLE;");
        }
        finally
        {
            await ExecuteAsync(admin, "RESET ROLE;");
            await ExecuteAsync(admin, originalDefinition);
        }

        Assert.Equal(
            originalDefinition,
            await ReadFunctionDefinitionAsync(admin, signature));
    }

    [Fact]
    public async Task F1_landed_security_definer_capabilities_still_succeed()
    {
        var policy = Guid.NewGuid();
        Guid session;
        await using (var setup = postgres.CreateDbContext())
        {
            session = await Tip88B34AuthorizationEngineTests.SeedCompletedSessionAsync(
                setup,
                Tip88B34AuthorizationEngineTests.ClientApplicationId,
                "subject:f1-landed-capabilities");
            await Tip88B34AuthorizationEngineTests.SeedActivePolicyAsync(
                setup,
                policy,
                [RawExportRawClass.LiveSelfieImage],
                300,
                DateTimeOffset.UtcNow.AddMinutes(5));
            await new EfRawExportSubjectConsentRepository(setup).GrantConsentAuthorityAsync(new(
                B34AdminPrincipal,
                B34RecorderPrincipal,
                Tip88B34AuthorizationEngineTests.ClientApplicationId,
                RawExportSubjectConsentAuthorityType.SubjectConsentRecorder,
                ExpectedRevision: 0,
                "decision:e3-f1-consent-authority"));
        }

        await using (var login = await CreateRuntimeLoginAsync())
        await using (var runtime = CreateDbContext(login.ConnectionString))
        {
            await AssertRuntimeLoginShapeAsync(runtime, login.Role);
            var lifecycleRevision = await new EfRawExportControlPlaneRepository(runtime)
                .SuspendPolicyAsync(new(
                    B34AdminPrincipal,
                    policy,
                    1,
                    ExpectedRevision: 1,
                    "decision:e3-f1-runtime-suspend"));
            Assert.Equal(2, lifecycleRevision);

            var consentRepository = new EfRawExportSubjectConsentRepository(runtime);
            var consent = await consentRepository.RecordSubjectConsentGrantedAsync(new(
                B34RecorderPrincipal,
                session,
                policy,
                1,
                new HashSet<RawExportRawClass> { RawExportRawClass.LiveSelfieImage },
                "consent-text:e3-f1",
                "sha256:e3-f1-consent",
                "external:e3-f1-consent",
                "decision:e3-f1-consent",
                DateTimeOffset.UtcNow.AddMinutes(4)));
            Assert.Equal(RawExportSubjectConsentState.Effective, consent.State);

            await using var transaction = await runtime.Database.BeginTransactionAsync();
            await runtime.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_catalog.set_config('tagekyc.actor_principal_id',{B34RecorderPrincipal.ToString("D")},true);");
            var resolved = await consentRepository.ResolveSubjectExportConsentForAuthorizationAsync(
                session,
                policy,
                1);
            Assert.Equal(RawExportSubjectConsentState.Effective, resolved.State);
            await transaction.CommitAsync();
        }

        var result = await RunAuthorizedAsRuntimeAsync("f1-capabilities");
        Assert.Equal(RawExportAuthorizationOutcome.Authorized, result.Decision.Outcome);
        Assert.NotNull(result.Permit);
    }

    [Fact]
    public async Task F2_intended_function_names_are_within_63_bytes_and_round_trip_exactly()
    {
        Assert.All(Functions, function =>
            Assert.InRange(Encoding.UTF8.GetByteCount(function.Name), 1, 63));

        await using var connection = await OpenAdminAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT p.proname
            FROM pg_proc AS p
            JOIN pg_namespace AS n ON n.oid = p.pronamespace
            WHERE n.nspname = 'tagekyc'
              AND p.proname = ANY(@names)
            ORDER BY p.proname;
            """,
            connection);
        command.Parameters.AddWithValue("names", Functions.Select(item => item.Name).ToArray());
        var actual = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            actual.Add(reader.GetString(0));
        }

        Assert.Equal(
            Functions.Select(item => item.Name).Order(StringComparer.Ordinal),
            actual);
    }

    [Fact]
    public async Task F2_each_e3_function_has_exact_signature_result_language_body_owner_search_path_and_acl()
    {
        await using var connection = await OpenAdminAsync();
        foreach (var expected in Functions)
        {
            await using var command = new NpgsqlCommand(
                """
                SELECT p.proname,
                       pg_get_function_identity_arguments(p.oid),
                       pg_get_function_result(p.oid),
                       l.lanname,
                       r.rolname,
                       r.rolcanlogin,
                       p.prosecdef,
                       p.proconfig,
                       p.prosrc,
                       has_function_privilege('tagekyc_runtime', p.oid, 'EXECUTE'),
                       has_function_privilege('public', p.oid, 'EXECUTE'),
                       has_function_privilege('tagekyc_raw_export_bootstrapper', p.oid, 'EXECUTE'),
                       COALESCE((
                           SELECT array_agg(
                               COALESCE(grantor.rolname, acl.grantor::text) || ':' ||
                               CASE
                                   WHEN acl.grantee = 0 THEN 'PUBLIC'
                                   ELSE COALESCE(grantee.rolname, acl.grantee::text)
                               END || ':' ||
                               acl.privilege_type || ':' ||
                               acl.is_grantable::text
                               ORDER BY
                                   COALESCE(grantor.rolname, acl.grantor::text),
                                   CASE
                                       WHEN acl.grantee = 0 THEN 'PUBLIC'
                                       ELSE COALESCE(grantee.rolname, acl.grantee::text)
                                   END,
                                   acl.privilege_type,
                                   acl.is_grantable)
                           FROM aclexplode(COALESCE(p.proacl, acldefault('f', p.proowner))) AS acl
                           LEFT JOIN pg_roles AS grantor ON grantor.oid = acl.grantor
                           LEFT JOIN pg_roles AS grantee ON grantee.oid = acl.grantee
                           WHERE acl.grantee <> p.proowner
                       ), ARRAY[]::text[])
                FROM pg_proc AS p
                JOIN pg_namespace AS n ON n.oid = p.pronamespace
                JOIN pg_language AS l ON l.oid = p.prolang
                JOIN pg_roles AS r ON r.oid = p.proowner
                WHERE n.nspname = 'tagekyc' AND p.proname = @name;
                """,
                connection);
            command.Parameters.AddWithValue("name", expected.Name);
            await using var reader = await command.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.Equal(expected.Name, reader.GetString(0));
            Assert.Equal(
                expected == Functions[2]
                    ? string.Empty
                    : "principal_id uuid, policy_id uuid, policy_version integer",
                reader.GetString(1));
            Assert.Equal(expected.Result, reader.GetString(2));
            Assert.Equal("plpgsql", reader.GetString(3));
            Assert.Equal("tagekyc_raw_export_deployer", reader.GetString(4));
            Assert.False(reader.GetBoolean(5));
            Assert.True(reader.GetBoolean(6));
            Assert.Equal(["search_path=pg_catalog"], reader.GetFieldValue<string[]>(7));
            Assert.Equal(expected.BodySha256, HashBody(reader.GetString(8)));
            Assert.True(reader.GetBoolean(9));
            Assert.False(reader.GetBoolean(10));
            Assert.False(reader.GetBoolean(11));
            Assert.Equal(
                ["tagekyc_raw_export_deployer:tagekyc_runtime:EXECUTE:false"],
                reader.GetFieldValue<string[]>(12));
            Assert.False(await reader.ReadAsync());
        }
    }

    [Fact]
    public async Task F2_each_function_manifest_drift_turns_readiness_red()
    {
        await using var setup = postgres.CreateDbContext();
        await BootstrapHealthyRootsAsync(setup);
        await using var admin = await OpenAdminAsync();
        var definition = Convert.ToString(await ScalarAsync(
            admin,
            $"SELECT pg_get_functiondef('{RootHealthFunction}'::regprocedure);"))!;

        var mutations = new (string Mutate, string Restore)[]
        {
            (
                """
                CREATE OR REPLACE FUNCTION tagekyc.raw_export_control_plane_root_health()
                RETURNS TABLE ("IsHealthy" boolean, "StatusCode" text)
                LANGUAGE plpgsql SECURITY DEFINER SET search_path = pg_catalog
                AS $$ BEGIN RETURN QUERY SELECT TRUE, 'OK'::text; END $$;
                """,
                definition),
            (
                $"ALTER FUNCTION {RootHealthFunction} OWNER TO tagekyc;",
                $"ALTER FUNCTION {RootHealthFunction} OWNER TO tagekyc_raw_export_deployer;"),
            (
                $"ALTER FUNCTION {RootHealthFunction} SECURITY INVOKER;",
                $"ALTER FUNCTION {RootHealthFunction} SECURITY DEFINER;"),
            (
                $"ALTER FUNCTION {RootHealthFunction} RESET ALL;",
                $"ALTER FUNCTION {RootHealthFunction} SET search_path = pg_catalog;"),
            (
                $"GRANT EXECUTE ON FUNCTION {RootHealthFunction} TO PUBLIC;",
                $"REVOKE EXECUTE ON FUNCTION {RootHealthFunction} FROM PUBLIC;"),
            (
                $"REVOKE EXECUTE ON FUNCTION {RootHealthFunction} FROM tagekyc_runtime;",
                $"GRANT EXECUTE ON FUNCTION {RootHealthFunction} TO tagekyc_runtime;"),
        };

        foreach (var (mutate, restore) in mutations)
        {
            await ExecuteAsync(admin, mutate);
            try
            {
                var exception = await Assert.ThrowsAsync<RawExportControlPlaneReadinessException>(
                    () => ValidateAsRuntimeAsync());
                Assert.Equal(RawExportControlPlaneReadinessValidator.FunctionAclInvalid, exception.Code);
            }
            finally
            {
                await ExecuteAsync(admin, restore);
                await ExecuteAsync(
                    admin,
                    $"ALTER FUNCTION {RootHealthFunction} OWNER TO tagekyc_raw_export_deployer; " +
                    $"ALTER FUNCTION {RootHealthFunction} SECURITY DEFINER; " +
                    $"ALTER FUNCTION {RootHealthFunction} SET search_path = pg_catalog; " +
                    $"REVOKE ALL ON FUNCTION {RootHealthFunction} FROM PUBLIC; " +
                    $"REVOKE ALL ON FUNCTION {RootHealthFunction} FROM tagekyc; " +
                    $"GRANT EXECUTE ON FUNCTION {RootHealthFunction} TO tagekyc_runtime;");
            }
        }

        var unexpectedRole = $"e3_unexpected_exec_{Guid.NewGuid():N}";
        await ExecuteAsync(admin, $"CREATE ROLE {Quote(unexpectedRole)} NOLOGIN;");
        try
        {
            foreach (var function in Functions)
            {
                await ExecuteAsync(
                    admin,
                    $"GRANT EXECUTE ON FUNCTION {function.Signature} TO {Quote(unexpectedRole)};");
                try
                {
                    var unexpectedGrant = await Assert.ThrowsAsync<RawExportControlPlaneReadinessException>(
                        () => ValidateAsRuntimeAsync());
                    Assert.Equal(
                        RawExportControlPlaneReadinessValidator.FunctionAclInvalid,
                        unexpectedGrant.Code);
                }
                finally
                {
                    await ExecuteAsync(
                        admin,
                        $"REVOKE EXECUTE ON FUNCTION {function.Signature} FROM {Quote(unexpectedRole)};");
                }

                await ExecuteAsync(
                    admin,
                    $"REVOKE EXECUTE ON FUNCTION {function.Signature} FROM tagekyc_runtime;");
                try
                {
                    var missingRuntime = await Assert.ThrowsAsync<RawExportControlPlaneReadinessException>(
                        () => ValidateAsRuntimeAsync());
                    Assert.Equal(
                        RawExportControlPlaneReadinessValidator.FunctionAclInvalid,
                        missingRuntime.Code);
                }
                finally
                {
                    await ExecuteAsync(
                        admin,
                        $"GRANT EXECUTE ON FUNCTION {function.Signature} TO tagekyc_runtime;");
                }
            }
        }
        finally
        {
            await ExecuteAsync(admin, $"DROP ROLE {Quote(unexpectedRole)};");
        }

        await ValidateAsRuntimeAsync();
    }

    [Fact]
    public async Task F2_actor_context_positive_and_six_negative_controls_are_exact()
    {
        var actor = Tip88B34AuthorizationEngineTests.ConsumerPrincipal;
        var policy = Guid.NewGuid();

        await AssertProjectionCallAsync(actor, actor.ToString("D"), EligibilityFunction, null);
        await AssertProjectionCallAsync(actor, actor.ToString("D"), PolicyFunction, null);
        await AssertProjectionCallAsync(actor, null, EligibilityFunction, "RAW_EXPORT_ACTOR_CONTEXT_MISSING");
        await AssertProjectionCallAsync(actor, string.Empty, EligibilityFunction, "RAW_EXPORT_ACTOR_CONTEXT_MISSING");
        await AssertProjectionCallAsync(actor, "not-a-uuid", EligibilityFunction, "RAW_EXPORT_ACTOR_CONTEXT_INVALID");
        await AssertProjectionCallAsync(actor, Guid.NewGuid().ToString("D"), EligibilityFunction,
            "RAW_EXPORT_AUTHORIZATION_ACTOR_MISMATCH");
        await AssertProjectionCallAsync(
            null,
            actor.ToString("D"),
            EligibilityFunction,
            "RAW_EXPORT_AUTHORIZATION_ACTOR_MISMATCH");
        await AssertProjectionCallAsync(
            null,
            actor.ToString("D"),
            PolicyFunction,
            "RAW_EXPORT_AUTHORIZATION_ACTOR_MISMATCH");

        await using var first = await OpenAdminAsync();
        await using var firstTx = await first.BeginTransactionAsync();
        await SetActorAsync(first, firstTx, actor.ToString("D"));
        await using var second = await OpenAdminAsync();
        await using var secondTx = await second.BeginTransactionAsync();
        var exception = await Assert.ThrowsAsync<PostgresException>(
            () => CallProjectionAsync(second, secondTx, EligibilityFunction, actor, policy));
        Assert.Equal(PostgresErrorCodes.RaiseException, exception.SqlState);
        Assert.Equal("RAW_EXPORT_ACTOR_CONTEXT_MISSING", exception.MessageText);
        await firstTx.RollbackAsync();
        await secondTx.RollbackAsync();
    }

    [Fact]
    public async Task F3_runtime_only_login_authorizes_and_denies_without_set_role()
    {
        var authorizedPolicy = Guid.NewGuid();
        var deniedPolicy = Guid.NewGuid();
        Guid authorizedSession;
        Guid deniedSession;
        await using (var setup = postgres.CreateDbContext())
        {
            authorizedSession = await Tip88B34AuthorizationEngineTests.SeedCompletedSessionAsync(
                setup,
                Tip88B34AuthorizationEngineTests.ClientApplicationId,
                "subject:e3-authorized");
            deniedSession = await Tip88B34AuthorizationEngineTests.SeedCompletedSessionAsync(
                setup,
                Tip88B34AuthorizationEngineTests.ClientApplicationId,
                "subject:e3-denied");
            await Tip88B34AuthorizationEngineTests.SeedActivePolicyAsync(
                setup,
                authorizedPolicy,
                [RawExportRawClass.LiveSelfieImage],
                300,
                DateTimeOffset.UtcNow.AddMinutes(5));
            await Tip88B34AuthorizationEngineTests.SeedEffectiveConsentAsync(
                setup,
                authorizedSession,
                authorizedPolicy,
                [RawExportRawClass.LiveSelfieImage],
                DateTimeOffset.UtcNow.AddMinutes(4));
            await Tip88B34AuthorizationEngineTests.SeedActivePolicyAsync(
                setup,
                deniedPolicy,
                [RawExportRawClass.LiveSelfieImage],
                300,
                DateTimeOffset.UtcNow.AddMinutes(5));
            await new EfRawExportControlPlaneRepository(setup).SuspendPolicyAsync(new(
                Guid.Parse("88b34000-0000-5000-8000-000000000001"),
                deniedPolicy,
                1,
                1,
                "decision:e3-suspend"));
        }

        await using var login = await CreateRuntimeLoginAsync();
        await using var runtime = CreateDbContext(login.ConnectionString);
        await runtime.Database.OpenConnectionAsync();
        await using (var identity = runtime.Database.GetDbConnection().CreateCommand())
        {
            identity.CommandText = """
                SELECT current_user, session_user,
                       pg_has_role(current_user, 'tagekyc_runtime', 'MEMBER'),
                       pg_has_role(current_user, 'tagekyc_raw_export_deployer', 'MEMBER'),
                       pg_has_role(current_user, 'tagekyc_raw_export_bootstrapper', 'MEMBER'),
                       (SELECT count(*)
                        FROM pg_auth_members m
                        JOIN pg_roles member ON member.oid = m.member
                        WHERE member.rolname = current_user);
                """;
            await using var reader = await identity.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.Equal(login.Role, reader.GetString(0));
            Assert.Equal(login.Role, reader.GetString(1));
            Assert.True(reader.GetBoolean(2));
            Assert.False(reader.GetBoolean(3));
            Assert.False(reader.GetBoolean(4));
            Assert.Equal(1L, reader.GetInt64(5));
        }

        var repository = Tip88B34AuthorizationEngineTests.CreateRepository(runtime);
        var authorized = await repository.AuthorizeExportAsync(
            Tip88B34AuthorizationEngineTests.Command(
                authorizedSession,
                authorizedPolicy,
                "f3-runtime-authorized"));
        var denied = await repository.AuthorizeExportAsync(
            Tip88B34AuthorizationEngineTests.Command(
                deniedSession,
                deniedPolicy,
                "f3-runtime-denied"));

        Assert.Equal(RawExportAuthorizationOutcome.Authorized, authorized.Decision.Outcome);
        Assert.Equal(RawExportAuthorizationOutcome.Denied, denied.Decision.Outcome);
        Assert.Equal(
            RawExportAuthorizationPrimaryCause.EXPORT_ELIGIBILITY_INACTIVE,
            denied.Decision.PrimaryCause);
        Assert.Equal(
            [RawExportEligibilityCause.PolicySuspended],
            denied.EligibilityCauses.Select(item => item.Cause));
    }

    [Fact]
    public async Task F3_eligibility_projection_execute_is_required_by_inactive_engine_path()
    {
        var policy = Guid.NewGuid();
        Guid session;
        await using (var setup = postgres.CreateDbContext())
        {
            session = await Tip88B34AuthorizationEngineTests.SeedCompletedSessionAsync(
                setup,
                Tip88B34AuthorizationEngineTests.ClientApplicationId,
                "subject:e3-eligibility-required");
            await Tip88B34AuthorizationEngineTests.SeedActivePolicyAsync(
                setup,
                policy,
                [RawExportRawClass.LiveSelfieImage],
                300);
            await new EfRawExportControlPlaneRepository(setup).SuspendPolicyAsync(new(
                Guid.Parse("88b34000-0000-5000-8000-000000000001"),
                policy,
                1,
                1,
                "decision:e3-suspend-required"));
            await setup.Database.ExecuteSqlRawAsync(
                $"REVOKE EXECUTE ON FUNCTION {EligibilityFunction} FROM tagekyc_runtime;");
        }

        try
        {
            await using var login = await CreateRuntimeLoginAsync();
            await using var runtime = CreateDbContext(login.ConnectionString);
            var exception = await Assert.ThrowsAsync<PostgresException>(
                () => Tip88B34AuthorizationEngineTests.CreateRepository(runtime).AuthorizeExportAsync(
                    Tip88B34AuthorizationEngineTests.Command(
                        session,
                        policy,
                        "f3-eligibility-required")));
            Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, exception.SqlState);
        }
        finally
        {
            await using var restore = postgres.CreateDbContext();
            await restore.Database.ExecuteSqlRawAsync(
                $"GRANT EXECUTE ON FUNCTION {EligibilityFunction} TO tagekyc_runtime;");
        }
    }

    [Fact]
    public async Task F3_policy_projection_execute_is_required_by_authorized_engine_path()
    {
        var policy = Guid.NewGuid();
        Guid session;
        await using (var setup = postgres.CreateDbContext())
        {
            session = await Tip88B34AuthorizationEngineTests.SeedCompletedSessionAsync(
                setup,
                Tip88B34AuthorizationEngineTests.ClientApplicationId,
                "subject:e3-policy-required");
            await Tip88B34AuthorizationEngineTests.SeedActivePolicyAsync(
                setup,
                policy,
                [RawExportRawClass.LiveSelfieImage],
                300,
                DateTimeOffset.UtcNow.AddMinutes(5));
            await Tip88B34AuthorizationEngineTests.SeedEffectiveConsentAsync(
                setup,
                session,
                policy,
                [RawExportRawClass.LiveSelfieImage],
                DateTimeOffset.UtcNow.AddMinutes(4));
            await setup.Database.ExecuteSqlRawAsync(
                $"REVOKE EXECUTE ON FUNCTION {PolicyFunction} FROM tagekyc_runtime;");
        }

        try
        {
            await using var login = await CreateRuntimeLoginAsync();
            await using var runtime = CreateDbContext(login.ConnectionString);
            var exception = await Assert.ThrowsAsync<PostgresException>(
                () => Tip88B34AuthorizationEngineTests.CreateRepository(runtime).AuthorizeExportAsync(
                    Tip88B34AuthorizationEngineTests.Command(
                        session,
                        policy,
                        "f3-policy-required")));
            Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, exception.SqlState);
        }
        finally
        {
            await using var restore = postgres.CreateDbContext();
            await restore.Database.ExecuteSqlRawAsync(
                $"GRANT EXECUTE ON FUNCTION {PolicyFunction} TO tagekyc_runtime;");
        }
    }

    [Fact]
    public async Task F3_root_health_execute_is_required_by_readiness_path()
    {
        await using var db = postgres.CreateDbContext();
        await BootstrapHealthyRootsAsync(db);
        await db.Database.ExecuteSqlRawAsync(
            $"REVOKE EXECUTE ON FUNCTION {RootHealthFunction} FROM tagekyc_runtime;");
        try
        {
            var (status, body) = await ReadinessAsync();
            Assert.Equal(HttpStatusCode.ServiceUnavailable, status);
            Assert.Contains(
                RawExportControlPlaneReadinessValidator.FunctionAclInvalid,
                body,
                StringComparison.Ordinal);
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync(
                $"GRANT EXECUTE ON FUNCTION {RootHealthFunction} TO tagekyc_runtime;");
        }
    }

    [Fact]
    public async Task F5_both_projection_times_equal_b2_and_persisted_decision_time()
    {
        var policy = Guid.NewGuid();
        await using var db = postgres.CreateDbContext();
        var session = await Tip88B34AuthorizationEngineTests.SeedCompletedSessionAsync(
            db,
            Tip88B34AuthorizationEngineTests.ClientApplicationId,
            "subject:e3-time");
        await Tip88B34AuthorizationEngineTests.SeedActivePolicyAsync(
            db,
            policy,
            [RawExportRawClass.LiveSelfieImage],
            300,
            DateTimeOffset.UtcNow.AddMinutes(5));
        await Tip88B34AuthorizationEngineTests.SeedEffectiveConsentAsync(
            db,
            session,
            policy,
            [RawExportRawClass.LiveSelfieImage],
            DateTimeOffset.UtcNow.AddMinutes(4));

        var capture = new CapturingProjectionReader(new EfRawExportAuthorizationProjectionReader(db));
        var result = await Tip88B34AuthorizationEngineTests.CreateRepository(db, projections: capture)
            .AuthorizeExportAsync(Tip88B34AuthorizationEngineTests.Command(
                session,
                policy,
                "f5-one-clock"));

        Assert.NotNull(capture.Eligibility);
        Assert.NotNull(capture.Policy);
        Assert.Equal(capture.Eligibility!.EvaluatedAtUtc, capture.Policy!.EvaluatedAtUtc);
        Assert.Equal(capture.Eligibility.EvaluatedAtUtc, result.Decision.EligibilityEvaluatedAtUtc);
        Assert.Equal(capture.Policy.EvaluatedAtUtc, result.Decision.ConsentEvaluatedAtUtc);
        Assert.Equal(capture.Policy.EvaluatedAtUtc, result.Decision.DecidedAtUtc);
        var persisted = await db.RawExportAuthorizationDecisions.SingleAsync(
            item => item.ExportDecisionId == result.Decision.ExportDecisionId);
        Assert.Equal(capture.Policy.EvaluatedAtUtc, persisted.DecidedAtUtc);
        Assert.Equal(capture.Eligibility.EvaluatedAtUtc, persisted.EligibilityEvaluatedAtUtc);
        Assert.Equal(capture.Policy.EvaluatedAtUtc, persisted.ConsentEvaluatedAtUtc);
    }

    [Fact]
    public async Task F6_migration_apply_rollback_reapply_restores_snapshot_functions_and_acls_exactly()
    {
        await using var db = postgres.CreateDbContext();
        var migrator = db.GetService<IMigrator>();
        var snapshotBefore = ModelSnapshotSha256();
        Assert.Equal(ExpectedModelSnapshotSha256, snapshotBefore);
        var r3Attempt = db.Model.FindEntityType(
            "TagEkyc.Infrastructure.Persistence.Entities.RawExportSourceEncryptionAttemptRow");
        Assert.NotNull(r3Attempt);
        foreach (var property in new[]
        {
            "StagedCiphertextFingerprintSchemaVersion", "StagedCiphertextFingerprint",
            "StagedObjectCustodyId", "StagedObjectStateRevision", "StagedFromReservationRevision",
            "VerifiedPlaintextLength", "StagedCiphertextLength", "StagedCiphertextDigest",
            "StagedProviderReceiptDigest", "StagedVerificationEvidenceDigest", "StagedAtUtc",
        })
            Assert.NotNull(r3Attempt!.FindProperty(property));
        Assert.Contains(r3Attempt!.GetForeignKeys(), foreignKey =>
            foreignKey.GetConstraintName() == "fk_raw_export_source_attempt_staged_object"
            && foreignKey.Properties.Single().Name == "StagedObjectCustodyId");
        string[] expectedPreE3RuntimeAcl =
        [
            "raw_export_subject_consent_authorities|tagekyc|tagekyc_runtime|SELECT|false",
            "raw_export_subject_consent_classes|tagekyc|tagekyc_runtime|SELECT|false",
            "raw_export_subject_consent_events|tagekyc|tagekyc_runtime|SELECT|false",
            "verification_sessions|tagekyc|tagekyc_runtime|SELECT|false",
        ];

        string? applied = null;
        try
        {
            await migrator.MigrateAsync(PreviousMigration);
            Assert.Equal(
                expectedPreE3RuntimeAcl,
                await ReadPreE3RuntimeAclManifestAsync(db));
            var before = await CatalogSnapshotAsync(db);
            Assert.DoesNotContain("raw_export_read_authorization_", before, StringComparison.Ordinal);

            await migrator.MigrateAsync(Migration);
            Assert.Empty(await ReadPreE3RuntimeAclManifestAsync(db));
            applied = await CatalogSnapshotAsync(db);
            Assert.Contains("raw_export_read_authorization_eligibility_inputs", applied, StringComparison.Ordinal);

            await migrator.MigrateAsync(PreviousMigration);
            Assert.Equal(
                expectedPreE3RuntimeAcl,
                await ReadPreE3RuntimeAclManifestAsync(db));
            Assert.Equal(before, await CatalogSnapshotAsync(db));
            await migrator.MigrateAsync(Migration);
            Assert.Empty(await ReadPreE3RuntimeAclManifestAsync(db));
            Assert.Equal(applied, await CatalogSnapshotAsync(db));
        }
        finally
        {
            await migrator.MigrateAsync(Migration);
        }

        Assert.NotNull(applied);
        Assert.Equal(applied, await CatalogSnapshotAsync(db));
        Assert.Equal(Migration, await CurrentMigrationAsync(db));
        Assert.Equal(snapshotBefore, ModelSnapshotSha256());

        var result = await RunAuthorizedAsRuntimeAsync("f6-reapply");
        Assert.Equal(RawExportAuthorizationOutcome.Authorized, result.Decision.Outcome);
    }

    [Fact]
    public async Task F7_root_health_is_bounded_and_returns_no_authority_identifiers()
    {
        await using var db = postgres.CreateDbContext();
        await BootstrapHealthyRootsAsync(db);
        await using var connection = await OpenAdminAsync();
        var resultShape = Convert.ToString(await ScalarAsync(
            connection,
            $"SELECT pg_get_function_result('{RootHealthFunction}'::regprocedure);"));
        Assert.Equal("TABLE(\"IsHealthy\" boolean, \"StatusCode\" text)", resultShape);
        Assert.DoesNotContain("principal", resultShape, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("authority", resultShape, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("detail", resultShape, StringComparison.OrdinalIgnoreCase);

        await using var health = new NpgsqlCommand(
            "SELECT \"IsHealthy\", \"StatusCode\" FROM tagekyc.raw_export_control_plane_root_health();",
            connection);
        await using var healthReader = await health.ExecuteReaderAsync();
        Assert.True(await healthReader.ReadAsync());
        Assert.True(healthReader.GetBoolean(0));
        Assert.Equal("OK", healthReader.GetString(1));
        Assert.False(await healthReader.ReadAsync());
    }

    [Fact]
    public async Task F7_root_readiness_uses_sd_health_and_preserves_exact_failure_codes()
    {
        var (missingStatus, missingBody) = await ReadinessAsync();
        Assert.Equal(HttpStatusCode.ServiceUnavailable, missingStatus);
        Assert.Contains(
            RawExportControlPlaneReadinessValidator.RootAuthorityMissing,
            missingBody,
            StringComparison.Ordinal);

        await using (var db = postgres.CreateDbContext())
        {
            await BootstrapHealthyRootsAsync(db, RawExportControlPlaneConstants.DeploymentPrincipalId);
        }
        var (devStatus, devBody) = await ReadinessAsync();
        Assert.Equal(HttpStatusCode.ServiceUnavailable, devStatus);
        Assert.Contains(
            RawExportControlPlaneReadinessValidator.DevDefaultPrincipalForbidden,
            devBody,
            StringComparison.Ordinal);

        await postgres.ResetDatabaseAsync();
        await using (var db = postgres.CreateDbContext())
        {
            await BootstrapHealthyRootsAsync(db);
        }
        await using var login = await CreateRuntimeLoginAsync();
        await using var runtime = CreateDbContext(login.ConnectionString);
        await new RawExportControlPlaneReadinessValidator(runtime).ValidateAsync(CancellationToken.None);
        await runtime.Database.OpenConnectionAsync();
        await AssertSqlStateAsync(
            (NpgsqlConnection)runtime.Database.GetDbConnection(),
            "SELECT * FROM tagekyc.raw_export_control_authorities LIMIT 0;",
            PostgresErrorCodes.InsufficientPrivilege);
    }

    private async Task<RawExportAuthorizationResult> RunAuthorizedAsRuntimeAsync(string key)
    {
        var policy = Guid.NewGuid();
        Guid session;
        await using (var setup = postgres.CreateDbContext())
        {
            session = await Tip88B34AuthorizationEngineTests.SeedCompletedSessionAsync(
                setup,
                Tip88B34AuthorizationEngineTests.ClientApplicationId,
                $"subject:{key}");
            await Tip88B34AuthorizationEngineTests.SeedActivePolicyAsync(
                setup,
                policy,
                [RawExportRawClass.LiveSelfieImage],
                300,
                DateTimeOffset.UtcNow.AddMinutes(5));
            await Tip88B34AuthorizationEngineTests.SeedEffectiveConsentAsync(
                setup,
                session,
                policy,
                [RawExportRawClass.LiveSelfieImage],
                DateTimeOffset.UtcNow.AddMinutes(4));
        }

        await using var login = await CreateRuntimeLoginAsync();
        await using var runtime = CreateDbContext(login.ConnectionString);
        return await Tip88B34AuthorizationEngineTests.CreateRepository(runtime).AuthorizeExportAsync(
            Tip88B34AuthorizationEngineTests.Command(session, policy, key));
    }

    private static async Task AssertRuntimeLoginShapeAsync(TagEkycDbContext runtime, string role)
    {
        await runtime.Database.OpenConnectionAsync();
        await using var identity = runtime.Database.GetDbConnection().CreateCommand();
        identity.CommandText = """
            SELECT current_user, session_user,
                   pg_has_role(current_user, 'tagekyc_runtime', 'MEMBER'),
                   pg_has_role(current_user, 'tagekyc_raw_export_deployer', 'MEMBER'),
                   pg_has_role(current_user, 'tagekyc_raw_export_bootstrapper', 'MEMBER'),
                   (SELECT count(*)
                    FROM pg_auth_members AS membership
                    JOIN pg_roles AS member ON member.oid = membership.member
                    WHERE member.rolname = current_user),
                   (SELECT NOT membership.admin_option
                               AND membership.inherit_option
                               AND NOT membership.set_option
                    FROM pg_auth_members AS membership
                    JOIN pg_roles AS member ON member.oid = membership.member
                    JOIN pg_roles AS granted ON granted.oid = membership.roleid
                    WHERE member.rolname = current_user
                      AND granted.rolname = 'tagekyc_runtime'),
                   role.rolinherit,
                   role.rolsuper,
                   role.rolcreatedb,
                   role.rolcreaterole,
                   role.rolreplication,
                   role.rolbypassrls
            FROM pg_roles AS role
            WHERE role.rolname = current_user;
            """;
        await using var reader = await identity.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(role, reader.GetString(0));
        Assert.Equal(role, reader.GetString(1));
        Assert.True(reader.GetBoolean(2));
        Assert.False(reader.GetBoolean(3));
        Assert.False(reader.GetBoolean(4));
        Assert.Equal(1L, reader.GetInt64(5));
        Assert.True(reader.GetBoolean(6));
        Assert.True(reader.GetBoolean(7));
        Assert.False(reader.GetBoolean(8));
        Assert.False(reader.GetBoolean(9));
        Assert.False(reader.GetBoolean(10));
        Assert.False(reader.GetBoolean(11));
        Assert.False(reader.GetBoolean(12));
        Assert.False(await reader.ReadAsync());
    }

    private async Task ValidateAsRuntimeAsync()
    {
        await using var login = await CreateRuntimeLoginAsync();
        await AssertReadinessCodeAsync(login.ConnectionString, expectedCode: null);
    }

    private static async Task AssertReadinessCodeAsync(
        string connectionString,
        string? expectedCode)
    {
        await using var db = CreateDbContext(connectionString);
        await AssertReadinessCodeAsync(db, expectedCode);
    }

    private static async Task AssertReadinessCodeAsync(
        TagEkycDbContext db,
        string? expectedCode)
    {
        var validator = new RawExportControlPlaneReadinessValidator(db);
        if (expectedCode is null)
        {
            await validator.ValidateAsync(CancellationToken.None);
            return;
        }

        var exception = await Assert.ThrowsAsync<RawExportControlPlaneReadinessException>(
            () => validator.ValidateAsync(CancellationToken.None));
        Assert.Equal(expectedCode, exception.Code);
    }

    private async Task<(HttpStatusCode Status, string Body)> ReadinessAsync()
    {
        await using var login = await CreateRuntimeLoginAsync();
        var builder = WebApplication.CreateBuilder(
            new WebApplicationOptions { EnvironmentName = "Production" });
        builder.WebHost.UseTestServer();
        builder.Services.AddDbContext<TagEkycDbContext>(
            options => options.UseNpgsql(login.ConnectionString));
        builder.Services.AddScoped<RawExportControlPlaneReadinessValidator>();
        builder.Services.AddScoped<IReadinessCheck, RawExportControlPlaneReadinessCheck>();
        await using var app = builder.Build();
        app.MapReadinessEndpoint();
        await app.StartAsync();
        var response = await app.GetTestClient().GetAsync("/readiness");
        return (response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    private async Task AssertProjectionCallAsync(
        Guid? principal,
        string? actorContext,
        string function,
        string? expectedMessage)
    {
        await using var connection = await OpenAdminAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        if (actorContext is not null)
        {
            await SetActorAsync(connection, transaction, actorContext);
        }

        if (expectedMessage is null)
        {
            await CallProjectionAsync(connection, transaction, function, principal, Guid.NewGuid());
        }
        else
        {
            var exception = await Assert.ThrowsAsync<PostgresException>(
                () => CallProjectionAsync(
                    connection,
                    transaction,
                    function,
                    principal,
                    Guid.NewGuid()));
            Assert.Equal(PostgresErrorCodes.RaiseException, exception.SqlState);
            Assert.Equal(expectedMessage, exception.MessageText);
        }

        await transaction.RollbackAsync();
    }

    private static async Task CallProjectionAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string function,
        Guid? principal,
        Guid policy)
    {
        await using var command = new NpgsqlCommand(
            $"SELECT * FROM {function[..function.IndexOf('(')]}(@principal,@policy,1);",
            connection,
            transaction);
        command.Parameters.Add("principal", NpgsqlDbType.Uuid).Value =
            principal is null ? DBNull.Value : principal.Value;
        command.Parameters.AddWithValue("policy", policy);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
        }
    }

    private static async Task SetActorAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string actor)
    {
        await using var command = new NpgsqlCommand(
            "SELECT pg_catalog.set_config('tagekyc.actor_principal_id', @actor, true);",
            connection,
            transaction);
        command.Parameters.AddWithValue("actor", actor);
        await command.ExecuteScalarAsync();
    }

    private async Task BootstrapHealthyRootsAsync(
        TagEkycDbContext db,
        Guid? principal = null)
    {
        var actor = principal ?? Guid.Parse("88b1e300-0000-5000-8000-000000000001");
        foreach (var authority in new[] { "GrantAdmin", "RecorderAuthorityAdmin", "ActivationAuthority" })
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                SELECT tagekyc.raw_export_bootstrap_global_authority(
                    {actor},{authority},{"decision:e3-bootstrap:" + authority});
                """);
        }
    }

    private async Task<Dictionary<string, string>> ReadFirstColumnsAsync()
    {
        await using var connection = await OpenAdminAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT table_name, column_name
            FROM (
                SELECT table_name, column_name,
                       row_number() OVER (PARTITION BY table_name ORDER BY ordinal_position) AS rn
                FROM information_schema.columns
                WHERE table_schema = 'tagekyc' AND table_name = ANY(@tables)
            ) AS columns
            WHERE rn = 1;
            """,
            connection);
        command.Parameters.AddWithValue("tables", E3ProtectedRuntimeDirectAccessTables);
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(reader.GetString(0), reader.GetString(1));
        }

        Assert.Equal(E3ProtectedRuntimeDirectAccessTables.Length, result.Count);
        return result;
    }

    private async Task<RuntimeLogin> CreateRuntimeLoginAsync()
        => await CreateRuntimeLoginAsync(postgres.ConnectionString);

    private static async Task<RuntimeLogin> CreateRuntimeLoginAsync(
        string adminConnectionString)
    {
        var role = $"e3_runtime_{Guid.NewGuid():N}";
        var password = $"E3_{Guid.NewGuid():N}!";
        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync();
        await ExecuteAsync(
            connection,
            $"""
            CREATE ROLE {Quote(role)} LOGIN INHERIT PASSWORD '{password}'
                NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
            GRANT tagekyc_runtime TO {Quote(role)}
                WITH ADMIN FALSE, INHERIT TRUE, SET FALSE;
            """);
        var builder = new NpgsqlConnectionStringBuilder(adminConnectionString)
        {
            Username = role,
            Password = password,
            Pooling = false,
        };
        return new RuntimeLogin(adminConnectionString, builder.ConnectionString, role);
    }

    private static TagEkycDbContext CreateDbContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<TagEkycDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new TagEkycDbContext(options);
    }

    private async Task<NpgsqlConnection> OpenAdminAsync()
    {
        var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    private static async Task AssertSqlStateAsync(
        NpgsqlConnection connection,
        string sql,
        string state)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        var exception = await Assert.ThrowsAsync<PostgresException>(
            () => command.ExecuteNonQueryAsync());
        Assert.Equal(state, exception.SqlState);
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<object?> ScalarAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return await command.ExecuteScalarAsync();
    }

    private async Task<ConstraintModeFixture> SeedConstraintModeFixtureAsync(int callSessionCount)
    {
        var policyId = Guid.NewGuid();
        var recorderPrincipalId = Guid.NewGuid();
        var sessions = new List<ConstraintSession>();
        await using var setup = postgres.CreateDbContext();
        await Tip88B34AuthorizationEngineTests.SeedActivePolicyAsync(
            setup,
            policyId,
            [RawExportRawClass.ChipDg1],
            300,
            DateTimeOffset.UtcNow.AddMinutes(5));
        await new EfRawExportSubjectConsentRepository(setup).GrantConsentAuthorityAsync(new(
            B34AdminPrincipal,
            recorderPrincipalId,
            Tip88B34AuthorizationEngineTests.ClientApplicationId,
            RawExportSubjectConsentAuthorityType.SubjectConsentRecorder,
            ExpectedRevision: 0,
            $"decision:r5-authority:{recorderPrincipalId:N}"));

        for (var index = 0; index <= callSessionCount; index++)
        {
            var subject = $"subject:r5:{recorderPrincipalId:N}:{index}";
            sessions.Add(new ConstraintSession(
                await Tip88B34AuthorizationEngineTests.SeedCompletedSessionAsync(
                    setup,
                    Tip88B34AuthorizationEngineTests.ClientApplicationId,
                    subject),
                subject));
        }

        return new ConstraintModeFixture(
            policyId,
            recorderPrincipalId,
            sessions.Take(callSessionCount).ToArray(),
            sessions[^1]);
    }

    private static async Task<int> CallGrantedConsentFunctionAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid sessionId,
        Guid policyId,
        string decisionRef)
    {
        await using var command = new NpgsqlCommand(
            """
            SELECT tagekyc.raw_export_append_subject_consent_granted(
                @sessionId,@policyId,1,@rawClasses,
                'consent-text:r5','sha256:r5','artifact:r5',@decisionRef,NULL);
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("sessionId", sessionId);
        command.Parameters.AddWithValue("policyId", policyId);
        command.Parameters.Add("rawClasses", NpgsqlDbType.Array | NpgsqlDbType.Text).Value =
            new[] { RawExportRawClass.ChipDg1.ToString() };
        command.Parameters.AddWithValue("decisionRef", decisionRef);
        return Convert.ToInt32(
            await command.ExecuteScalarAsync(),
            System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task InsertGrantedConsentParentAndClassAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        ConstraintSession session,
        Guid policyId,
        Guid recorderPrincipalId)
    {
        var recordId = await InsertGrantedConsentParentOnlyAsync(
            connection,
            transaction,
            session,
            policyId,
            recorderPrincipalId);
        await using var child = new NpgsqlCommand(
            """
            INSERT INTO tagekyc.raw_export_subject_consent_classes
                ("SubjectConsentRecordId","RawClass","CreatedAt")
            VALUES (@recordId,'ChipDg1',transaction_timestamp());
            """,
            connection,
            transaction);
        child.Parameters.AddWithValue("recordId", recordId);
        await child.ExecuteNonQueryAsync();
    }

    private static async Task<Guid> InsertGrantedConsentParentOnlyAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        ConstraintSession session,
        Guid policyId,
        Guid recorderPrincipalId)
    {
        var recordId = Guid.NewGuid();
        await using var parent = new NpgsqlCommand(
            """
            SELECT pg_catalog.set_config(
                'tagekyc.raw_export_subject_consent_append_context',
                'consent',
                true);
            INSERT INTO tagekyc.raw_export_subject_consent_events
                ("SubjectConsentRecordId","ConsentScopeHash","VerificationSessionId",
                 "SubjectRef","PolicyId","PolicyVersion","PurposeCode",
                 "RecipientClientApplicationId","Revision","EventType","TargetRevision",
                 "ConsentTextVersion","ConsentTextContentHash",
                 "ExternalConsentArtifactRef","DecisionRef","ValidFromUtc",
                 "ValidUntilUtc","CapturedAtUtc","CapturedByPrincipalId","RecordedAtUtc")
            VALUES
                (@recordId,
                 tagekyc.raw_export_consent_scope_hash(
                     @sessionId,@subjectRef,@policyId,1,
                     'SubjectRawBiometricExport',@clientApplicationId),
                 @sessionId,@subjectRef,@policyId,1,'SubjectRawBiometricExport',
                 @clientApplicationId,1,'Granted',NULL,'consent-text:r5',
                 'sha256:r5','artifact:r5','decision:r5-probe',
                 transaction_timestamp(),NULL,transaction_timestamp(),
                 @recorderPrincipalId,transaction_timestamp());
            """,
            connection,
            transaction);
        parent.Parameters.AddWithValue("recordId", recordId);
        parent.Parameters.AddWithValue("sessionId", session.SessionId);
        parent.Parameters.AddWithValue("subjectRef", session.SubjectRef);
        parent.Parameters.AddWithValue("policyId", policyId);
        parent.Parameters.AddWithValue(
            "clientApplicationId",
            Tip88B34AuthorizationEngineTests.ClientApplicationId);
        parent.Parameters.AddWithValue("recorderPrincipalId", recorderPrincipalId);
        await parent.ExecuteNonQueryAsync();
        return recordId;
    }

    private static async Task<string> ReadFunctionDefinitionAsync(
        NpgsqlConnection connection,
        string signature) =>
        (string)(await ScalarAsync(
            connection,
            $"SELECT pg_catalog.pg_get_functiondef('{signature}'::regprocedure);")
            ?? throw new InvalidOperationException($"Function {signature} is missing."));

    private static FulfillmentShapeCase FulfillmentCase(
        string name,
        string eventId = "'88b1e300-0000-5000-8000-000000000004'::uuid",
        string revision = "1",
        string eventType = "'Accepted'::text",
        string artifactRef = "'artifact:r4'::text",
        string artifactVersion = "'v1'::text",
        string validFrom = "transaction_timestamp() - interval '1 minute'",
        string validUntil = "NULL::timestamptz") =>
        new(
            name,
            eventId,
            revision,
            eventType,
            artifactRef,
            artifactVersion,
            validFrom,
            validUntil);

    private static string BuildInvalidFulfillmentProjectionFunction(FulfillmentShapeCase shape) =>
        $"""
        CREATE OR REPLACE FUNCTION tagekyc.raw_export_read_authorization_eligibility_inputs(
            principal_id uuid,
            policy_id uuid,
            policy_version integer)
        RETURNS TABLE (
            "PolicyId" uuid,
            "PolicyVersion" integer,
            "EvaluatedAtUtc" timestamp with time zone,
            "PolicyExists" boolean,
            "BoundRuleSetVersion" integer,
            "CurrentRuleSetVersion" integer,
            "ClosureType" text,
            "GrantPrincipalId" uuid,
            "GrantPolicyId" uuid,
            "GrantPolicyVersion" integer,
            "GrantRevision" integer,
            "GrantEventType" text,
            "LifecyclePolicyId" uuid,
            "LifecyclePolicyVersion" integer,
            "LifecycleRevision" integer,
            "LifecycleEventType" text,
            "RequirementOrdinal" integer,
            "RequirementType" text,
            "FulfillmentEventId" uuid,
            "FulfillmentRevision" integer,
            "FulfillmentEventType" text,
            "ArtifactRef" text,
            "ArtifactVersion" text,
            "ValidFromUtc" timestamp with time zone,
            "ValidUntilUtc" timestamp with time zone)
        LANGUAGE plpgsql
        SECURITY DEFINER
        SET search_path = pg_catalog
        AS $function$
        BEGIN
            IF tagekyc.raw_export_current_actor() IS DISTINCT FROM principal_id THEN
                RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_ACTOR_MISMATCH';
            END IF;

            RETURN QUERY SELECT
                policy_id,
                policy_version,
                transaction_timestamp(),
                TRUE,
                1,
                1,
                'CatalogApproved'::text,
                principal_id,
                policy_id,
                policy_version,
                1,
                'Granted'::text,
                policy_id,
                policy_version,
                1,
                'Activated'::text,
                0,
                'LegalApproval'::text,
                {shape.EventId},
                {shape.Revision},
                {shape.EventType},
                {shape.ArtifactRef},
                {shape.ArtifactVersion},
                {shape.ValidFrom},
                {shape.ValidUntil};
        END;
        $function$;
        """;

    private static async Task<string> ReadAuthorizationTableCountsAsync(
        NpgsqlConnection connection) =>
        Convert.ToString(await ScalarAsync(
            connection,
            """
            SELECT pg_catalog.concat_ws(
                ',',
                (SELECT count(*) FROM tagekyc.raw_export_authorization_idempotency),
                (SELECT count(*) FROM tagekyc.raw_export_authorization_decisions),
                (SELECT count(*) FROM tagekyc.raw_export_decision_eligibility_causes),
                (SELECT count(*) FROM tagekyc.raw_export_decision_fulfillment_refs),
                (SELECT count(*) FROM tagekyc.raw_export_decision_classes),
                (SELECT count(*) FROM tagekyc.raw_export_authorization_permits),
                (SELECT count(*) FROM tagekyc.raw_export_permit_classes));
            """)) ?? string.Empty;

    private static async Task<string[]> ReadPreE3RuntimeAclManifestAsync(
        TagEkycDbContext db) =>
        await db.Database.SqlQueryRaw<string>(
                """
                SELECT
                    relation.relname || '|' ||
                    COALESCE(grantor.rolname, acl.grantor::text) || '|' ||
                    COALESCE(grantee.rolname, acl.grantee::text) || '|' ||
                    acl.privilege_type || '|' ||
                    acl.is_grantable::text AS "Value"
                FROM pg_class AS relation
                JOIN pg_namespace AS namespace
                  ON namespace.oid = relation.relnamespace
                CROSS JOIN LATERAL aclexplode(
                    COALESCE(
                        relation.relacl,
                        acldefault('r', relation.relowner))) AS acl
                LEFT JOIN pg_roles AS grantor ON grantor.oid = acl.grantor
                LEFT JOIN pg_roles AS grantee ON grantee.oid = acl.grantee
                WHERE namespace.nspname = 'tagekyc'
                  AND relation.relname IN (
                      'verification_sessions',
                      'raw_export_subject_consent_authorities',
                      'raw_export_subject_consent_events',
                      'raw_export_subject_consent_classes')
                  AND acl.grantee = 'tagekyc_runtime'::regrole::oid
                ORDER BY relation.relname, grantor.rolname, acl.privilege_type;
                """)
            .ToArrayAsync();

    private static string Quote(string identifier) =>
        '"' + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + '"';

    private static string HashBody(string body) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body))).ToLowerInvariant();

    private static async Task<string> CatalogSnapshotAsync(TagEkycDbContext db)
    {
        await db.Database.OpenConnectionAsync();
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        await using var command = new NpgsqlCommand(
            """
            WITH function_rows AS (
                SELECT
                    'FUNCTION|' || n.nspname || '.' || p.proname || '(' ||
                    pg_get_function_identity_arguments(p.oid) || ')|' ||
                    pg_get_function_result(p.oid) || '|' ||
                    language.lanname || '|' ||
                    r.rolname || '|' || r.rolcanlogin::text || '|' ||
                    p.prosecdef::text || '|' ||
                    COALESCE(array_to_string(p.proconfig, ','), '') || '|' ||
                    md5(p.prosrc) || '|' ||
                    COALESCE((
                        SELECT string_agg(
                            (CASE WHEN acl.grantee = 0 THEN 'PUBLIC' ELSE grantee.rolname END) ||
                            ':' || COALESCE(grantor.rolname, acl.grantor::text) ||
                            ':' || acl.privilege_type || ':' || acl.is_grantable::text,
                            ',' ORDER BY
                            CASE WHEN acl.grantee = 0 THEN 'PUBLIC' ELSE grantee.rolname END,
                            COALESCE(grantor.rolname, acl.grantor::text),
                            acl.privilege_type,
                            acl.is_grantable)
                        FROM aclexplode(COALESCE(p.proacl, acldefault('f', p.proowner))) AS acl
                        LEFT JOIN pg_roles AS grantor ON grantor.oid = acl.grantor
                        LEFT JOIN pg_roles AS grantee ON grantee.oid = acl.grantee
                    ), '') AS line
                FROM pg_proc p
                JOIN pg_namespace n ON n.oid = p.pronamespace
                JOIN pg_roles r ON r.oid = p.proowner
                JOIN pg_language language ON language.oid = p.prolang
                WHERE n.nspname = 'tagekyc'
                  AND p.proname LIKE '%raw_export%'
            ),
            acl_rows AS (
                SELECT
                    t.table_name || '|tagekyc_runtime|' || p.privilege_name || '|' ||
                    has_table_privilege(
                        'tagekyc_runtime',
                        'tagekyc.' || t.table_name,
                        p.privilege_name)::text AS line
                FROM unnest(@tables) AS t(table_name)
                CROSS JOIN unnest(@privileges) AS p(privilege_name)
                UNION ALL
                SELECT
                    'raw_export_policy_allowed_classes|tagekyc_raw_export_deployer|SELECT|' ||
                     has_table_privilege(
                         'tagekyc_raw_export_deployer',
                         'tagekyc.raw_export_policy_allowed_classes',
                         'SELECT')::text
            ),
            table_acl_rows AS (
                SELECT
                    'TABLE_ACL|' || relation.relname || '|' ||
                    COALESCE(grantor.rolname, acl.grantor::text) || '|' ||
                    CASE
                        WHEN acl.grantee = 0 THEN 'PUBLIC'
                        ELSE COALESCE(grantee.rolname, acl.grantee::text)
                    END || '|' ||
                    acl.privilege_type || '|' || acl.is_grantable::text AS line
                FROM pg_class AS relation
                JOIN pg_namespace AS namespace ON namespace.oid = relation.relnamespace
                CROSS JOIN LATERAL aclexplode(
                    COALESCE(relation.relacl, acldefault('r', relation.relowner))) AS acl
                LEFT JOIN pg_roles AS grantor ON grantor.oid = acl.grantor
                LEFT JOIN pg_roles AS grantee ON grantee.oid = acl.grantee
                WHERE namespace.nspname = 'tagekyc'
                  AND relation.relname = ANY(@tables)
            ),
            role_rows AS (
                SELECT
                    'ROLE|' || role.rolname || '|' ||
                    role.rolsuper::text || '|' ||
                    role.rolinherit::text || '|' ||
                    role.rolcreaterole::text || '|' ||
                    role.rolcreatedb::text || '|' ||
                    role.rolcanlogin::text || '|' ||
                    role.rolreplication::text || '|' ||
                    role.rolbypassrls::text AS line
                FROM pg_roles AS role
                WHERE role.rolname IN (
                    'tagekyc_runtime',
                    'tagekyc_raw_export_deployer',
                    'tagekyc_raw_export_bootstrapper')
            ),
            membership_rows AS (
                SELECT
                    'MEMBERSHIP|' || granted.rolname || '|' ||
                    member.rolname || '|' ||
                    COALESCE(grantor.rolname, membership.grantor::text) || '|' ||
                    membership.admin_option::text || '|' ||
                    membership.inherit_option::text || '|' ||
                    membership.set_option::text AS line
                FROM pg_auth_members AS membership
                JOIN pg_roles AS granted ON granted.oid = membership.roleid
                JOIN pg_roles AS member ON member.oid = membership.member
                LEFT JOIN pg_roles AS grantor ON grantor.oid = membership.grantor
                WHERE granted.rolname IN (
                    'tagekyc_runtime',
                    'tagekyc_raw_export_deployer',
                    'tagekyc_raw_export_bootstrapper')
                   OR member.rolname IN (
                    'tagekyc_runtime',
                    'tagekyc_raw_export_deployer',
                    'tagekyc_raw_export_bootstrapper')
            )
            SELECT string_agg(line, E'\n' ORDER BY line)
            FROM (
                SELECT line FROM function_rows
                UNION ALL
                SELECT line FROM acl_rows
                UNION ALL
                SELECT line FROM table_acl_rows
                UNION ALL
                SELECT line FROM role_rows
                UNION ALL
                SELECT line FROM membership_rows
            ) snapshot;
            """,
            connection);
        command.Parameters.AddWithValue("tables", E3ProtectedRuntimeDirectAccessTables);
        command.Parameters.AddWithValue("privileges", E3ForbiddenRuntimeTablePrivileges);
        return Convert.ToString(await command.ExecuteScalarAsync()) ?? string.Empty;
    }

    private static async Task<string> CurrentMigrationAsync(TagEkycDbContext db) =>
        await db.Database.SqlQueryRaw<string>(
                """
                SELECT "MigrationId" AS "Value"
                FROM "__EFMigrationsHistory"
                ORDER BY "MigrationId" DESC
                LIMIT 1
                """)
            .SingleAsync();

    private static string ModelSnapshotSha256()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null &&
               !File.Exists(Path.Combine(directory.FullName, "TagEkyc.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        var path = Path.Combine(
            directory!.FullName,
            "src",
            "TagEkyc.Infrastructure",
            "Persistence",
            "Migrations",
            "TagEkycDbContextModelSnapshot.cs");
        return Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
    }

    private sealed class CapturingProjectionReader(IRawExportAuthorizationProjectionReader inner)
        : IRawExportAuthorizationProjectionReader
    {
        public RawExportAuthorizationEligibilityProjection? Eligibility { get; private set; }

        public RawExportAuthorizationPolicyProjection? Policy { get; private set; }

        public async Task<RawExportAuthorizationEligibilityProjection> ReadEligibilityInputsAsync(
            Guid principalId,
            Guid policyId,
            int policyVersion,
            CancellationToken cancellationToken = default)
        {
            Eligibility = await inner.ReadEligibilityInputsAsync(
                principalId,
                policyId,
                policyVersion,
                cancellationToken);
            return Eligibility;
        }

        public async Task<RawExportAuthorizationPolicyProjection> ReadPolicyInputsAsync(
            Guid principalId,
            Guid policyId,
            int policyVersion,
            CancellationToken cancellationToken = default)
        {
            Policy = await inner.ReadPolicyInputsAsync(
                principalId,
                policyId,
                policyVersion,
                cancellationToken);
            return Policy;
        }
    }

    private sealed class RuntimeLogin : IAsyncDisposable
    {
        private readonly string _adminConnectionString;

        public RuntimeLogin(
            string adminConnectionString,
            string connectionString,
            string role)
        {
            _adminConnectionString = adminConnectionString;
            ConnectionString = connectionString;
            Role = role;
        }

        public string ConnectionString { get; }

        public string Role { get; }

        public async ValueTask DisposeAsync()
        {
            await using var connection = new NpgsqlConnection(_adminConnectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(
                $"""
                REVOKE tagekyc_runtime FROM {Quote(Role)};
                DROP ROLE {Quote(Role)};
                """,
                connection);
            await command.ExecuteNonQueryAsync();
        }
    }

    private sealed class IsolatedPostgres : IAsyncDisposable
    {
        private readonly string _containerName;

        private IsolatedPostgres(string containerName, string connectionString)
        {
            _containerName = containerName;
            ConnectionString = connectionString;
        }

        public string ConnectionString { get; }

        public static async Task<IsolatedPostgres> CreateAsync()
        {
            var containerName = $"tagekyc-e3-isolated-{Guid.NewGuid():N}";
            try
            {
                await RunDockerAsync(
                    "run",
                    "-d",
                    "--rm",
                    "--tmpfs",
                    "/var/lib/postgresql/data",
                    "--name",
                    containerName,
                    "-e",
                    "POSTGRES_DB=tagekyc_e3_isolated",
                    "-e",
                    "POSTGRES_USER=tagekyc",
                    "-e",
                    "POSTGRES_PASSWORD=tagekyc",
                    "-p",
                    "127.0.0.1::5432",
                    "postgres:16");

                var ready = false;
                for (var attempt = 0; attempt < 60; attempt++)
                {
                    var result = await RunDockerAsync(
                        allowFailure: true,
                        "exec",
                        containerName,
                        "pg_isready",
                        "-U",
                        "tagekyc",
                        "-d",
                        "tagekyc_e3_isolated");
                    if (result.ExitCode == 0)
                    {
                        ready = true;
                        break;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(1));
                }

                Assert.True(ready, "Isolated PostgreSQL did not become ready.");
                var portResult = await RunDockerAsync(
                    "port",
                    containerName,
                    "5432/tcp");
                var port = int.Parse(
                    portResult.StandardOutput.Trim().Split(':')[^1],
                    System.Globalization.CultureInfo.InvariantCulture);
                var connectionString =
                    $"Host=127.0.0.1;Port={port};Database=tagekyc_e3_isolated;" +
                    "Username=tagekyc;Password=tagekyc;Include Error Detail=true;Pooling=false";
                var sqlReady = false;
                for (var attempt = 0; attempt < 30; attempt++)
                {
                    try
                    {
                        await using var probe = new NpgsqlConnection(connectionString);
                        await probe.OpenAsync();
                        await using var command = new NpgsqlCommand("SELECT 1;", probe);
                        await command.ExecuteScalarAsync();
                        await Task.Delay(TimeSpan.FromMilliseconds(500));
                        await command.ExecuteScalarAsync();
                        sqlReady = true;
                        break;
                    }
                    catch (NpgsqlException)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(500));
                    }
                }

                Assert.True(
                    sqlReady,
                    "Isolated PostgreSQL did not sustain a stable SQL connection.");
                var isolated = new IsolatedPostgres(containerName, connectionString);
                await using var db = CreateDbContext(connectionString);
                var migrator = db.GetService<IMigrator>();
                await migrator.MigrateAsync("20260731130919_Tip88C1B2BetaExistingCandidates");

                await using (var bootstrap = new NpgsqlConnection(connectionString))
                {
                    await bootstrap.OpenAsync();
                    await using var transaction = await bootstrap.BeginTransactionAsync();
                    await using var command = bootstrap.CreateCommand();
                    command.Transaction = transaction;
                    command.CommandText = """
                        DO $bootstrap_roles$
                        DECLARE login_name text;
                        BEGIN
                            IF NOT EXISTS (
                                SELECT 1 FROM pg_catalog.pg_roles
                                WHERE rolname = 'tagekyc_raw_export_deployer') THEN
                                RAISE EXCEPTION 'DK-PROD disposable bootstrap requires landed deployer role';
                            END IF;

                            FOREACH login_name IN ARRAY ARRAY[
                                'tagekyc_raw_export_encryptor_login',
                                'tagekyc_raw_export_reconciler_login',
                                'tagekyc_raw_export_lifecycle_login',
                                'tagekyc_raw_export_assembly_resolver_login',
                                'tagekyc_raw_export_assembly_sealer_login',
                                'tagekyc_raw_export_package_preparer_login',
                                'tagekyc_raw_export_package_reconciler_login',
                                'tagekyc_raw_export_package_lifecycle_login',
                                'tagekyc_raw_export_package_delivery_login',
                                'tagekyc_raw_export_package_reference_login']
                            LOOP
                                IF EXISTS (
                                    SELECT 1 FROM pg_catalog.pg_roles
                                    WHERE rolname = login_name) THEN
                                    IF EXISTS (
                                        SELECT 1 FROM pg_catalog.pg_roles
                                        WHERE rolname = login_name
                                          AND (NOT rolcanlogin OR NOT rolinherit
                                            OR rolsuper OR rolcreatedb OR rolcreaterole
                                            OR rolreplication OR rolbypassrls)) THEN
                                        RAISE EXCEPTION 'DK-PROD disposable LOGIN role attributes invalid: %', login_name;
                                    END IF;
                                ELSE
                                    EXECUTE pg_catalog.format(
                                        'CREATE ROLE %I LOGIN INHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS PASSWORD NULL',
                                        login_name);
                                END IF;
                            END LOOP;
                        END
                        $bootstrap_roles$;

                        CREATE SCHEMA tagekyc_extensions;
                        CREATE EXTENSION pgcrypto WITH SCHEMA tagekyc_extensions;
                        REVOKE ALL ON SCHEMA tagekyc_extensions FROM PUBLIC;
                        REVOKE ALL ON FUNCTION tagekyc_extensions.gen_random_bytes(integer) FROM PUBLIC;
                        GRANT USAGE ON SCHEMA tagekyc_extensions TO tagekyc_raw_export_deployer;
                        GRANT EXECUTE ON FUNCTION tagekyc_extensions.gen_random_bytes(integer)
                            TO tagekyc_raw_export_deployer;
                        """;
                    await command.ExecuteNonQueryAsync();

                    command.CommandText = """
                        SELECT
                            (SELECT count(*) = 1
                             FROM pg_catalog.pg_extension e
                             JOIN pg_catalog.pg_namespace n ON n.oid = e.extnamespace
                             WHERE e.extname = 'pgcrypto'
                               AND n.nspname = 'tagekyc_extensions'
                               AND e.extowner = (SELECT oid FROM pg_catalog.pg_roles WHERE rolname = current_user))
                        AND (SELECT n.nspowner = (SELECT oid FROM pg_catalog.pg_roles WHERE rolname = current_user)
                             FROM pg_catalog.pg_namespace n
                             WHERE n.nspname = 'tagekyc_extensions')
                        AND (SELECT count(*) = 1
                             FROM pg_catalog.pg_proc p
                             JOIN pg_catalog.pg_namespace n ON n.oid = p.pronamespace
                             WHERE n.nspname = 'tagekyc_extensions'
                               AND p.proname = 'gen_random_bytes'
                               AND p.pronargs = 1
                               AND p.oid = 'tagekyc_extensions.gen_random_bytes(integer)'::pg_catalog.regprocedure
                               AND pg_catalog.pg_get_function_result(p.oid) = 'bytea'
                               AND p.proowner = (SELECT oid FROM pg_catalog.pg_roles WHERE rolname = current_user))
                        AND NOT EXISTS (
                            SELECT 1
                            FROM pg_catalog.pg_proc p
                            CROSS JOIN LATERAL pg_catalog.aclexplode(p.proacl) acl
                            WHERE p.oid = 'tagekyc_extensions.gen_random_bytes(integer)'::pg_catalog.regprocedure
                              AND acl.grantee = 0
                              AND acl.privilege_type = 'EXECUTE')
                        AND pg_catalog.has_schema_privilege(
                            'tagekyc_raw_export_deployer', 'tagekyc_extensions', 'USAGE')
                        AND pg_catalog.has_function_privilege(
                            'tagekyc_raw_export_deployer',
                            'tagekyc_extensions.gen_random_bytes(integer)', 'EXECUTE')
                        AND (SELECT count(*) = 3 AND pg_catalog.bool_and(
                                   rolcanlogin AND rolinherit
                                   AND NOT rolsuper AND NOT rolcreatedb AND NOT rolcreaterole
                                   AND NOT rolreplication AND NOT rolbypassrls)
                             FROM pg_catalog.pg_roles
                             WHERE rolname IN (
                                'tagekyc_raw_export_encryptor_login',
                                'tagekyc_raw_export_reconciler_login',
                                'tagekyc_raw_export_lifecycle_login'))
                        AND NOT EXISTS (
                            SELECT 1 FROM pg_catalog.pg_roles
                            WHERE rolname IN (
                                'tagekyc_raw_export_custody_encryptor',
                                'tagekyc_raw_export_reconciler',
                                'tagekyc_raw_export_lifecycle'))
                        AND NOT EXISTS (
                            SELECT 1
                            FROM pg_catalog.pg_auth_members membership
                            JOIN pg_catalog.pg_roles member_role ON member_role.oid = membership.member
                            WHERE member_role.rolname IN (
                                'tagekyc_raw_export_encryptor_login',
                                'tagekyc_raw_export_reconciler_login',
                                'tagekyc_raw_export_lifecycle_login'))
                        AND NOT EXISTS (
                            SELECT 1
                            FROM pg_catalog.pg_namespace namespace
                            CROSS JOIN LATERAL pg_catalog.aclexplode(namespace.nspacl) acl
                            JOIN pg_catalog.pg_roles grantee ON grantee.oid = acl.grantee
                            WHERE grantee.rolname IN (
                                'tagekyc_raw_export_encryptor_login',
                                'tagekyc_raw_export_reconciler_login',
                                'tagekyc_raw_export_lifecycle_login'))
                        AND NOT EXISTS (
                            SELECT 1
                            FROM pg_catalog.pg_proc function
                            CROSS JOIN LATERAL pg_catalog.aclexplode(function.proacl) acl
                            JOIN pg_catalog.pg_roles grantee ON grantee.oid = acl.grantee
                            WHERE grantee.rolname IN (
                                'tagekyc_raw_export_encryptor_login',
                                'tagekyc_raw_export_reconciler_login',
                                'tagekyc_raw_export_lifecycle_login'))
                        AND NOT EXISTS (
                            SELECT 1
                            FROM pg_catalog.pg_class relation
                            CROSS JOIN LATERAL pg_catalog.aclexplode(relation.relacl) acl
                            JOIN pg_catalog.pg_roles grantee ON grantee.oid = acl.grantee
                            WHERE grantee.rolname IN (
                                'tagekyc_raw_export_encryptor_login',
                                'tagekyc_raw_export_reconciler_login',
                                'tagekyc_raw_export_lifecycle_login'))
                        AND pg_catalog.octet_length(
                            tagekyc_extensions.gen_random_bytes(32)) = 32;
                        """;
                    var topologyReady = (bool)(await command.ExecuteScalarAsync() ?? false);
                    Assert.True(
                        topologyReady,
                        "Disposable DK-PROD prerequisite catalog topology was not exact.");
                    await transaction.CommitAsync();
                }

                await db.Database.MigrateAsync();
                return isolated;
            }
            catch
            {
                await RunDockerAsync(allowFailure: true, "rm", "-f", containerName);
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await RunDockerAsync(allowFailure: true, "rm", "-f", _containerName);
        }

        private static Task<DockerResult> RunDockerAsync(params string[] arguments) =>
            RunDockerAsync(false, arguments);

        private static async Task<DockerResult> RunDockerAsync(
            bool allowFailure,
            params string[] arguments)
        {
            var startInfo = new ProcessStartInfo("docker")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            foreach (var argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Failed to start Docker.");
            var standardOutput = await process.StandardOutput.ReadToEndAsync();
            var standardError = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            if (!allowFailure && process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"Docker exited {process.ExitCode}: {standardError}");
            }

            return new DockerResult(process.ExitCode, standardOutput, standardError);
        }

        private sealed record DockerResult(
            int ExitCode,
            string StandardOutput,
            string StandardError);
    }

    private sealed record FunctionManifest(
        string Name,
        string Signature,
        int Utf8Length,
        string Result,
        string BodySha256);

    private sealed record FulfillmentShapeCase(
        string Name,
        string EventId,
        string Revision,
        string EventType,
        string ArtifactRef,
        string ArtifactVersion,
        string ValidFrom,
        string ValidUntil);

    private sealed record ConstraintSession(Guid SessionId, string SubjectRef);

    private sealed record ConstraintModeFixture(
        Guid PolicyId,
        Guid RecorderPrincipalId,
        IReadOnlyList<ConstraintSession> Sessions,
        ConstraintSession ProbeSession);
}
