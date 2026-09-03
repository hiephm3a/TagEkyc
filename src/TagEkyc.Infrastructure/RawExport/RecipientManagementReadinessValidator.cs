using System.Data;
using Microsoft.Extensions.DependencyInjection;

namespace TagEkyc.Infrastructure.RawExport;

public sealed class RecipientManagementReadinessException(
    string code,
    string? component = null,
    string? componentCode = null) : Exception(code)
{
    public string Code { get; } = code;
    public string? Component { get; } = component;
    public string? ComponentCode { get; } = componentCode;
}

public sealed class RecipientManagementReadinessValidator(
    RecipientManagementOptions options,
    IServiceProvider services)
{
    public static readonly string[] Codes =
    [
        "PROD_RAW_EXPORT_RECIPIENT_MANAGEMENT_CONFIG_INVALID",
        "PROD_RAW_EXPORT_RECIPIENT_MANAGEMENT_ROLE_TOPOLOGY_INVALID",
        "PROD_RAW_EXPORT_RECIPIENT_MANAGEMENT_CATALOG_INVALID",
        "PROD_RAW_EXPORT_RECIPIENT_MANAGEMENT_MANAGED_AUTH_INVALID",
        "PROD_RAW_EXPORT_RECIPIENT_MANAGEMENT_KEY_LIFECYCLE_INVALID",
        "PROD_RAW_EXPORT_RECIPIENT_MANAGEMENT_PREDECESSOR_INVALID",
    ];

    public async Task ValidateAsync(CancellationToken cancellationToken)
    {
        if (options.Topology == RecipientManagementTopology.Disabled && options.IsSyntacticallyValid) return;
        if (options.Topology != RecipientManagementTopology.PostgresDurable || !options.IsSyntacticallyValid)
            throw new RecipientManagementReadinessException(Codes[0]);

        var factory = services.GetService<IRecipientManagementConnectionFactory>()
            ?? throw new RecipientManagementReadinessException(Codes[1]);
        await using var connection = await factory.OpenAsync(cancellationToken).ConfigureAwait(false);
        if (connection.State != ConnectionState.Open)
            throw new RecipientManagementReadinessException(Codes[1]);
        await using (var role = connection.CreateCommand())
        {
            role.CommandText = "SELECT current_user = 'tagekyc_raw_export_recipient_manager_login'";
            if (await role.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) is not true)
                throw new RecipientManagementReadinessException(Codes[1]);
        }
        await using (var catalog = connection.CreateCommand())
        {
            catalog.CommandText = CatalogSql;
            if (await catalog.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) is not true)
                throw new RecipientManagementReadinessException(Codes[2]);
        }

        var c2 = services.GetRequiredService<RecipientPackageReadinessValidator>();
        var c3 = services.GetRequiredService<RecipientPackageDeliveryReadinessValidator>();
        var c4 = services.GetRequiredService<RecipientPackageReferenceReadinessValidator>();
        try { await c2.ValidateAsync(cancellationToken).ConfigureAwait(false); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (RecipientPackageReadinessException exception)
        { throw Predecessor("C2", exception.Code); }
        try { await c3.ValidateAsync(cancellationToken).ConfigureAwait(false); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (RecipientPackageDeliveryReadinessException exception)
        { throw Predecessor("C3", exception.Code); }
        try { await c4.ValidateAsync(cancellationToken).ConfigureAwait(false); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (RecipientPackageReferenceReadinessException exception)
        { throw Predecessor("C4", exception.Code); }
    }

    private static RecipientManagementReadinessException Predecessor(
        string component, string componentCode) => new(Codes[5], component, componentCode);

    internal const string CatalogSql = """
        WITH expected_tables(name) AS (VALUES
          ('raw_export_managed_recipient_identities'),
          ('raw_export_managed_recipient_policies'),
          ('raw_export_managed_recipient_credentials'),
          ('raw_export_recipient_management_operations'),
          ('raw_export_recipient_management_events')),
        expected_functions(name,args) AS (VALUES
          ('raw_export_enroll_managed_recipient','uuid, uuid, uuid, uuid, bytea, bytea, bytea, uuid'),
          ('raw_export_issue_recipient_credential','uuid, uuid, uuid, uuid, bytea, bytea, bytea, uuid, text, bytea, timestamp with time zone'),
          ('raw_export_replace_recipient_credential','uuid, uuid, uuid, uuid, bytea, bytea, bytea, uuid, bigint, uuid, text, bytea, timestamp with time zone, text'),
          ('raw_export_revoke_recipient_credential','uuid, uuid, uuid, uuid, bytea, bytea, bytea, uuid, bigint, text'),
          ('raw_export_enroll_recipient_key','uuid, uuid, uuid, uuid, bytea, bytea, bytea, text, integer, text, bytea, bytea, timestamp with time zone, timestamp with time zone'),
          ('raw_export_rotate_recipient_key','uuid, uuid, uuid, uuid, bytea, bytea, bytea, text, integer, bigint, integer, text, bytea, bytea, timestamp with time zone, timestamp with time zone, text'),
          ('raw_export_revoke_recipient_key','uuid, uuid, uuid, uuid, bytea, bytea, bytea, text, integer, bigint, text'),
          ('raw_export_read_recipient_activation_readiness','uuid'),
          ('raw_export_guard_recipient_management_event','')),
        expected_indexes(name,owner_name) AS (VALUES
          ('uq_raw_export_managed_recipient_identity_pair','tagekyc_raw_export_deployer'),
          ('uq_api_keys_managed_identity','tagekyc'),
          ('uq_raw_export_managed_credential_version','tagekyc_raw_export_deployer'),
          ('uq_raw_export_managed_credential_active','tagekyc_raw_export_deployer'),
          ('uq_raw_export_recipient_management_idempotency','tagekyc_raw_export_deployer'),
          ('uq_raw_export_recipient_management_event_operation','tagekyc_raw_export_deployer')),
        expected_roles(name,canlogin) AS (VALUES
          ('tagekyc_raw_export_recipient_manager',false),
          ('tagekyc_raw_export_recipient_manager_login',true)),
        table_ok AS (
          SELECT count(*)=5 AND bool_and(pg_get_userbyid(c.relowner)='tagekyc_raw_export_deployer') ok
          FROM expected_tables e JOIN pg_catalog.pg_class c ON c.relname=e.name AND c.relkind='r'
          JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace AND n.nspname='tagekyc'),
        function_ok AS (
          SELECT count(*)=9 AND bool_and(pg_get_userbyid(p.proowner)='tagekyc_raw_export_deployer'
            AND p.prosecdef AND p.proconfig=ARRAY['search_path=pg_catalog']::text[]) ok
          FROM expected_functions e JOIN pg_catalog.pg_proc p ON p.proname=e.name
            AND pg_catalog.oidvectortypes(p.proargtypes)=e.args
          JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace AND n.nspname='tagekyc'),
        index_ok AS (
          SELECT count(*)=6 AND bool_and(pg_get_userbyid(c.relowner)=e.owner_name) ok
          FROM expected_indexes e JOIN pg_catalog.pg_class c ON c.relname=e.name AND c.relkind IN ('i','I')
          JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace AND n.nspname='tagekyc'),
        role_ok AS (
          SELECT count(*)=2 AND bool_and(r.rolcanlogin=e.canlogin AND r.rolinherit
            AND NOT r.rolsuper AND NOT r.rolcreatedb AND NOT r.rolcreaterole
            AND NOT r.rolreplication AND NOT r.rolbypassrls) ok
          FROM expected_roles e JOIN pg_catalog.pg_roles r ON r.rolname=e.name),
        membership_ok AS (
          SELECT count(*)=1 AND bool_and(member.rolname='tagekyc_raw_export_recipient_manager_login'
            AND role.rolname='tagekyc_raw_export_recipient_manager'
            AND NOT m.admin_option AND m.inherit_option AND NOT m.set_option) ok
          FROM pg_catalog.pg_auth_members m
          JOIN pg_catalog.pg_roles member ON member.oid=m.member
          JOIN pg_catalog.pg_roles role ON role.oid=m.roleid
          WHERE member.rolname IN ('tagekyc_raw_export_recipient_manager','tagekyc_raw_export_recipient_manager_login')
             OR role.rolname IN ('tagekyc_raw_export_recipient_manager','tagekyc_raw_export_recipient_manager_login')),
        acl_ok AS (
          SELECT count(*)=8 AND bool_and(
            (SELECT count(*)=1 AND bool_and(pg_get_userbyid(a.grantee)='tagekyc_raw_export_recipient_manager'
               AND a.privilege_type='EXECUTE' AND NOT a.is_grantable AND a.grantor=p.proowner)
             FROM pg_catalog.aclexplode(COALESCE(p.proacl,pg_catalog.acldefault('f',p.proowner))) a
             WHERE a.grantee<>p.proowner)
            AND NOT EXISTS (
              SELECT 1 FROM pg_catalog.aclexplode(COALESCE(p.proacl,pg_catalog.acldefault('f',p.proowner))) a
              WHERE a.grantee<>p.proowner AND (a.grantee=0 OR a.is_grantable OR a.grantor<>p.proowner
                OR pg_get_userbyid(a.grantee)<>'tagekyc_raw_export_recipient_manager'))) ok
          FROM expected_functions e JOIN pg_catalog.pg_proc p ON p.proname=e.name
            AND pg_catalog.oidvectortypes(p.proargtypes)=e.args
          JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace AND n.nspname='tagekyc'
          WHERE e.name<>'raw_export_guard_recipient_management_event')
        SELECT t.ok AND f.ok AND i.ok AND r.ok AND m.ok AND a.ok
          AND pg_catalog.has_schema_privilege('tagekyc_raw_export_recipient_manager','tagekyc','USAGE')
          AND NOT pg_catalog.has_function_privilege('tagekyc_runtime',
            'tagekyc.raw_export_read_recipient_activation_readiness(uuid)','EXECUTE')
        FROM table_ok t CROSS JOIN function_ok f CROSS JOIN index_ok i
          CROSS JOIN role_ok r CROSS JOIN membership_ok m CROSS JOIN acl_ok a
        """;
}
