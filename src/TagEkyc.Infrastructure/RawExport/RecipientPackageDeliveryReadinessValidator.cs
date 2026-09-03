using System.Data;

namespace TagEkyc.Infrastructure.RawExport;

public sealed class RecipientPackageDeliveryReadinessException(string code) : Exception(code)
{
    public string Code { get; } = code;
}

public sealed class RecipientPackageDeliveryReadinessValidator(
    RecipientPackageDeliveryOptions options,
    IServiceProvider services)
{
    public static readonly string[] Codes =
    [
        "PROD_RAW_EXPORT_PACKAGE_DELIVERY_CONFIG_INVALID",
        "PROD_RAW_EXPORT_PACKAGE_DELIVERY_ROLE_TOPOLOGY_INVALID",
        "PROD_RAW_EXPORT_PACKAGE_DELIVERY_CATALOG_INVALID",
        "PROD_RAW_EXPORT_PACKAGE_DELIVERY_PROVIDER_UNAVAILABLE",
        "PROD_RAW_EXPORT_PACKAGE_DELIVERY_BUCKET_UNAVAILABLE",
        "PROD_RAW_EXPORT_PACKAGE_DELIVERY_OBJECT_LOCK_PROHIBITED",
        "PROD_RAW_EXPORT_PACKAGE_DELIVERY_VERSIONING_PROHIBITED",
        "PROD_RAW_EXPORT_PACKAGE_DELIVERY_LIFECYCLE_PROHIBITED",
        "PROD_RAW_EXPORT_PACKAGE_DELIVERY_PUBLIC_ACCESS_PROHIBITED",
        "PROD_RAW_EXPORT_PACKAGE_DELIVERY_IAM_INVALID",
    ];

    public async Task ValidateAsync(CancellationToken cancellationToken)
    {
        if (options.Topology == RecipientPackageDeliveryTopology.Disabled && options.IsSyntacticallyValid) return;
        if (options.Topology != RecipientPackageDeliveryTopology.S3CompatibleDurable || !options.IsSyntacticallyValid)
            throw new RecipientPackageDeliveryReadinessException(Codes[0]);
        var factory = services.GetService(typeof(IRecipientPackageDeliveryConnectionFactory)) as IRecipientPackageDeliveryConnectionFactory
            ?? throw new RecipientPackageDeliveryReadinessException(Codes[1]);
        await using var connection = await factory.OpenAsync(cancellationToken).ConfigureAwait(false);
        if (connection.State != ConnectionState.Open)
            throw new RecipientPackageDeliveryReadinessException(Codes[2]);
        await using var command = connection.CreateCommand();
        command.CommandText = CatalogSql;
        var value = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        if (value is not true)
            throw new RecipientPackageDeliveryReadinessException(Codes[2]);

        var predecessor = services.GetService(typeof(RecipientPackageReadinessValidator)) as RecipientPackageReadinessValidator
            ?? throw new RecipientPackageDeliveryReadinessException(Codes[3]);
        try { await predecessor.ValidateAsync(cancellationToken).ConfigureAwait(false); }
        catch (RecipientPackageReadinessException exception)
        {
            var index = Array.IndexOf(RecipientPackageReadinessValidator.Codes, exception.Code);
            throw new RecipientPackageDeliveryReadinessException(index switch
            {
                0 => Codes[0], 1 => Codes[1], 2 => Codes[2], 3 => Codes[3], 4 => Codes[4],
                5 => Codes[5], 6 => Codes[6], 7 => Codes[7], 8 => Codes[8], _ => Codes[3],
            });
        }

        var reader = services.GetService(typeof(IRecipientPackageDeliveryReader)) as IRecipientPackageDeliveryReader
            ?? throw new RecipientPackageDeliveryReadinessException(Codes[3]);
        var provider = options.Provider!;
        var endpoint = RecipientPackageCodec.ProviderEndpointFingerprint(provider);
        var objectKey = RecipientPackageCodec.ObjectKey(Guid.NewGuid());
        var binding = RecipientPackageCodec.ObjectBindingDigest(
            provider.ProviderConfigurationId, endpoint, provider.BucketName, objectKey);
        try
        {
            var probe = await reader.OpenExactAsync(new(provider.ProviderConfigurationId, provider.ServiceUrl,
                provider.RegionIdentifier, provider.ForcePathStyle, provider.BucketName, objectKey, binding),
                cancellationToken).ConfigureAwait(false);
            if (probe.Content is not null) await probe.Content.DisposeAsync().ConfigureAwait(false);
            if (probe.Outcome == RecipientPackageDeliveryReadOutcome.BucketUnavailable)
                throw new RecipientPackageDeliveryReadinessException(Codes[4]);
            if (probe.Outcome == RecipientPackageDeliveryReadOutcome.Forbidden)
                throw new RecipientPackageDeliveryReadinessException(Codes[9]);
            if (probe.Outcome != RecipientPackageDeliveryReadOutcome.PositivelyAbsent)
                throw new RecipientPackageDeliveryReadinessException(Codes[3]);
        }
        finally
        {
            System.Security.Cryptography.CryptographicOperations.ZeroMemory(endpoint);
            System.Security.Cryptography.CryptographicOperations.ZeroMemory(binding);
        }
    }

    private const string CatalogSql = """
        WITH expected_roles(name,login) AS (VALUES
          ('tagekyc_raw_export_package_delivery',false),
          ('tagekyc_raw_export_package_delivery_login',true)),
        expected_members(member_name,role_name) AS (VALUES
          ('tagekyc_raw_export_package_delivery_login','tagekyc_raw_export_package_delivery')),
        expected_tables(name) AS (VALUES
          ('raw_export_recipient_package_deliveries'),
          ('raw_export_recipient_package_delivery_events')),
        expected_functions(name,args,runtime) AS (VALUES
          ('raw_export_create_recipient_package_delivery','uuid, uuid, uuid, bytea, uuid, uuid, bytea',true),
          ('raw_export_read_recipient_package_delivery','uuid, uuid',true),
          ('raw_export_probe_recipient_package_delivery_content','uuid, uuid',true),
          ('raw_export_begin_recipient_package_delivery_stream','uuid, uuid, uuid, uuid, bytea',true),
          ('raw_export_record_recipient_package_delivery_interrupted','uuid, bigint, bigint, text, bytea',true),
          ('raw_export_record_recipient_package_integrity_unavailable','uuid, bigint, bigint, text, bytea',true),
          ('raw_export_complete_recipient_package_delivery','uuid, bigint, bigint, bigint, bytea',true),
          ('raw_export_reconcile_next_recipient_package_delivery','',true),
          ('raw_export_guard_recipient_package_delivery_event','',false)),
        role_ok AS (
          SELECT pg_catalog.count(*)=2 AND pg_catalog.bool_and(
            r.rolcanlogin=e.login AND r.rolinherit AND NOT r.rolsuper AND NOT r.rolcreatedb
            AND NOT r.rolcreaterole AND NOT r.rolreplication AND NOT r.rolbypassrls) ok
          FROM expected_roles e JOIN pg_catalog.pg_roles r ON r.rolname=e.name),
        member_ok AS (
          SELECT pg_catalog.count(*)=1 AND pg_catalog.bool_and(
            e.member_name IS NOT NULL AND NOT m.admin_option AND m.inherit_option AND NOT m.set_option) ok
          FROM pg_catalog.pg_auth_members m
          JOIN pg_catalog.pg_roles member ON member.oid=m.member
          JOIN pg_catalog.pg_roles role ON role.oid=m.roleid
          LEFT JOIN expected_members e ON e.member_name=member.rolname AND e.role_name=role.rolname
          WHERE member.rolname IN (SELECT name FROM expected_roles)
             OR role.rolname IN (SELECT name FROM expected_roles)),
        table_ok AS (
          SELECT pg_catalog.count(*)=2 AND pg_catalog.bool_and(
            pg_catalog.pg_get_userbyid(c.relowner)='tagekyc_raw_export_deployer'
            AND NOT EXISTS (SELECT 1 FROM pg_catalog.aclexplode(COALESCE(c.relacl,pg_catalog.acldefault('r',c.relowner))) a
                            WHERE a.grantee<>c.relowner)) ok
          FROM expected_tables e JOIN pg_catalog.pg_namespace n ON n.nspname='tagekyc'
          JOIN pg_catalog.pg_class c ON c.relnamespace=n.oid AND c.relname=e.name AND c.relkind='r'),
        function_ok AS (
          SELECT pg_catalog.count(*)=9 AND pg_catalog.bool_and(
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
        expected_acl AS (
          SELECT name,args,'tagekyc_raw_export_package_delivery'::text grantee FROM expected_functions WHERE runtime),
        acl_ok AS (
          SELECT pg_catalog.count(*)=8
            AND NOT EXISTS (SELECT name,args,grantee FROM expected_acl EXCEPT SELECT name,args,grantee FROM actual_acl)
            AND NOT EXISTS (SELECT name,args,grantee FROM actual_acl EXCEPT SELECT name,args,grantee FROM expected_acl) ok
          FROM actual_acl),
        surface_ok AS (
          SELECT pg_catalog.count(*)=9 ok FROM pg_catalog.pg_proc p
          JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
          WHERE n.nspname='tagekyc' AND p.proname IN (SELECT name FROM expected_functions))
        SELECT r.ok AND m.ok AND t.ok AND f.ok AND a.ok AND s.ok
          AND pg_catalog.has_schema_privilege('tagekyc_raw_export_package_delivery','tagekyc','USAGE')
        FROM role_ok r CROSS JOIN member_ok m CROSS JOIN table_ok t CROSS JOIN function_ok f
          CROSS JOIN acl_ok a CROSS JOIN surface_ok s
        """;
}
