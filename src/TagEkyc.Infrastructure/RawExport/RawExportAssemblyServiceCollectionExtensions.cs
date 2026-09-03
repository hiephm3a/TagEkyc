using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using TagEkyc.Application.Ports;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.Infrastructure.RawExport;

public static class RawExportAssemblyServiceCollectionExtensions
{
    public static IServiceCollection AddTagEkycRawExportAssembly(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isProduction)
    {
        var options = RawExportAssemblyOptions.Resolve(configuration);
        services.TryAddSingleton(options);
        services.TryAddSingleton(new RawExportAssemblyHostPosture(isProduction));
        services.TryAddScoped<RawExportAssemblyReadinessValidator>();
        if (options.Topology != RawExportAssemblyTopology.FixtureProof || isProduction)
            return services;

        services.TryAddScoped<RawExportAssemblyRepository>();
        services.TryAddScoped<RawExportFramedSourceVerificationService>();
        services.TryAddScoped<RawExportAssemblySourceResolver>();
        services.TryAddScoped<RawExportAssemblyAuthenticationService>();
        services.TryAddScoped<RawExportAssemblyOrchestrator>();
        services.TryAddScoped<IRawExportAssemblyOrchestrator>(provider =>
            provider.GetRequiredService<RawExportAssemblyOrchestrator>());
        return services;
    }
}

public sealed record RawExportAssemblyHostPosture(bool IsProduction);

public sealed class RawExportAssemblyReadinessException(string code) : Exception(code)
{
    public string Code { get; } = code;
}

public sealed class RawExportAssemblyReadinessValidator(
    RawExportAssemblyOptions options,
    RawExportAssemblyHostPosture host,
    IServiceProvider services)
{
    public async Task ValidateAsync(CancellationToken cancellationToken)
    {
        if (options.Topology == RawExportAssemblyTopology.Invalid
            || (host.IsProduction && options.Topology != RawExportAssemblyTopology.Disabled))
            throw new RawExportAssemblyReadinessException(RawExportAssemblyOptions.ConfigInvalid);
        if (options.Topology == RawExportAssemblyTopology.Disabled) return;

        if (services.GetService<IRawExportAssemblyAuthenticationProvider>() is null)
            throw new RawExportAssemblyReadinessException(RawExportAssemblyOptions.AuthenticatorUnavailable);
        if (services.GetService<IC2AssemblyPreparationProvider>() is null)
            throw new RawExportAssemblyReadinessException(RawExportAssemblyOptions.C2Unavailable);
        if (services.GetService<IRawExportAssemblyConnectionFactory>() is null)
            throw new RawExportAssemblyReadinessException(RawExportAssemblyOptions.RoleTopologyInvalid);

        var db = services.GetService<TagEkycDbContext>()
            ?? throw new RawExportAssemblyReadinessException(RawExportAssemblyOptions.RoleTopologyInvalid);
        var connection = db.Database.GetDbConnection() as NpgsqlConnection
            ?? throw new RawExportAssemblyReadinessException(RawExportAssemblyOptions.RoleTopologyInvalid);
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            WITH expected_roles(role_name,can_login) AS (VALUES
              ('tagekyc_raw_export_assembly_resolver',false),
              ('tagekyc_raw_export_assembly_sealer',false),
              ('tagekyc_raw_export_assembly_resolver_login',true),
              ('tagekyc_raw_export_assembly_sealer_login',true)),
            expected_members(member_name,granted_name) AS (VALUES
              ('tagekyc_raw_export_assembly_resolver_login','tagekyc_raw_export_assembly_resolver'),
              ('tagekyc_raw_export_assembly_sealer_login','tagekyc_raw_export_assembly_sealer')),
            expected_tables(name) AS (VALUES
              ('raw_export_job_source_bindings'),('raw_export_assembly_preparation_dispositions'),
              ('raw_export_assembly_identities'),('raw_export_assembly_items')),
            expected_functions(name,args,grantee) AS (VALUES
              ('raw_export_freeze_job_source_bindings','uuid, uuid, bigint, bigint, uuid','tagekyc_raw_export_assembly_resolver'),
              ('raw_export_read_job_source_verification_context','uuid, integer, uuid, bigint, bigint, uuid','tagekyc_raw_export_assembly_resolver'),
              ('raw_export_register_assembly_preparing','uuid, uuid, uuid, bigint, bigint, bytea, bytea','tagekyc_raw_export_assembly_sealer'),
              ('raw_export_record_assembly_pending','uuid, bigint, bytea','tagekyc_raw_export_assembly_sealer'),
              ('raw_export_seal_authenticated_assembly','uuid, uuid, bigint, bigint, bigint, uuid, bytea, bytea, bytea, text, integer, bytea, bigint, integer, jsonb','tagekyc_raw_export_assembly_sealer'),
              ('raw_export_authorize_assembly_abort','uuid, bigint, bytea','tagekyc_raw_export_assembly_sealer'),
              ('raw_export_record_assembly_finalized','uuid, bigint, bytea','tagekyc_raw_export_assembly_sealer'),
              ('raw_export_record_assembly_aborted','uuid, bigint, bytea','tagekyc_raw_export_assembly_sealer'),
              ('raw_export_read_assembly_recovery_context','uuid','tagekyc_raw_export_assembly_sealer'),
              ('raw_export_read_committed_assembly_recovery_context','uuid, uuid, bigint, bigint','tagekyc_raw_export_assembly_sealer')),
            role_check AS (
              SELECT pg_catalog.count(*)=4 AND pg_catalog.bool_and(
                r.rolinherit AND NOT r.rolsuper AND NOT r.rolcreatedb AND NOT r.rolcreaterole
                AND NOT r.rolreplication AND NOT r.rolbypassrls AND r.rolcanlogin=e.can_login) AS ok
              FROM expected_roles e JOIN pg_catalog.pg_roles r ON r.rolname=e.role_name),
            membership_check AS (
              SELECT pg_catalog.count(*)=2 AND pg_catalog.bool_and(
                em.member_name IS NOT NULL AND NOT m.admin_option AND m.inherit_option AND NOT m.set_option) AS ok
              FROM pg_catalog.pg_auth_members m
              JOIN pg_catalog.pg_roles member ON member.oid=m.member
              JOIN pg_catalog.pg_roles granted ON granted.oid=m.roleid
              LEFT JOIN expected_members em ON em.member_name=member.rolname AND em.granted_name=granted.rolname
              WHERE member.rolname IN (SELECT role_name FROM expected_roles)
                 OR granted.rolname IN (SELECT role_name FROM expected_roles)),
            table_check AS (
              SELECT pg_catalog.count(*)=4 AND pg_catalog.bool_and(
                pg_catalog.pg_get_userbyid(c.relowner)='tagekyc_raw_export_deployer'
                AND NOT EXISTS (
                  SELECT 1 FROM pg_catalog.aclexplode(COALESCE(c.relacl,pg_catalog.acldefault('r',c.relowner))) a
                  WHERE a.grantee<>c.relowner)) AS ok
              FROM expected_tables e
              JOIN pg_catalog.pg_namespace n ON n.nspname='tagekyc'
              JOIN pg_catalog.pg_class c ON c.relnamespace=n.oid AND c.relname=e.name AND c.relkind='r'),
            function_check AS (
              SELECT pg_catalog.count(*)=10 AND pg_catalog.bool_and(
                pg_catalog.pg_get_userbyid(p.proowner)='tagekyc_raw_export_deployer'
                AND p.prosecdef AND p.proconfig=ARRAY['search_path=pg_catalog']::text[]
                AND EXISTS (
                  SELECT 1 FROM pg_catalog.aclexplode(COALESCE(p.proacl,pg_catalog.acldefault('f',p.proowner))) a
                  JOIN pg_catalog.pg_roles expected_grantee ON expected_grantee.oid=a.grantee
                  WHERE a.privilege_type='EXECUTE'
                    AND expected_grantee.rolname=e.grantee
                    AND a.grantor=p.proowner
                    AND NOT a.is_grantable)
                AND NOT EXISTS (
                  SELECT 1 FROM pg_catalog.aclexplode(COALESCE(p.proacl,pg_catalog.acldefault('f',p.proowner))) a
                  WHERE a.privilege_type='EXECUTE'
                    AND (a.grantee NOT IN (p.proowner,(SELECT oid FROM pg_catalog.pg_roles WHERE rolname=e.grantee))
                      OR a.grantor<>p.proowner
                      OR a.is_grantable))) AS ok
              FROM expected_functions e
              JOIN pg_catalog.pg_namespace n ON n.nspname='tagekyc'
              JOIN pg_catalog.pg_proc p ON p.pronamespace=n.oid AND p.proname=e.name
                AND pg_catalog.oidvectortypes(p.proargtypes)=e.args),
            function_surface_check AS (
              SELECT pg_catalog.count(*)=10 AS ok
              FROM pg_catalog.pg_proc p
              JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
              WHERE n.nspname='tagekyc'
                AND p.proname IN (SELECT name FROM expected_functions))
            SELECT r.ok AND m.ok AND t.ok AND f.ok AND fs.ok
              AND pg_catalog.has_schema_privilege('tagekyc_raw_export_assembly_resolver','tagekyc','USAGE')
              AND pg_catalog.has_schema_privilege('tagekyc_raw_export_assembly_sealer','tagekyc','USAGE')
            FROM role_check r CROSS JOIN membership_check m CROSS JOIN table_check t CROSS JOIN function_check f
              CROSS JOIN function_surface_check fs
            """;
        var valid = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        if (valid is not true)
            throw new RawExportAssemblyReadinessException(RawExportAssemblyOptions.RoleTopologyInvalid);
    }
}
