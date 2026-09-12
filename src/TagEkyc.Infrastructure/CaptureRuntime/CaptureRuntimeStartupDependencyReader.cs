using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NpgsqlTypes;
using TagEkyc.Application.CaptureRuntime;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.Infrastructure.CaptureRuntime;

public sealed class CaptureRuntimeStartupDependencyReader(TagEkycDbContext ordinary,
    ICaptureRuntimeDbContextFactory onlineFactory, ICaptureRuntimeOperatorDbContextFactory operatorFactory)
    : ICaptureRuntimeStartupDependencyReader
{
    public async Task<CaptureRuntimeStartupDependencies> ReadAsync(DateTimeOffset now, CancellationToken ct)
    {
        try
        {
            await new PostgresProductionReadinessValidator(ordinary).ValidateAsync(ct);
            await using var online = await onlineFactory.CreateAsync(ct);
            await using var management = await operatorFactory.CreateAsync(ct);
            var identities = new List<string>();
            string? database = null;
            (string Host, int Port, string Database)? endpoint = null;
            foreach (var (db, roles) in new[] {
                (ordinary, new[] { "tagekyc_runtime" }),
                (online, new[] { "tagekyc_capture_runtime_application", "tagekyc_capture_runtime_authenticator" }),
                (management, new[] { "tagekyc_capture_runtime_operator" }) })
            {
                await db.Database.OpenConnectionAsync(ct);
                var connection = (NpgsqlConnection)db.Database.GetDbConnection();
                var currentEndpoint = (connection.Host ?? throw NotReady(), connection.Port, connection.Database);
                if (endpoint is not null && endpoint.Value != currentEndpoint) throw NotReady();
                endpoint = currentEndpoint;
                await using var identity = new NpgsqlCommand("SELECT session_user::text,current_database()::text", connection);
                await using (var reader = await identity.ExecuteReaderAsync(ct))
                {
                    if (!await reader.ReadAsync(ct)) throw NotReady();
                    identities.Add(reader.GetString(0));
                    var currentDatabase = reader.GetString(1);
                    if (database is not null && database != currentDatabase) throw NotReady();
                    database = currentDatabase;
                }
                await ValidateIdentityAsync(connection, roles, ct);
                await ValidateCatalogueAsync(connection, ct);
            }
            if (identities.Distinct(StringComparer.Ordinal).Count() != 3) throw NotReady();
            await using var transaction = await online.Database.BeginTransactionAsync(ct);
            await online.Database.ExecuteSqlRawAsync("SET LOCAL ROLE tagekyc_capture_runtime_application", ct);
            await using var command = new NpgsqlCommand(
                "SELECT * FROM tagekyc.capture_runtime_read_cutover_state(@profile,@now)",
                (NpgsqlConnection)online.Database.GetDbConnection(),
                (NpgsqlTransaction)transaction.GetDbTransaction());
            command.Parameters.AddWithValue("profile", NpgsqlDbType.Text, "Managed");
            command.Parameters.AddWithValue("now", NpgsqlDbType.TimestampTz, now);
            CaptureRuntimeStartupDependencies result;
            await using (var reader = await command.ExecuteReaderAsync(ct))
            {
                if (reader.FieldCount != 7 || !await reader.ReadAsync(ct) ||
                    Enumerable.Range(0, 4).Any(reader.IsDBNull) || reader.IsDBNull(6)) throw NotReady();
                result = new(reader.GetString(0), reader.GetString(1), reader.GetInt64(2),
                    reader.GetFieldValue<DateTimeOffset>(3), reader.IsDBNull(4) ? null : reader.GetFieldValue<DateTimeOffset>(4),
                    reader.IsDBNull(5) ? null : reader.GetGuid(5), reader.GetFieldValue<int[]>(6));
                if (await reader.ReadAsync(ct)) throw NotReady();
            }
            await transaction.RollbackAsync(ct);
            return result;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception) { throw NotReady(); }
    }

    private static async Task ValidateIdentityAsync(NpgsqlConnection connection, string[] expectedRoles, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("""
            SELECT r.rolname::text FROM pg_catalog.pg_roles r
            WHERE pg_catalog.pg_has_role(session_user,r.oid,'MEMBER') AND r.rolname<>session_user
            ORDER BY r.rolname
            """, connection);
        var actual = new List<string>();
        await using (var reader = await command.ExecuteReaderAsync(ct))
            while (await reader.ReadAsync(ct)) actual.Add(reader.GetString(0));
        if (!actual.SequenceEqual(expectedRoles.Order(StringComparer.Ordinal))) throw NotReady();
        command.CommandText = """
            SELECT NOT (rolsuper OR rolcreatedb OR rolcreaterole OR rolreplication OR rolbypassrls)
            AND rolcanlogin FROM pg_catalog.pg_roles WHERE rolname=session_user
            """;
        if (await command.ExecuteScalarAsync(ct) is not true) throw NotReady();
        command.CommandText = """
            SELECT count(*) FROM pg_catalog.pg_roles
            WHERE rolname=ANY(@roles) AND NOT (rolcanlogin OR rolsuper OR rolcreatedb OR
              rolcreaterole OR rolreplication OR rolbypassrls) AND rolinherit
            """;
        command.Parameters.AddWithValue("roles", expectedRoles);
        if (await command.ExecuteScalarAsync(ct) is not long safeRoles || safeRoles != expectedRoles.Length) throw NotReady();
        command.Parameters.Clear();
        // The online/operator service identities may only enter closed capability roles.
        command.CommandText = """
            SELECT NOT EXISTS (
              SELECT 1 FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
              WHERE n.nspname='tagekyc' AND c.relname=ANY(@tables)
                AND pg_catalog.has_table_privilege(session_user,c.oid,'SELECT,INSERT,UPDATE,DELETE,TRUNCATE,REFERENCES,TRIGGER'))
            """;
        command.Parameters.AddWithValue("tables", CaptureRuntimeStartupCatalogue.Tables);
        if (await command.ExecuteScalarAsync(ct) is not true) throw NotReady();
    }

    private static async Task ValidateCatalogueAsync(NpgsqlConnection connection, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("""
            SELECT c.relname::text FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            WHERE n.nspname='tagekyc' AND c.relname=ANY(@tables) AND c.relkind='r'
              AND pg_catalog.pg_get_userbyid(c.relowner)='tagekyc_raw_export_deployer' ORDER BY c.relname
            """, connection);
        command.Parameters.AddWithValue("tables", CaptureRuntimeStartupCatalogue.Tables);
        var tables = new List<string>();
        await using (var reader = await command.ExecuteReaderAsync(ct))
            while (await reader.ReadAsync(ct)) tables.Add(reader.GetString(0));
        if (!tables.SequenceEqual(CaptureRuntimeStartupCatalogue.Tables.Order(StringComparer.Ordinal))) throw NotReady();
        command.Parameters.Clear();
        command.CommandText = """
            SELECT pg_catalog.format('%I.%I(%s)',n.nspname,p.proname,pg_catalog.oidvectortypes(p.proargtypes)),
              pg_catalog.pg_get_userbyid(p.proowner), p.prosecdef,
              p.proconfig @> ARRAY['search_path=pg_catalog'],
              COALESCE((SELECT string_agg(CASE WHEN a.grantee=0 THEN 'PUBLIC' ELSE pg_catalog.pg_get_userbyid(a.grantee) END,','
                ORDER BY CASE WHEN a.grantee=0 THEN 'PUBLIC' ELSE pg_catalog.pg_get_userbyid(a.grantee) END)
                FROM pg_catalog.aclexplode(COALESCE(p.proacl,pg_catalog.acldefault('f',p.proowner))) a
                WHERE a.privilege_type='EXECUTE' AND a.grantee<>p.proowner),'')
            FROM pg_catalog.pg_proc p JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
            WHERE n.nspname='tagekyc' AND p.proname=ANY(@names)
            """;
        command.Parameters.AddWithValue("names", CaptureRuntimeStartupCatalogue.Functions
            .Select(s => s["tagekyc.".Length..s.IndexOf('(')]).Distinct().ToArray());
        var grants = new List<string>();
        await using (var reader = await command.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                if (reader.GetString(1) != "tagekyc_raw_export_deployer" || reader.IsDBNull(3) || !reader.GetBoolean(3)) throw NotReady();
                var key = reader.GetString(0);
                // All external callables are definer functions. Trigger/pure internal helpers retain their ratified security kind.
                var grant = reader.GetString(4);
                if (grant.Length > 0 && !reader.GetBoolean(2)) throw NotReady();
                grants.Add(key + "|" + grant);
            }
        }
        if (grants.Count != 47 || !grants.Order(StringComparer.Ordinal)
            .SequenceEqual(CaptureRuntimeStartupCatalogue.Grants.Order(StringComparer.Ordinal))) throw NotReady();
    }

    private static InvalidOperationException NotReady() => new("CAPTURE_RUNTIME_STARTUP_NOT_READY");
}
