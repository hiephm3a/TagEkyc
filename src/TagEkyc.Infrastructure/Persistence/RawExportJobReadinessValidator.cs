using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.Infrastructure.Persistence;

public sealed class RawExportJobReadinessException(string code) : InvalidOperationException(code)
{
    public string Code { get; } = code;
}

public sealed class RawExportJobReadinessValidator(
    TagEkycDbContext dbContext,
    RawExportJobLeaseState leaseState)
{
    public const string SchemaInvalid = "PROD_RAW_EXPORT_JOB_SCHEMA_INVALID";
    public const string FunctionAclInvalid = "PROD_RAW_EXPORT_JOB_FUNCTION_ACL_INVALID";
    public const string TablePrivilegeInvalid = "PROD_RAW_EXPORT_JOB_TABLE_PRIVILEGE_INVALID";

    private const int ExpectedSchemaRows = 114;
    private const string ExpectedSchemaDigest = "dc1208f09606eede7f76d12be233ef40";
    private const int ExpectedFunctionCount = 14;
    private const string ExpectedFunctionDigest = "40dc8b061e399f3c68fcfacc393106d6";

    public async Task ValidateAsync(CancellationToken cancellationToken)
    {
        if (!leaseState.IsValid || leaseState.LeaseSeconds is < 10 or > 300)
        {
            throw new RawExportJobReadinessException(RawExportJobLeaseOptions.InvalidCode);
        }

        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var schema = await ReadDigestAsync(connection, SchemaDigestSql, cancellationToken);
        if (schema.Count != ExpectedSchemaRows ||
            !string.Equals(schema.Digest, ExpectedSchemaDigest, StringComparison.Ordinal))
        {
            throw new RawExportJobReadinessException(SchemaInvalid);
        }

        var functions = await ReadDigestAsync(connection, FunctionDigestSql, cancellationToken);
        if (functions.Count != ExpectedFunctionCount ||
            !string.Equals(functions.Digest, ExpectedFunctionDigest, StringComparison.Ordinal) ||
            !await HasExactFunctionAclAsync(connection, cancellationToken))
        {
            throw new RawExportJobReadinessException(FunctionAclInvalid);
        }

        if (!await HasExactTableAndColumnAclAsync(connection, cancellationToken))
        {
            throw new RawExportJobReadinessException(TablePrivilegeInvalid);
        }
    }

    private static async Task<(int Count, string Digest)> ReadDigestAsync(
        System.Data.Common.DbConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken) || reader.IsDBNull(0) || reader.IsDBNull(1))
        {
            return (-1, string.Empty);
        }

        var result = (reader.GetInt32(0), reader.GetString(1));
        return await reader.ReadAsync(cancellationToken) ? (-1, string.Empty) : result;
    }

    private static async Task<bool> HasExactFunctionAclAsync(
        System.Data.Common.DbConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            WITH b4 AS (
              SELECT p.oid,p.proowner,p.proname,
                     owner_role.rolname,owner_role.rolcanlogin,owner_role.rolinherit,
                     owner_role.rolsuper,owner_role.rolcreatedb,owner_role.rolcreaterole,
                     owner_role.rolreplication,owner_role.rolbypassrls
              FROM pg_catalog.pg_proc p
              JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
              JOIN pg_catalog.pg_roles owner_role ON owner_role.oid=p.proowner
              WHERE n.nspname='tagekyc'
                AND p.proname IN (
                    'enforce_raw_export_job_attempt_insert',
                    'enforce_raw_export_job_class_insert',
                    'enforce_raw_export_job_head_mutation',
                    'enforce_raw_export_job_identity_has_classes',
                    'enforce_raw_export_job_identity_insert',
                    'enforce_raw_export_job_transition_insert',
                    'raw_export_acquire_or_reclaim_job_lease',
                    'raw_export_claim_or_read_job',
                    'raw_export_lock_job_for_attempt',
                    'raw_export_read_job',
                    'raw_export_read_job_binding_inputs',
                    'raw_export_record_job_attempt_failure',
                    'raw_export_renew_job_lease',
                    'raw_export_terminalize_job')
            ),
            nonowner AS (
              SELECT b.proname,x.grantor,x.grantee,x.privilege_type,x.is_grantable
              FROM b4 b
              JOIN pg_catalog.pg_proc p ON p.oid=b.oid
              CROSS JOIN LATERAL pg_catalog.aclexplode(
                COALESCE(p.proacl,pg_catalog.acldefault('f',p.proowner))) x
              WHERE x.grantee<>p.proowner
            )
            SELECT
              (SELECT count(*) FROM b4)=14
              AND NOT EXISTS (
                SELECT 1 FROM b4
                WHERE rolname<>'tagekyc_raw_export_deployer'
                   OR rolcanlogin OR NOT rolinherit OR rolsuper OR rolcreatedb
                   OR rolcreaterole OR rolreplication OR rolbypassrls)
              AND (SELECT count(*) FROM nonowner)=8
              AND NOT EXISTS (
                SELECT 1 FROM nonowner x
                WHERE x.proname NOT IN (
                    'raw_export_read_job_binding_inputs',
                    'raw_export_claim_or_read_job',
                    'raw_export_read_job',
                    'raw_export_lock_job_for_attempt',
                    'raw_export_acquire_or_reclaim_job_lease',
                    'raw_export_renew_job_lease',
                    'raw_export_record_job_attempt_failure',
                    'raw_export_terminalize_job')
                   OR x.grantor<>(SELECT oid FROM pg_catalog.pg_roles WHERE rolname='tagekyc_raw_export_deployer')
                   OR x.grantee<>(SELECT oid FROM pg_catalog.pg_roles WHERE rolname='tagekyc_runtime')
                   OR x.privilege_type<>'EXECUTE'
                   OR x.is_grantable);
            """;
        return await command.ExecuteScalarAsync(cancellationToken) is true;
    }

    private static async Task<bool> HasExactTableAndColumnAclAsync(
        System.Data.Common.DbConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            WITH b4_tables AS (
              SELECT c.oid,c.relowner,c.relacl
              FROM pg_catalog.pg_class c
              JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
              WHERE n.nspname='tagekyc'
                AND c.relkind='r'
                AND c.relname IN (
                    'raw_export_job_identities',
                    'raw_export_job_classes',
                    'raw_export_job_attempts',
                    'raw_export_job_transitions',
                    'raw_export_job_operational_heads')
            )
            SELECT
              (SELECT count(*) FROM b4_tables)=5
              AND NOT EXISTS (
                SELECT 1
                FROM b4_tables b
                CROSS JOIN LATERAL pg_catalog.aclexplode(
                  COALESCE(b.relacl,pg_catalog.acldefault('r',b.relowner))) x
                WHERE x.grantee<>b.relowner)
              AND NOT EXISTS (
                SELECT 1
                FROM b4_tables b
                JOIN pg_catalog.pg_attribute a
                  ON a.attrelid=b.oid AND a.attnum>0 AND NOT a.attisdropped
                CROSS JOIN LATERAL pg_catalog.aclexplode(a.attacl) x
                WHERE a.attacl IS NOT NULL AND x.grantee<>b.relowner);
            """;
        return await command.ExecuteScalarAsync(cancellationToken) is true;
    }

    private const string SchemaDigestSql =
        """
        WITH rows AS (
          SELECT 'T|'||c.relname::text||'|'||r.rolname AS value
          FROM pg_catalog.pg_class c
          JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
          JOIN pg_catalog.pg_roles r ON r.oid=c.relowner
          WHERE n.nspname='tagekyc' AND c.relname IN (
              'raw_export_job_identities','raw_export_job_classes','raw_export_job_attempts',
              'raw_export_job_transitions','raw_export_job_operational_heads') AND c.relkind='r'
          UNION ALL
          SELECT 'C|'||c.relname::text||'|'||a.attnum::text||'|'||a.attname::text||'|'||
                 pg_catalog.format_type(a.atttypid,a.atttypmod)||'|'||a.attnotnull::text
          FROM pg_catalog.pg_class c
          JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
          JOIN pg_catalog.pg_attribute a ON a.attrelid=c.oid AND a.attnum>0 AND NOT a.attisdropped
          WHERE n.nspname='tagekyc' AND c.relname IN (
              'raw_export_job_identities','raw_export_job_classes','raw_export_job_attempts',
              'raw_export_job_transitions','raw_export_job_operational_heads') AND c.relkind='r'
          UNION ALL
          SELECT 'K|'||c.relname::text||'|'||con.conname::text||'|'||con.contype::text||'|'||
                 pg_catalog.pg_get_constraintdef(con.oid,true)
          FROM pg_catalog.pg_constraint con
          JOIN pg_catalog.pg_class c ON c.oid=con.conrelid
          JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
          WHERE n.nspname='tagekyc' AND c.relname IN (
              'raw_export_job_identities','raw_export_job_classes','raw_export_job_attempts',
              'raw_export_job_transitions','raw_export_job_operational_heads')
          UNION ALL
          SELECT 'I|'||t.relname::text||'|'||i.relname::text||'|'||pg_catalog.pg_get_indexdef(ix.indexrelid)
          FROM pg_catalog.pg_index ix
          JOIN pg_catalog.pg_class t ON t.oid=ix.indrelid
          JOIN pg_catalog.pg_namespace n ON n.oid=t.relnamespace
          JOIN pg_catalog.pg_class i ON i.oid=ix.indexrelid
          WHERE n.nspname='tagekyc' AND t.relname IN (
              'raw_export_job_identities','raw_export_job_classes','raw_export_job_attempts',
              'raw_export_job_transitions','raw_export_job_operational_heads')
          UNION ALL
          SELECT 'G|'||c.relname::text||'|'||t.tgname::text||'|'||t.tgdeferrable::text||'|'||
                 t.tginitdeferred::text||'|'||pg_catalog.pg_get_triggerdef(t.oid,true)
          FROM pg_catalog.pg_trigger t
          JOIN pg_catalog.pg_class c ON c.oid=t.tgrelid
          JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
          WHERE n.nspname='tagekyc' AND c.relname IN (
              'raw_export_job_identities','raw_export_job_classes','raw_export_job_attempts',
              'raw_export_job_transitions','raw_export_job_operational_heads') AND NOT t.tgisinternal
        )
        SELECT count(*)::integer,md5(string_agg(value,E'\n' ORDER BY value)) FROM rows;
        """;

    private const string FunctionDigestSql =
        """
        SELECT count(*)::integer,
               md5(string_agg(
                 p.proname||'|'||
                 pg_catalog.pg_get_function_identity_arguments(p.oid)||'|'||
                 pg_catalog.pg_get_function_result(p.oid)||'|'||
                 l.lanname||'|'||p.prosecdef::text||'|'||r.rolname||'|'||
                 COALESCE(array_to_string(p.proconfig,','),'')||'|'||
                 md5(pg_catalog.pg_get_functiondef(p.oid)),
                 E'\n' ORDER BY p.proname,pg_catalog.pg_get_function_identity_arguments(p.oid)))
        FROM pg_catalog.pg_proc p
        JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
        JOIN pg_catalog.pg_language l ON l.oid=p.prolang
        JOIN pg_catalog.pg_roles r ON r.oid=p.proowner
        WHERE n.nspname='tagekyc'
          AND p.proname IN (
              'enforce_raw_export_job_attempt_insert',
              'enforce_raw_export_job_class_insert',
              'enforce_raw_export_job_head_mutation',
              'enforce_raw_export_job_identity_has_classes',
              'enforce_raw_export_job_identity_insert',
              'enforce_raw_export_job_transition_insert',
              'raw_export_acquire_or_reclaim_job_lease',
              'raw_export_claim_or_read_job',
              'raw_export_lock_job_for_attempt',
              'raw_export_read_job',
              'raw_export_read_job_binding_inputs',
              'raw_export_record_job_attempt_failure',
              'raw_export_renew_job_lease',
              'raw_export_terminalize_job');
        """;
}
