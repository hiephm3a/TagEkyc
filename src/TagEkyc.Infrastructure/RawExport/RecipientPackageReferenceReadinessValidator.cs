using System.Data;
using Microsoft.Extensions.DependencyInjection;

namespace TagEkyc.Infrastructure.RawExport;

public sealed class RecipientPackageReferenceReadinessException(string code) : Exception(code)
{
    public string Code { get; } = code;
}

public sealed class RecipientPackageReferenceReadinessValidator(
    RecipientPackageReferenceOptions options,
    IServiceProvider services)
{
    public static readonly string[] Codes =
    [
        "PROD_RAW_EXPORT_PACKAGE_REFERENCE_CONFIG_INVALID",
        "PROD_RAW_EXPORT_PACKAGE_REFERENCE_ROLE_TOPOLOGY_INVALID",
        "PROD_RAW_EXPORT_PACKAGE_REFERENCE_CATALOG_INVALID",
        "PROD_RAW_EXPORT_PACKAGE_REFERENCE_CURSOR_KEY_UNAVAILABLE",
    ];

    public async Task ValidateAsync(CancellationToken cancellationToken)
    {
        if (options.Topology == RecipientPackageReferenceTopology.Disabled && options.IsSyntacticallyValid) return;
        if (options.Topology != RecipientPackageReferenceTopology.PostgresDurable || !options.IsSyntacticallyValid)
            throw new RecipientPackageReferenceReadinessException(Codes[0]);
        var factory = services.GetService<IRecipientPackageReferenceConnectionFactory>()
            ?? throw new RecipientPackageReferenceReadinessException(Codes[1]);
        await using var connection = await factory.OpenAsync(cancellationToken).ConfigureAwait(false);
        if (connection.State != ConnectionState.Open) throw new RecipientPackageReferenceReadinessException(Codes[2]);
        await using (var roleCommand = connection.CreateCommand())
        {
            roleCommand.CommandText = RoleSql;
            var roleValue = await roleCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            if (roleValue is not true) throw new RecipientPackageReferenceReadinessException(Codes[1]);
        }
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = CatalogSql;
            var value = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            if (value is not true) throw new RecipientPackageReferenceReadinessException(Codes[2]);
        }

        var keyService = services.GetService<RecipientPackageReferenceCursorKeyService>()
            ?? throw new RecipientPackageReferenceReadinessException(Codes[1]);
        foreach (var identity in options.AcceptedKeys)
        {
            using var lease = await keyService.ResolveAsync(identity, cancellationToken).ConfigureAwait(false);
            if (lease is null || lease.Material.Length != 32)
                throw new RecipientPackageReferenceReadinessException(Codes[3]);
        }
    }

    private const string RoleSql = """
        WITH expected_roles(name,login) AS (VALUES
          ('tagekyc_raw_export_package_reference',false),
          ('tagekyc_raw_export_package_reference_login',true)),
        role_ok AS (
          SELECT pg_catalog.count(*)=2 AND pg_catalog.bool_and(
            r.rolcanlogin=e.login AND r.rolinherit AND NOT r.rolsuper AND NOT r.rolcreatedb
            AND NOT r.rolcreaterole AND NOT r.rolreplication AND NOT r.rolbypassrls) ok
          FROM expected_roles e JOIN pg_catalog.pg_roles r ON r.rolname=e.name),
        member_ok AS (
          SELECT pg_catalog.count(*)=1 AND pg_catalog.bool_and(
            member.rolname='tagekyc_raw_export_package_reference_login'
            AND role.rolname='tagekyc_raw_export_package_reference'
            AND NOT m.admin_option AND m.inherit_option AND NOT m.set_option) ok
          FROM pg_catalog.pg_auth_members m
          JOIN pg_catalog.pg_roles member ON member.oid=m.member
          JOIN pg_catalog.pg_roles role ON role.oid=m.roleid
          WHERE member.rolname IN ('tagekyc_raw_export_package_reference','tagekyc_raw_export_package_reference_login')
             OR role.rolname IN ('tagekyc_raw_export_package_reference','tagekyc_raw_export_package_reference_login'))
        SELECT r.ok AND m.ok FROM role_ok r CROSS JOIN member_ok m
        """;

    private const string CatalogSql = """
        WITH expected_roles(name,login) AS (VALUES
          ('tagekyc_raw_export_package_reference',false),
          ('tagekyc_raw_export_package_reference_login',true)),
        role_ok AS (
          SELECT pg_catalog.count(*)=2 AND pg_catalog.bool_and(
            r.rolcanlogin=e.login AND r.rolinherit AND NOT r.rolsuper AND NOT r.rolcreatedb
            AND NOT r.rolcreaterole AND NOT r.rolreplication AND NOT r.rolbypassrls) ok
          FROM expected_roles e JOIN pg_catalog.pg_roles r ON r.rolname=e.name),
        member_ok AS (
          SELECT pg_catalog.count(*)=1 AND pg_catalog.bool_and(
            member.rolname='tagekyc_raw_export_package_reference_login'
            AND role.rolname='tagekyc_raw_export_package_reference'
            AND NOT m.admin_option AND m.inherit_option AND NOT m.set_option) ok
          FROM pg_catalog.pg_auth_members m
          JOIN pg_catalog.pg_roles member ON member.oid=m.member
          JOIN pg_catalog.pg_roles role ON role.oid=m.roleid
          WHERE member.rolname IN ('tagekyc_raw_export_package_reference','tagekyc_raw_export_package_reference_login')
             OR role.rolname IN ('tagekyc_raw_export_package_reference','tagekyc_raw_export_package_reference_login')),
        function_ok AS (
          SELECT pg_catalog.count(*)=1 AND pg_catalog.bool_and(
            pg_catalog.pg_get_userbyid(p.proowner)='tagekyc_raw_export_deployer'
            AND p.prosecdef AND p.proconfig=ARRAY['search_path=pg_catalog']::text[]
            AND pg_catalog.pg_get_function_result(p.oid)='TABLE("PackageId" uuid, "FinalizedAtUtc" timestamp with time zone)'
            AND (SELECT pg_catalog.count(*)=1 AND pg_catalog.bool_and(
                   pg_catalog.pg_get_userbyid(a.grantee)='tagekyc_raw_export_package_reference'
                   AND a.privilege_type='EXECUTE' AND NOT a.is_grantable AND a.grantor=p.proowner)
                 FROM pg_catalog.aclexplode(COALESCE(p.proacl,pg_catalog.acldefault('f',p.proowner))) a
                 WHERE a.grantee<>p.proowner)
            AND NOT EXISTS (
              SELECT 1 FROM pg_catalog.aclexplode(COALESCE(p.proacl,pg_catalog.acldefault('f',p.proowner))) a
              WHERE a.grantee<>p.proowner AND (a.grantee=0 OR a.is_grantable OR a.grantor<>p.proowner
                 OR pg_catalog.pg_get_userbyid(a.grantee)<>'tagekyc_raw_export_package_reference'))) ok
          FROM pg_catalog.pg_namespace n JOIN pg_catalog.pg_proc p ON p.pronamespace=n.oid
          WHERE n.nspname='tagekyc'
            AND p.proname IN ('raw_export_list_recipient_package_references')
            AND pg_catalog.oidvectortypes(p.proargtypes)='uuid, timestamp with time zone, uuid, integer'),
        surface_ok AS (
          SELECT pg_catalog.count(*)=1 ok FROM pg_catalog.pg_namespace n
          JOIN pg_catalog.pg_proc p ON p.pronamespace=n.oid
          WHERE n.nspname='tagekyc' AND p.proname IN ('raw_export_list_recipient_package_references')),
        index_ok AS (
          SELECT pg_catalog.count(*)=1 AND pg_catalog.bool_and(
            pg_catalog.pg_get_userbyid(c.relowner)='tagekyc_raw_export_deployer'
            AND pg_catalog.pg_get_indexdef(c.oid) =
              'CREATE INDEX ix_raw_export_recipient_package_reference_list ON tagekyc.raw_export_recipient_package_preparations USING btree ("RecipientClientApplicationId", "FinalizedAtUtc" DESC, "PackageId" DESC) WHERE ((("State")::text = ''Finalized''::text) AND ("FinalizedAtUtc" IS NOT NULL))') ok
          FROM pg_catalog.pg_namespace n JOIN pg_catalog.pg_class c ON c.relnamespace=n.oid
          WHERE n.nspname='tagekyc' AND c.relname IN ('ix_raw_export_recipient_package_reference_list') AND c.relkind='i'),
        table_privilege_ok AS (
          SELECT NOT pg_catalog.has_table_privilege('tagekyc_raw_export_package_reference','tagekyc.raw_export_recipient_package_preparations','SELECT,INSERT,UPDATE,DELETE') ok),
        identifier_ok AS (
          SELECT pg_catalog.bool_and(pg_catalog.octet_length(v.name)<=63) ok FROM (VALUES
            ('tagekyc_raw_export_package_reference'),('tagekyc_raw_export_package_reference_login'),
            ('raw_export_list_recipient_package_references'),('ix_raw_export_recipient_package_reference_list')) v(name))
        SELECT r.ok AND m.ok AND f.ok AND s.ok AND i.ok AND t.ok AND d.ok
          AND pg_catalog.has_schema_privilege('tagekyc_raw_export_package_reference','tagekyc','USAGE')
        FROM role_ok r CROSS JOIN member_ok m CROSS JOIN function_ok f CROSS JOIN surface_ok s
          CROSS JOIN index_ok i CROSS JOIN table_privilege_ok t CROSS JOIN identifier_ok d
        """;
}
