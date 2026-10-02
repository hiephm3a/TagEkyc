using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace TagEkyc.Infrastructure.Persistence;

public sealed class ApplicationPersistenceReadinessException(string code, Exception? innerException = null)
    : InvalidOperationException(code, innerException)
{
    public string Code { get; } = code;
}

public sealed class ApplicationPersistenceReadinessValidator(TagEkycDbContext dbContext)
{
    public const string RoleName = "tagekyc_application_persistence";
    public const string RoleInvalid = "PROD_APPLICATION_PERSISTENCE_ROLE_INVALID";
    public const string PrivilegeInvalid = "PROD_APPLICATION_PERSISTENCE_PRIVILEGE_INVALID";
    public const string RecipientFunctionInvalid = "PROD_APPLICATION_PERSISTENCE_RECIPIENT_FUNCTION_INVALID";

    private const string RecipientFunctionSignature =
        "tagekyc.raw_export_managed_recipient_scope_matches(uuid,uuid,uuid,text,bytea)";

    private const string ExpectedRecipientFunctionSourceSha256 =
        "3639AB57514727BD06C85BF69FFBFD8A1306D8D28B5EE1022FA735D12F4F831E";

    private static readonly string[] CoreTables =
    [
        "verification_sessions", "capture_artifacts", "evidence_results",
        "verification_decisions", "evidence_packages", "evidence_manifests",
        "audit_events", "append_idempotency_records"
    ];

    private static readonly string[] SessionUpdateColumns =
    [
        "State", "Result", "AssuranceLevel", "FinalDecisionId", "EvidencePackageId",
        "EvidencePackageHash", "ManifestHash", "RequestId", "CorrelationId", "CompletedAt"
    ];

    public async Task ValidateAsync(CancellationToken cancellationToken)
    {
        var connection = (NpgsqlConnection)dbContext.Database.GetDbConnection();
        try
        {
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(cancellationToken);
            if (!await RoleIsValidAsync(connection, cancellationToken))
                throw new ApplicationPersistenceReadinessException(RoleInvalid);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (ApplicationPersistenceReadinessException) { throw; }
        catch (Exception exception)
        {
            throw new ApplicationPersistenceReadinessException(RoleInvalid, exception);
        }

        try
        {
            if (!await PrivilegesAreValidAsync(connection, cancellationToken))
                throw new ApplicationPersistenceReadinessException(PrivilegeInvalid);
            if (!await RecipientFunctionIsValidAsync(connection, cancellationToken))
                throw new ApplicationPersistenceReadinessException(RecipientFunctionInvalid);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (ApplicationPersistenceReadinessException) { throw; }
        catch (Exception exception)
        {
            throw new ApplicationPersistenceReadinessException(PrivilegeInvalid, exception);
        }
    }

    private static async Task<bool> RoleIsValidAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("""
            WITH RECURSIVE roles AS (
              SELECT oid, rolname, rolcanlogin, rolsuper, rolcreatedb, rolcreaterole,
                     rolreplication, rolbypassrls, rolinherit
              FROM pg_catalog.pg_roles
              WHERE rolname IN ('tagekyc_runtime','tagekyc_application_persistence',session_user)
            ), caller AS (
              SELECT * FROM roles WHERE rolname=session_user
            ), runtime AS (
              SELECT * FROM roles WHERE rolname='tagekyc_runtime'
            ), application AS (
              SELECT * FROM roles WHERE rolname='tagekyc_application_persistence'
            ), reachable(roleid,path) AS (
              SELECT m.roleid, ARRAY[m.member,m.roleid]::oid[]
              FROM pg_catalog.pg_auth_members m
              WHERE m.member=(SELECT oid FROM caller)
              UNION ALL
              SELECT m.roleid, reachable.path || m.roleid
              FROM reachable
              JOIN pg_catalog.pg_auth_members m ON m.member=reachable.roleid
              WHERE NOT m.roleid=ANY(reachable.path)
            )
            SELECT
              (SELECT count(*) FROM caller)=1
              AND (SELECT rolcanlogin AND rolinherit AND NOT (rolsuper OR rolcreatedb OR
                   rolcreaterole OR rolreplication OR rolbypassrls) FROM caller)
              AND (SELECT count(*) FROM runtime)=1
              AND (SELECT count(*) FROM application)=1
              AND (SELECT NOT rolcanlogin AND rolinherit AND NOT (rolsuper OR rolcreatedb OR
                   rolcreaterole OR rolreplication OR rolbypassrls) FROM application)
              AND (SELECT array_agg(DISTINCT roleid ORDER BY roleid) FROM reachable)
                  = (SELECT array_agg(oid ORDER BY oid) FROM (SELECT oid FROM runtime UNION ALL SELECT oid FROM application) expected)
              AND (SELECT count(*) FROM pg_catalog.pg_auth_members m
                   WHERE m.member=(SELECT oid FROM caller)
                     AND m.roleid IN ((SELECT oid FROM runtime),(SELECT oid FROM application)))=2
              AND (SELECT bool_and(NOT admin_option AND inherit_option AND NOT set_option)
                   FROM pg_catalog.pg_auth_members m
                   WHERE m.member=(SELECT oid FROM caller)
                     AND m.roleid IN ((SELECT oid FROM runtime),(SELECT oid FROM application)))
              AND NOT EXISTS (
                SELECT 1 FROM pg_catalog.pg_auth_members m
                WHERE m.roleid=(SELECT oid FROM application)
                  AND m.member<>(SELECT oid FROM caller))
              AND NOT EXISTS (SELECT 1 FROM pg_catalog.pg_auth_members m
                              WHERE m.member IN ((SELECT oid FROM runtime),(SELECT oid FROM application)))
            """, connection);
        return await command.ExecuteScalarAsync(cancellationToken) is true;
    }

    private static async Task<bool> PrivilegesAreValidAsync(
        NpgsqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("""
            WITH expected_core(schema_name,relation_name) AS (
              SELECT 'tagekyc', pg_catalog.unnest(@core_tables::text[])
            ), expected_relations(schema_name,relation_name,can_insert) AS (
              SELECT schema_name,relation_name,true FROM expected_core
              UNION ALL SELECT 'tagekyc','api_keys',false
              UNION ALL SELECT 'public','__EFMigrationsHistory',false
            ), expected_update_columns(column_name) AS (
              SELECT pg_catalog.unnest(@update_columns::text[])
            ), runtime_forbidden(schema_name,relation_name) AS (
              SELECT schema_name,relation_name FROM expected_relations
            ), principals AS (
              SELECT oid,rolname FROM pg_catalog.pg_roles
              WHERE rolname IN ('tagekyc_runtime','tagekyc_application_persistence',session_user)
            ), runtime AS (
              SELECT oid FROM principals WHERE rolname='tagekyc_runtime'
            ), capability AS (
              SELECT oid FROM principals WHERE rolname='tagekyc_application_persistence'
            ), caller AS (
              SELECT oid FROM principals WHERE rolname=session_user
            ), current_database_row AS (
              SELECT datdba FROM pg_catalog.pg_database WHERE datname=current_database()
            ), user_namespaces AS (
              SELECT oid,nspname,nspowner,nspacl FROM pg_catalog.pg_namespace
              WHERE nspname NOT IN ('pg_catalog','information_schema')
                AND nspname NOT LIKE 'pg_toast%'
                AND nspname NOT LIKE 'pg_temp%'
            ), user_relations AS (
              SELECT c.oid,n.nspname,c.relname,c.relkind,c.relowner,c.relacl
              FROM pg_catalog.pg_class c JOIN user_namespaces n ON n.oid=c.relnamespace
              WHERE c.relkind IN ('r','p','v','m','f')
            ), expected_actual AS (
              SELECT e.*,a.oid,a.relkind
              FROM expected_relations e
              LEFT JOIN user_relations a ON a.nspname=e.schema_name AND a.relname=e.relation_name
            )
            SELECT
              (SELECT count(*) FROM expected_core)=8
              AND (SELECT count(*) FROM current_database_row)=1
              AND NOT pg_catalog.has_database_privilege(session_user,current_database(),'CREATE')
              AND NOT pg_catalog.has_database_privilege('tagekyc_runtime',current_database(),'CREATE')
              AND NOT pg_catalog.has_database_privilege('tagekyc_application_persistence',current_database(),'CREATE')
              AND (SELECT datdba NOT IN (
                    (SELECT oid FROM caller),(SELECT oid FROM runtime),(SELECT oid FROM capability))
                   FROM current_database_row)
              AND pg_catalog.has_schema_privilege('tagekyc_application_persistence','tagekyc','USAGE')
              AND pg_catalog.has_schema_privilege('tagekyc_application_persistence','public','USAGE')
              AND NOT pg_catalog.has_schema_privilege('tagekyc_application_persistence','tagekyc','CREATE')
              AND NOT pg_catalog.has_schema_privilege('tagekyc_application_persistence','public','CREATE')
              AND NOT EXISTS (
                SELECT 1 FROM user_namespaces n
                WHERE n.nspowner IN ((SELECT oid FROM capability),(SELECT oid FROM caller)))
              AND NOT EXISTS (
                SELECT 1 FROM user_namespaces n
                CROSS JOIN LATERAL pg_catalog.aclexplode(n.nspacl) acl
                WHERE acl.grantee=(SELECT oid FROM caller)
                   OR (acl.grantee=(SELECT oid FROM capability)
                       AND (n.nspname NOT IN ('tagekyc','public') OR acl.privilege_type<>'USAGE'
                            OR acl.is_grantable)))
              AND NOT EXISTS (
                SELECT 1 FROM expected_actual a
                WHERE a.oid IS NULL OR a.relkind NOT IN ('r','p')
                   OR NOT pg_catalog.has_table_privilege('tagekyc_application_persistence',a.oid,'SELECT')
                   OR (a.can_insert <> pg_catalog.has_table_privilege('tagekyc_application_persistence',a.oid,'INSERT'))
                   OR pg_catalog.has_table_privilege('tagekyc_application_persistence',a.oid,'UPDATE')
                   OR pg_catalog.has_table_privilege('tagekyc_application_persistence',a.oid,'DELETE')
                   OR pg_catalog.has_table_privilege('tagekyc_application_persistence',a.oid,'TRUNCATE')
                   OR pg_catalog.has_table_privilege('tagekyc_application_persistence',a.oid,'REFERENCES')
                   OR pg_catalog.has_table_privilege('tagekyc_application_persistence',a.oid,'TRIGGER')
                   OR pg_catalog.has_any_column_privilege('tagekyc_application_persistence',a.oid,'REFERENCES'))
              AND NOT EXISTS (
                SELECT 1 FROM user_relations r
                CROSS JOIN LATERAL pg_catalog.aclexplode(r.relacl) acl
                LEFT JOIN expected_relations e ON e.schema_name=r.nspname AND e.relation_name=r.relname
                WHERE acl.grantee=0
                   OR acl.grantee=(SELECT oid FROM caller)
                   OR (acl.grantee=(SELECT oid FROM capability) AND e.relation_name IS NULL)
                   OR (acl.grantee=(SELECT oid FROM capability) AND e.relation_name IS NOT NULL
                       AND ((acl.privilege_type='SELECT' OR (acl.privilege_type='INSERT' AND e.can_insert)) IS NOT TRUE
                            OR acl.is_grantable)))
              AND NOT EXISTS (
                SELECT 1 FROM user_relations r
                WHERE r.relowner IN ((SELECT oid FROM capability),(SELECT oid FROM caller)))
              AND NOT EXISTS (
                SELECT 1 FROM pg_catalog.pg_attribute a
                JOIN user_relations r ON r.oid=a.attrelid
                CROSS JOIN LATERAL pg_catalog.aclexplode(a.attacl) acl
                WHERE a.attnum>0 AND NOT a.attisdropped
                  AND (acl.grantee=(SELECT oid FROM caller)
                    OR (acl.grantee=(SELECT oid FROM capability)
                        AND NOT (r.nspname='tagekyc' AND r.relname='verification_sessions'
                          AND acl.privilege_type='UPDATE'
                          AND NOT acl.is_grantable
                          AND EXISTS (SELECT 1 FROM expected_update_columns e WHERE e.column_name=a.attname)))))
              AND NOT EXISTS (
                SELECT 1 FROM pg_catalog.pg_attribute a
                JOIN user_relations r ON r.oid=a.attrelid
                WHERE r.nspname='tagekyc' AND r.relname='verification_sessions'
                  AND a.attnum>0 AND NOT a.attisdropped
                  AND pg_catalog.has_column_privilege('tagekyc_application_persistence',r.oid,a.attnum,'UPDATE')
                      <> EXISTS (SELECT 1 FROM expected_update_columns e WHERE e.column_name=a.attname))
              AND NOT EXISTS (
                SELECT 1 FROM runtime_forbidden e JOIN user_relations r
                  ON r.nspname=e.schema_name AND r.relname=e.relation_name
                WHERE pg_catalog.has_table_privilege('tagekyc_runtime',r.oid,
                    'SELECT,INSERT,UPDATE,DELETE,TRUNCATE,REFERENCES,TRIGGER')
                   OR pg_catalog.has_any_column_privilege('tagekyc_runtime',r.oid,
                    'SELECT,INSERT,UPDATE,REFERENCES'))
              AND NOT EXISTS (
                SELECT 1 FROM pg_catalog.pg_class s JOIN user_namespaces n ON n.oid=s.relnamespace
                CROSS JOIN LATERAL pg_catalog.aclexplode(s.relacl) acl
                WHERE s.relkind='S' AND acl.grantee IN ((SELECT oid FROM capability),(SELECT oid FROM caller)))
              AND NOT EXISTS (
                SELECT 1 FROM pg_catalog.pg_class s JOIN user_namespaces n ON n.oid=s.relnamespace
                WHERE s.relkind='S'
                  AND s.relowner IN ((SELECT oid FROM capability),(SELECT oid FROM caller)))
              AND NOT EXISTS (
                SELECT 1 FROM pg_catalog.pg_proc p JOIN user_namespaces n ON n.oid=p.pronamespace
                CROSS JOIN LATERAL pg_catalog.aclexplode(p.proacl) acl
                WHERE acl.grantee=(SELECT oid FROM caller)
                   OR (acl.grantee=(SELECT oid FROM capability)
                       AND (p.oid<>pg_catalog.to_regprocedure(@recipient_function)
                            OR acl.privilege_type<>'EXECUTE' OR acl.is_grantable)))
              AND NOT EXISTS (
                SELECT 1 FROM pg_catalog.pg_proc p JOIN user_namespaces n ON n.oid=p.pronamespace
                WHERE p.proowner IN ((SELECT oid FROM capability),(SELECT oid FROM caller)))
            """, connection);
        command.Parameters.AddWithValue("core_tables", CoreTables);
        command.Parameters.AddWithValue("update_columns", SessionUpdateColumns);
        command.Parameters.AddWithValue("recipient_function", RecipientFunctionSignature);
        return await command.ExecuteScalarAsync(cancellationToken) is true;
    }

    private static async Task<bool> RecipientFunctionIsValidAsync(
        NpgsqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("""
            SELECT owner.rolname,
                   p.prosecdef,
                   COALESCE(p.proconfig=ARRAY['search_path=pg_catalog']::text[],false),
                   p.prosrc,
                   (SELECT count(*)=1 AND bool_and(
                       acl.grantee='tagekyc_application_persistence'::pg_catalog.regrole::oid
                       AND acl.privilege_type='EXECUTE'
                       AND NOT acl.is_grantable)
                    FROM pg_catalog.aclexplode(p.proacl) acl
                    WHERE acl.grantee<>p.proowner)
            FROM pg_catalog.pg_proc p
            JOIN pg_catalog.pg_roles owner ON owner.oid=p.proowner
            WHERE p.oid=pg_catalog.to_regprocedure(@signature)
            """, connection);
        command.Parameters.AddWithValue("signature", RecipientFunctionSignature);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)
            || !string.Equals(reader.GetString(0), "tagekyc_raw_export_deployer", StringComparison.Ordinal)
            || !reader.GetBoolean(1)
            || !reader.GetBoolean(2)
            || !reader.GetBoolean(4))
            return false;

        var actual = reader.GetString(3).Replace("\r\n", "\n", StringComparison.Ordinal);
        var hashesMatch = CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(actual)),
            Convert.FromHexString(ExpectedRecipientFunctionSourceSha256));
        return hashesMatch && !await reader.ReadAsync(cancellationToken);
    }
}
