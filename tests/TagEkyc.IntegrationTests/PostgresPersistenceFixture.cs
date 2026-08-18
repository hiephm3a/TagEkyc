using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.IntegrationTests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PostgresPersistenceCollection : ICollectionFixture<PostgresPersistenceFixture>
{
    public const string Name = "PostgresPersistence";
}

public sealed class PostgresPersistenceFixture : IAsyncLifetime
{
    private const string DurableKeyPrerequisiteMigration =
        "20260731130919_Tip88C1B2BetaExistingCandidates";

    public string ConnectionString { get; } =
        Environment.GetEnvironmentVariable("TAGEKYC_POSTGRES_TEST_CONNECTION_STRING") ??
        "Host=localhost;Port=55432;Database=tagekyc_persistence_tests;Username=tagekyc;Password=tagekyc;Include Error Detail=true";

    public async Task InitializeAsync()
    {
        await RunDockerComposeAsync("up -d --wait");
        await ResetDatabaseAsync();
    }

    public async Task DisposeAsync() => await RunDockerComposeAsync("down");

    public async Task ResetDatabaseAsync()
    {
        await BootstrapClusterPrerequisitesAsync();
        await EnsureTemplateGrantPrincipalExistsAsync();
        await BootstrapTemplateDatabasePrerequisitesAsync();
        await using var db = CreateDbContext();
        await db.Database.EnsureDeletedAsync();
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync();
        await VerifyDatabasePrerequisitesAsync();
    }

    public TagEkycDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TagEkycDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new TagEkycDbContext(options);
    }

    public async Task AssertLatestMigrationAsync(string testName)
    {
        await using var db = CreateDbContext();
        var expectedLatest = db.Database.GetMigrations().Last();
        var actualLatest = (await db.Database.GetAppliedMigrationsAsync()).LastOrDefault() ?? "<none>";

        if (!StringComparer.Ordinal.Equals(actualLatest, expectedLatest))
        {
            throw new InvalidOperationException(
                $"Shared database migration-state leak: test '{testName}' left the database at " +
                $"'{actualLatest}'; expected latest '{expectedLatest}'.");
        }
    }

    private async Task BootstrapClusterPrerequisitesAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            DO $bootstrap_roles$
            DECLARE
                login_name text;
                role_row record;
            BEGIN
                FOREACH login_name IN ARRAY ARRAY[
                    'tagekyc_raw_export_encryptor_login',
                    'tagekyc_raw_export_reconciler_login',
                    'tagekyc_raw_export_lifecycle_login',
                    'tagekyc_raw_export_assembly_resolver_login',
                    'tagekyc_raw_export_assembly_sealer_login',
                    'tagekyc_raw_export_package_preparer_login',
                    'tagekyc_raw_export_package_reconciler_login',
                    'tagekyc_raw_export_package_lifecycle_login']
                LOOP
                    SELECT
                        rolcanlogin,
                        rolinherit,
                        rolsuper,
                        rolcreatedb,
                        rolcreaterole,
                        rolreplication,
                        rolbypassrls,
                        rolpassword
                    INTO role_row
                    FROM pg_catalog.pg_authid
                    WHERE rolname = login_name;

                    IF NOT FOUND THEN
                        EXECUTE pg_catalog.format(
                            'CREATE ROLE %I LOGIN INHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS PASSWORD NULL',
                            login_name);
                    ELSIF NOT role_row.rolcanlogin OR NOT role_row.rolinherit
                       OR role_row.rolsuper OR role_row.rolcreatedb
                       OR role_row.rolcreaterole OR role_row.rolreplication
                       OR role_row.rolbypassrls OR role_row.rolpassword IS NOT NULL THEN
                        RAISE EXCEPTION USING
                            ERRCODE = 'P0001',
                            MESSAGE = 'DK-PROD canonical LOGIN role attributes invalid: ' || login_name;
                    END IF;
                END LOOP;
            END
            $bootstrap_roles$;
            """;
        await command.ExecuteNonQueryAsync();
    }

    private async Task EnsureTemplateGrantPrincipalExistsAsync()
    {
        await using (var connection = new NpgsqlConnection(ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT pg_catalog.count(*) = 1 FROM pg_catalog.pg_roles WHERE rolname = 'tagekyc_raw_export_deployer';";
            if (await command.ExecuteScalarAsync() is true)
                return;
        }

        await using var db = CreateDbContext();
        await db.Database.GetService<IMigrator>().MigrateAsync(DurableKeyPrerequisiteMigration);

        await using var verification = new NpgsqlConnection(ConnectionString);
        await verification.OpenAsync();
        await using var verifyCommand = verification.CreateCommand();
        verifyCommand.CommandText = "SELECT pg_catalog.count(*) = 1 FROM pg_catalog.pg_roles WHERE rolname = 'tagekyc_raw_export_deployer';";
        if (await verifyCommand.ExecuteScalarAsync() is not true)
            throw new InvalidOperationException("DK-PROD canonical deployer role was not established by its owning migration.");
    }

    private async Task BootstrapTemplateDatabasePrerequisitesAsync()
    {
        var builder = new NpgsqlConnectionStringBuilder(ConnectionString)
        {
            Database = "template1",
        };

        await BootstrapDatabasePrerequisitesAsync(builder.ConnectionString);
    }

    private static async Task BootstrapDatabasePrerequisitesAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            DO $bootstrap_pgcrypto$
            DECLARE
                owner_oid oid := (SELECT oid FROM pg_catalog.pg_roles WHERE rolname = current_user);
                schema_oid oid;
                extension_row record;
            BEGIN
                SELECT oid INTO schema_oid
                FROM pg_catalog.pg_namespace
                WHERE nspname = 'tagekyc_extensions';

                IF schema_oid IS NULL THEN
                    CREATE SCHEMA tagekyc_extensions;
                ELSIF NOT EXISTS (
                    SELECT 1 FROM pg_catalog.pg_namespace
                    WHERE oid = schema_oid AND nspowner = owner_oid) THEN
                    RAISE EXCEPTION USING ERRCODE='P0001',
                        MESSAGE='DK-PROD canonical extension schema owner invalid';
                END IF;

                SELECT e.extowner, n.nspname
                INTO extension_row
                FROM pg_catalog.pg_extension e
                JOIN pg_catalog.pg_namespace n ON n.oid = e.extnamespace
                WHERE e.extname = 'pgcrypto';

                IF NOT FOUND THEN
                    CREATE EXTENSION pgcrypto WITH SCHEMA tagekyc_extensions;
                    REVOKE ALL ON SCHEMA tagekyc_extensions FROM PUBLIC;
                    REVOKE ALL ON FUNCTION tagekyc_extensions.gen_random_bytes(integer) FROM PUBLIC;
                    GRANT USAGE ON SCHEMA tagekyc_extensions TO tagekyc_raw_export_deployer;
                    GRANT EXECUTE ON FUNCTION tagekyc_extensions.gen_random_bytes(integer)
                        TO tagekyc_raw_export_deployer;
                ELSIF extension_row.extowner <> owner_oid
                   OR extension_row.nspname <> 'tagekyc_extensions' THEN
                    RAISE EXCEPTION USING ERRCODE='P0001',
                        MESSAGE='DK-PROD canonical pgcrypto placement or owner invalid';
                END IF;
            END
            $bootstrap_pgcrypto$;
            """;
        await command.ExecuteNonQueryAsync();
        await transaction.CommitAsync();

        if (!await DatabasePrerequisitesAreExactAsync(connection, requireCapabilityIsolation: false))
            throw new InvalidOperationException("DK-PROD canonical database prerequisite catalog is not exact.");
    }

    private async Task VerifyDatabasePrerequisitesAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        if (!await DatabasePrerequisitesAreExactAsync(connection, requireCapabilityIsolation: true))
            throw new InvalidOperationException("DK-PROD canonical post-migration CSPRNG catalog is not exact.");
    }

    private static async Task<bool> DatabasePrerequisitesAreExactAsync(
        NpgsqlConnection connection,
        bool requireCapabilityIsolation)
    {
        await using var command = connection.CreateCommand();
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
            AND NOT pg_catalog.has_schema_privilege('public', 'tagekyc_extensions', 'USAGE')
            AND NOT pg_catalog.has_schema_privilege('public', 'tagekyc_extensions', 'CREATE')
            AND NOT pg_catalog.has_function_privilege(
                'public', 'tagekyc_extensions.gen_random_bytes(integer)', 'EXECUTE')
            AND pg_catalog.has_schema_privilege(
                'tagekyc_raw_export_deployer', 'tagekyc_extensions', 'USAGE')
            AND pg_catalog.has_function_privilege(
                'tagekyc_raw_export_deployer',
                'tagekyc_extensions.gen_random_bytes(integer)', 'EXECUTE')
            AND NOT EXISTS (
                SELECT 1
                FROM pg_catalog.pg_proc p
                CROSS JOIN LATERAL pg_catalog.aclexplode(
                    COALESCE(p.proacl, pg_catalog.acldefault('f', p.proowner))) acl
                WHERE p.oid = 'tagekyc_extensions.gen_random_bytes(integer)'::pg_catalog.regprocedure
                  AND acl.grantee = 0)
            AND pg_catalog.octet_length(tagekyc_extensions.gen_random_bytes(32)) = 32
            AND (NOT @require_capability_isolation OR (
                NOT pg_catalog.has_schema_privilege('tagekyc_runtime', 'tagekyc_extensions', 'USAGE')
                AND NOT pg_catalog.has_function_privilege(
                    'tagekyc_runtime', 'tagekyc_extensions.gen_random_bytes(integer)', 'EXECUTE')
                AND NOT pg_catalog.has_function_privilege(
                    'tagekyc_raw_export_custody_encryptor',
                    'tagekyc_extensions.gen_random_bytes(integer)', 'EXECUTE')
                AND NOT pg_catalog.has_function_privilege(
                    'tagekyc_raw_export_reconciler',
                    'tagekyc_extensions.gen_random_bytes(integer)', 'EXECUTE')
                AND NOT pg_catalog.has_function_privilege(
                    'tagekyc_raw_export_lifecycle',
                    'tagekyc_extensions.gen_random_bytes(integer)', 'EXECUTE')))
            AND NOT EXISTS (
                SELECT 1
                FROM pg_catalog.pg_proc shadow
                WHERE shadow.proname = 'gen_random_bytes'
                  AND shadow.pronargs = 1
                  AND shadow.oid <> 'tagekyc_extensions.gen_random_bytes(integer)'::pg_catalog.regprocedure);
            """;
        command.Parameters.AddWithValue("require_capability_isolation", requireCapabilityIsolation);
        return await command.ExecuteScalarAsync() is true;
    }

    private static async Task RunDockerComposeAsync(string arguments)
    {
        var startInfo = new ProcessStartInfo("docker", $"compose -f docker-compose.persistence-tests.yml {arguments}")
        {
            WorkingDirectory = FindRepoRoot(),
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start docker compose.");
        var completed = await Task.Run(() => process.WaitForExit(120_000));
        if (!completed)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
            }

            throw new TimeoutException("Timed out while starting Postgres test container.");
        }

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"docker compose failed with exit code {process.ExitCode}.{Environment.NewLine}{output}{Environment.NewLine}{error}");
        }
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "docker-compose.persistence-tests.yml")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root.");
    }
}
