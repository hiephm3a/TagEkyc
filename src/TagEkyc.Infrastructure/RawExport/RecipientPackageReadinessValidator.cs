using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.Infrastructure.RawExport;

public sealed class RecipientPackageReadinessException(string code) : Exception(code)
{
    public string Code { get; } = code;
}

public sealed class RecipientPackageReadinessValidator(
    RecipientPackageOptions options,
    TagEkycDbContext db,
    IServiceProvider services)
{
    public static readonly string[] Codes =
    [
        "PROD_RAW_EXPORT_RECIPIENT_PACKAGE_CONFIG_INVALID",
        "PROD_RAW_EXPORT_RECIPIENT_PACKAGE_ROLE_TOPOLOGY_INVALID",
        "PROD_RAW_EXPORT_RECIPIENT_PACKAGE_CATALOG_INVALID",
        "PROD_RAW_EXPORT_RECIPIENT_PACKAGE_PROVIDER_UNAVAILABLE",
        "PROD_RAW_EXPORT_RECIPIENT_PACKAGE_BUCKET_UNAVAILABLE",
        "PROD_RAW_EXPORT_RECIPIENT_PACKAGE_OBJECT_LOCK_PROHIBITED",
        "PROD_RAW_EXPORT_RECIPIENT_PACKAGE_VERSIONING_PROHIBITED",
        "PROD_RAW_EXPORT_RECIPIENT_PACKAGE_LIFECYCLE_PROHIBITED",
        "PROD_RAW_EXPORT_RECIPIENT_PACKAGE_PUBLIC_ACCESS_PROHIBITED",
    ];

    public async Task ValidateAsync(CancellationToken cancellationToken)
    {
        if (!options.IsSyntacticallyValid || options.Topology == RecipientPackageTopology.Invalid)
            throw new RecipientPackageReadinessException(Codes[0]);
        if (options.Topology == RecipientPackageTopology.Disabled) return;
        if (options.Provider is null || services.GetService<IRecipientPackageConnectionFactory>() is null)
            throw new RecipientPackageReadinessException(Codes[1]);

        var connection = db.Database.GetDbConnection() as NpgsqlConnection
            ?? throw new RecipientPackageReadinessException(Codes[2]);
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = CatalogSql;
        var catalog = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        if (catalog is not true) throw new RecipientPackageReadinessException(Codes[2]);

        var probe = services.GetService<IRecipientPackagePostureProbe>()
            ?? throw new RecipientPackageReadinessException(Codes[3]);
        RecipientPackageBucketPosture posture;
        try { posture = await probe.InspectAsync(cancellationToken).ConfigureAwait(false); }
        catch { throw new RecipientPackageReadinessException(Codes[3]); }
        if (!posture.BucketAvailable) throw new RecipientPackageReadinessException(Codes[4]);
        if (!posture.ObjectLockAbsent) throw new RecipientPackageReadinessException(Codes[5]);
        if (!posture.VersioningNeverEnabled) throw new RecipientPackageReadinessException(Codes[6]);
        if (!posture.LifecycleAbsent) throw new RecipientPackageReadinessException(Codes[7]);
        if (!posture.PublicAccessAbsent) throw new RecipientPackageReadinessException(Codes[8]);
    }

    private const string CatalogSql = """
        WITH expected_roles(name,login) AS (VALUES
          ('tagekyc_raw_export_package_preparer',false),
          ('tagekyc_raw_export_package_reconciler',false),
          ('tagekyc_raw_export_package_lifecycle',false),
          ('tagekyc_raw_export_package_preparer_login',true),
          ('tagekyc_raw_export_package_reconciler_login',true),
          ('tagekyc_raw_export_package_lifecycle_login',true)),
        expected_members(member_name,role_name) AS (VALUES
          ('tagekyc_raw_export_package_preparer_login','tagekyc_raw_export_package_preparer'),
          ('tagekyc_raw_export_package_reconciler_login','tagekyc_raw_export_package_reconciler'),
          ('tagekyc_raw_export_package_lifecycle_login','tagekyc_raw_export_package_lifecycle')),
        expected_tables(name) AS (VALUES
          ('raw_export_recipient_key_registrations'),
          ('raw_export_recipient_package_preparations'),
          ('raw_export_recipient_package_events')),
        expected_functions(name,args) AS (VALUES
          ('raw_export_select_active_recipient_key','uuid'),
          ('raw_export_reserve_recipient_package','uuid, uuid, uuid, uuid, uuid, bigint, bytea, bytea, bytea, bytea, bigint, uuid, text, integer, bytea, bigint, bytea, bytea, text, text, bytea, text, text, bytea, text'),
          ('raw_export_begin_recipient_package_put','uuid, bigint, bytea, bigint, bytea'),
          ('raw_export_record_recipient_package_put_unknown','uuid, bigint, bytea'),
          ('raw_export_record_recipient_package_prepared','uuid, bigint, bytea, bigint, bytea, bytea, bytea'),
          ('raw_export_read_recipient_package_recovery_context','uuid'),
          ('raw_export_finalize_recipient_package','uuid, bigint, bytea'),
          ('raw_export_authorize_recipient_package_abort','uuid, bigint, bytea'),
          ('raw_export_record_recipient_package_abort_result','uuid, bigint, text, bytea'),
          ('raw_export_record_recipient_package_quarantined','uuid, bigint, bytea, text')),
        expected_acl(name,args,grantee) AS (VALUES
          ('raw_export_select_active_recipient_key','uuid','tagekyc_raw_export_package_preparer'),
          ('raw_export_reserve_recipient_package','uuid, uuid, uuid, uuid, uuid, bigint, bytea, bytea, bytea, bytea, bigint, uuid, text, integer, bytea, bigint, bytea, bytea, text, text, bytea, text, text, bytea, text','tagekyc_raw_export_package_preparer'),
          ('raw_export_begin_recipient_package_put','uuid, bigint, bytea, bigint, bytea','tagekyc_raw_export_package_preparer'),
          ('raw_export_record_recipient_package_put_unknown','uuid, bigint, bytea','tagekyc_raw_export_package_preparer'),
          ('raw_export_record_recipient_package_prepared','uuid, bigint, bytea, bigint, bytea, bytea, bytea','tagekyc_raw_export_package_preparer'),
          ('raw_export_record_recipient_package_prepared','uuid, bigint, bytea, bigint, bytea, bytea, bytea','tagekyc_raw_export_package_reconciler'),
          ('raw_export_read_recipient_package_recovery_context','uuid','tagekyc_raw_export_package_preparer'),
          ('raw_export_read_recipient_package_recovery_context','uuid','tagekyc_raw_export_package_reconciler'),
          ('raw_export_read_recipient_package_recovery_context','uuid','tagekyc_raw_export_package_lifecycle'),
          ('raw_export_finalize_recipient_package','uuid, bigint, bytea','tagekyc_raw_export_package_preparer'),
          ('raw_export_authorize_recipient_package_abort','uuid, bigint, bytea','tagekyc_raw_export_package_lifecycle'),
          ('raw_export_record_recipient_package_abort_result','uuid, bigint, text, bytea','tagekyc_raw_export_package_lifecycle'),
          ('raw_export_record_recipient_package_quarantined','uuid, bigint, bytea, text','tagekyc_raw_export_package_reconciler'),
          ('raw_export_record_recipient_package_quarantined','uuid, bigint, bytea, text','tagekyc_raw_export_package_lifecycle')),
        role_ok AS (
          SELECT pg_catalog.count(*)=6 AND pg_catalog.bool_and(
            r.rolcanlogin=e.login AND r.rolinherit AND NOT r.rolsuper AND NOT r.rolcreatedb
            AND NOT r.rolcreaterole AND NOT r.rolreplication AND NOT r.rolbypassrls) ok
          FROM expected_roles e JOIN pg_catalog.pg_roles r ON r.rolname=e.name),
        member_ok AS (
          SELECT pg_catalog.count(*)=3 AND pg_catalog.bool_and(
            e.member_name IS NOT NULL AND NOT m.admin_option AND m.inherit_option AND NOT m.set_option) ok
          FROM pg_catalog.pg_auth_members m
          JOIN pg_catalog.pg_roles member ON member.oid=m.member
          JOIN pg_catalog.pg_roles role ON role.oid=m.roleid
          LEFT JOIN expected_members e ON e.member_name=member.rolname AND e.role_name=role.rolname
          WHERE member.rolname IN (SELECT name FROM expected_roles)
             OR role.rolname IN (SELECT name FROM expected_roles)),
        table_ok AS (
          SELECT pg_catalog.count(*)=3 AND pg_catalog.bool_and(
            pg_catalog.pg_get_userbyid(c.relowner)='tagekyc_raw_export_deployer'
            AND NOT EXISTS (SELECT 1 FROM pg_catalog.aclexplode(COALESCE(c.relacl,pg_catalog.acldefault('r',c.relowner))) a
                            WHERE a.grantee<>c.relowner)) ok
          FROM expected_tables e JOIN pg_catalog.pg_namespace n ON n.nspname='tagekyc'
          JOIN pg_catalog.pg_class c ON c.relnamespace=n.oid AND c.relname=e.name AND c.relkind='r'),
        function_ok AS (
          SELECT pg_catalog.count(*)=10 AND pg_catalog.bool_and(
            pg_catalog.pg_get_userbyid(p.proowner)='tagekyc_raw_export_deployer'
            AND p.prosecdef AND p.proconfig=ARRAY['search_path=pg_catalog']::text[]
            AND NOT EXISTS (SELECT 1 FROM pg_catalog.aclexplode(COALESCE(p.proacl,pg_catalog.acldefault('f',p.proowner))) a
                            WHERE a.grantee=0 OR a.is_grantable OR a.grantor<>p.proowner)) ok
          FROM expected_functions e JOIN pg_catalog.pg_namespace n ON n.nspname='tagekyc'
          JOIN pg_catalog.pg_proc p ON p.pronamespace=n.oid AND p.proname=e.name
            AND pg_catalog.oidvectortypes(p.proargtypes)=e.args),
        actual_acl AS (
          SELECT p.proname name,pg_catalog.oidvectortypes(p.proargtypes) args,
                 pg_catalog.pg_get_userbyid(a.grantee) grantee
          FROM expected_functions e JOIN pg_catalog.pg_namespace n ON n.nspname='tagekyc'
          JOIN pg_catalog.pg_proc p ON p.pronamespace=n.oid AND p.proname=e.name
            AND pg_catalog.oidvectortypes(p.proargtypes)=e.args
          CROSS JOIN LATERAL pg_catalog.aclexplode(COALESCE(p.proacl,pg_catalog.acldefault('f',p.proowner))) a
          WHERE a.grantee<>p.proowner),
        acl_ok AS (
          SELECT pg_catalog.count(*)=14
            AND NOT EXISTS (SELECT name,args,grantee FROM expected_acl
                            EXCEPT SELECT name,args,grantee FROM actual_acl)
            AND NOT EXISTS (SELECT name,args,grantee FROM actual_acl
                            EXCEPT SELECT name,args,grantee FROM expected_acl) ok
          FROM actual_acl),
        surface_ok AS (
          SELECT pg_catalog.count(*)=10 ok FROM pg_catalog.pg_proc p
          JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
          WHERE n.nspname='tagekyc' AND p.proname IN (SELECT name FROM expected_functions))
        SELECT r.ok AND m.ok AND t.ok AND f.ok AND a.ok AND s.ok
          AND pg_catalog.has_schema_privilege('tagekyc_raw_export_package_preparer','tagekyc','USAGE')
          AND pg_catalog.has_schema_privilege('tagekyc_raw_export_package_reconciler','tagekyc','USAGE')
          AND pg_catalog.has_schema_privilege('tagekyc_raw_export_package_lifecycle','tagekyc','USAGE')
        FROM role_ok r CROSS JOIN member_ok m CROSS JOIN table_ok t CROSS JOIN function_ok f
          CROSS JOIN acl_ok a CROSS JOIN surface_ok s
        """;
}
