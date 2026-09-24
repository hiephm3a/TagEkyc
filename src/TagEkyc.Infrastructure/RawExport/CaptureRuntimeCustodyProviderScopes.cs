using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.Infrastructure.RawExport;

// Connection strings and object credentials belong to host configuration, never
// to an ingress request. Capture resolved values once; do not retain reloadable
// configuration or allow a caller to select a different role inside a scope.
internal sealed class CaptureRuntimeCustodyProviderScopes : IAsyncDisposable
{
    internal const string WriterLogin = "tagekyc_raw_export_encryptor_login";
    internal const string ReconcilerLogin = "tagekyc_raw_export_reconciler_login";
    internal const string LifecycleLogin = "tagekyc_raw_export_lifecycle_login";
    private readonly ServiceProvider[] owners;

    // Literal grants from DurableKeyProd, DurableObjectCustody, R2, R3,
    // R4R6 and the A3 forward migration. Bits: Writer=1, Reconciler=2,
    // Lifecycle=4. Expected rights never come from the live role's ACL.
    internal static IReadOnlyList<(string Signature, int Roles)> StageRights { get; } = Array.AsReadOnly<(string Signature, int Roles)>(
    [
        ("raw_export_prepare_attempt_key_reservation(uuid,uuid,uuid)",1),
        ("raw_export_record_key_provider_wrapped_result(uuid,uuid,uuid,bigint,text,bytea,bytea,bytea,text,integer,text,text)",1),
        ("raw_export_activate_attempt_key_reservation(uuid,uuid,bigint)",1),
        ("raw_export_inspect_attempt_key_reservation(uuid)",3),
        ("raw_export_read_active_attempt_key_envelope(uuid)",3),
        ("raw_export_mark_attempt_key_preparation_expired(uuid,uuid,bigint)",2),
        ("raw_export_resolve_attempt_key_provider_outcome(uuid,uuid,bigint,text,text,text,text)",2),
        ("raw_export_mark_key_provider_cleanup_required(uuid,uuid,uuid,bigint,text,text)",2),
        ("raw_export_record_key_provider_cleanup_observation(uuid,uuid,uuid,bigint,text,text,text,text)",2),
        ("raw_export_acknowledge_key_provider_cleanup(uuid,uuid,uuid,bigint,text,text,text,text)",2),
        ("raw_export_record_recovered_key_provider_result(uuid,uuid,bigint,text,bytea,bytea,bytea,text,integer,text,text)",2),
        ("raw_export_revoke_attempt_key_reservation(uuid,text)",6),
        ("raw_export_read_current_attempt_key_recovery_context(uuid)",2),
        ("raw_export_request_abandon_attempt_key_reservation(uuid,text)",4),
        ("raw_export_finalize_abandon_attempt_key_reservation(uuid)",4),
        ("raw_export_begin_provisional_object_custody(uuid,bigint,bigint,integer)",1),
        ("raw_export_arm_provisional_object_put(uuid,bigint,uuid)",1),
        ("raw_export_record_provisional_object_not_armed(uuid,bigint,bytea)",1),
        ("raw_export_record_provisional_object_put_result(uuid,bigint,uuid,text,integer,bigint,bytea,bytea)",1),
        ("raw_export_resolve_provisional_object_put_outcome(uuid,bigint,text,bigint,bytea,timestamptz,timestamptz,bytea)",2),
        ("raw_export_mark_provisional_object_verified(uuid,bigint,bytea)",2),
        ("raw_export_mark_provisional_object_cleanup_required(uuid,bigint,text,bytea)",6),
        ("raw_export_record_provisional_object_absence_confirmed(uuid,bigint,timestamptz,timestamptz,bytea)",2),
        ("raw_export_read_provisional_object_reconcile_context(uuid)",2),
        ("raw_export_record_provisional_object_delete_acknowledged(uuid,bigint,integer,bytea)",4),
        ("raw_export_record_provisional_object_quarantined(uuid,bigint,text,bytea)",4),
        ("raw_export_read_provisional_object_lifecycle_context(uuid)",4),
        ("raw_export_read_source_encryption_context(uuid,bigint,bigint)",1),
        ("raw_export_read_source_verification_context(uuid,bigint,bigint)",2),
        ("raw_export_terminate_source_encryption_attempt(uuid,bigint,bigint,text)",2),
        ("raw_export_stage_verified_source_ciphertext(uuid,uuid,bigint,bigint,bigint,bigint)",2),
        ("raw_export_commit_staged_source(uuid,bigint,bigint,bigint,bigint)",2),
        ("raw_export_publish_available_source(uuid,bigint,bigint)",2),
        ("raw_export_read_next_source_cleanup_item(uuid,bigint)",6),
        ("raw_export_complete_source_cleanup_item(uuid,bigint)",4),
        ("raw_export_finalize_source_cleanup(uuid,bigint)",4),
        ("raw_export_read_retained_source_continuation(uuid)",7),
        ("raw_export_list_retained_source_continuations(uuid,integer)",6),
        ("raw_export_record_retained_r2_terminal_intent(uuid,uuid,bigint,bigint,text,text)",3),
        ("raw_export_finalize_retained_r2_terminal(uuid,uuid,bigint,bigint)",2),
        ("raw_export_terminate_retained_r2_before_provider_start(uuid,uuid,bigint,bigint)",2),
    ]);

    internal CaptureRuntimeCustodyProviderScopes(
        string writerConnection, IConfiguration writerConfiguration,
        string reconcilerConnection, IConfiguration reconcilerConfiguration,
        string lifecycleConnection, IConfiguration lifecycleConfiguration)
        : this(writerConnection, ProvisionalObjectCustodyOptions.Resolve(writerConfiguration),
            reconcilerConnection, ProvisionalObjectCustodyOptions.Resolve(reconcilerConfiguration),
            lifecycleConnection, ProvisionalObjectCustodyOptions.Resolve(lifecycleConfiguration))
    {
    }

    internal CaptureRuntimeCustodyProviderScopes(
        string writerConnection, ProvisionalObjectCustodyOptions writerOptions,
        string reconcilerConnection, ProvisionalObjectCustodyOptions reconcilerOptions,
        string lifecycleConnection, ProvisionalObjectCustodyOptions lifecycleOptions)
    {
        var connections = new[] { writerConnection, reconcilerConnection, lifecycleConnection };
        var logins = new[] { WriterLogin, ReconcilerLogin, LifecycleLogin };
        var options = new[] { writerOptions, reconcilerOptions, lifecycleOptions };
        var builders = connections.Select(value => new NpgsqlConnectionStringBuilder(value)).ToArray();
        for (var i = 0; i < 3; i++)
        {
            if (builders[i].Username != logins[i] || !string.IsNullOrEmpty(builders[i].Options)
                || string.IsNullOrWhiteSpace(builders[i].Host) || string.IsNullOrWhiteSpace(builders[i].Database)
                || builders[i].Host != builders[0].Host || builders[i].Port != builders[0].Port
                || builders[i].Database != builders[0].Database)
                throw Invalid("CONNECTION_OWNER");
            if (!options[i].IsSyntacticallyValid
                || options[i].Topology != ProvisionalObjectTopology.S3CompatibleDurable
                || options[i].Capability != Capability(i)
                || options[i].ServiceUrl != options[0].ServiceUrl || options[i].BucketName != options[0].BucketName)
                throw Invalid("OBJECT_OWNER");
        }
        if (options.Select(x => x.AccessKeyId).Distinct(StringComparer.Ordinal).Count() != 3)
            throw Invalid("COMBINED_OBJECT_CREDENTIAL");

        // Build three roots, not three scopes underneath an all-role root.
        // Each root owns its data source; each opened scope owns its DbContext
        // and S3 client. None is borrowed from the API or broker container.
        // R2/stage transactions and continuation scopes never enlist in a
        // caller's ambient transaction, including later connections from these
        // data sources. Suppressing only the qualification call is insufficient.
        foreach (var builder in builders) builder.Enlist = false;
        owners = Enumerable.Range(0, 3).Select(i => Build(builders[i].ConnectionString, options[i], i)).ToArray();
    }

    internal Task<RoleScope> OpenWriterAsync(CancellationToken ct) => OpenAsync(0, ct);
    internal Task<RoleScope> OpenReconcilerAsync(CancellationToken ct) => OpenAsync(1, ct);
    internal Task<RoleScope> OpenLifecycleAsync(CancellationToken ct) => OpenAsync(2, ct);

    private async Task<RoleScope> OpenAsync(int role, CancellationToken ct)
    {
        var scope = owners[role].CreateAsyncScope();
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<TagEkycDbContext>();
            // Keep this exact qualified physical connection open for the scope.
            // A username in a connection string is not proof of its SQL actor.
            await db.Database.OpenConnectionAsync(ct);
            await scope.ServiceProvider.GetRequiredService<CustodyRoleReadinessValidator>().ValidateAsync(ct);
            await using var sql = new NpgsqlCommand("""
                SELECT session_user=$1 AND current_user=session_user
                  AND (SELECT array_agg(rolname::text ORDER BY rolname)
                    FROM pg_catalog.pg_roles WHERE rolname<>session_user
                      AND pg_catalog.pg_has_role(session_user,oid,'MEMBER'))=ARRAY[$2]::text[]
                  AND NOT EXISTS(SELECT 1 FROM pg_catalog.pg_roles other
                    WHERE other.rolname<>session_user AND NOT other.rolsuper
                      AND pg_catalog.pg_has_role(other.oid,session_user,'MEMBER'))
                """, (NpgsqlConnection)db.Database.GetDbConnection());
            sql.Parameters.AddWithValue(Login(role));
            sql.Parameters.AddWithValue(DatabaseCapability(role));
            if (await sql.ExecuteScalarAsync(ct) is not true) throw Invalid("ACTUAL_DATABASE_ACTOR");
            await QualifyStagePrivilegesAsync((NpgsqlConnection)db.Database.GetDbConnection(), role, ct);
            // Provider resolution follows database qualification. The existing
            // production object/GovArt readiness gate is NOT bypassed or changed
            // here: a successful role scope is not production activation.
            _ = scope.ServiceProvider.GetRequiredService(ProviderType(role));
            return new RoleScope(scope);
        }
        catch
        {
            await scope.DisposeAsync();
            throw;
        }
    }

    private static async Task QualifyStagePrivilegesAsync(NpgsqlConnection connection, int role, CancellationToken ct)
    {
        await using var sql = new NpgsqlCommand("""
            WITH expected AS (SELECT * FROM unnest($1::text[],$2::boolean[]) AS x(signature,allowed)),
            functions AS (SELECT e.*,p.* FROM expected e LEFT JOIN pg_catalog.pg_proc p
              ON p.oid=pg_catalog.to_regprocedure(e.signature))
            SELECT NOT EXISTS(SELECT 1 FROM functions f WHERE f.oid IS NULL
              OR NOT f.prosecdef OR pg_catalog.pg_get_userbyid(f.proowner)<>'tagekyc_raw_export_deployer'
              OR f.proconfig IS DISTINCT FROM ARRAY['search_path=pg_catalog']::text[]
              OR pg_catalog.has_function_privilege(session_user,f.oid,'EXECUTE') IS DISTINCT FROM f.allowed
              OR pg_catalog.has_function_privilege(session_user,f.oid,'EXECUTE WITH GRANT OPTION'))
            AND NOT EXISTS(SELECT 1 FROM pg_catalog.pg_proc p
              JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
              WHERE n.nspname='tagekyc' AND p.proname=ANY($3::text[])
                AND NOT EXISTS(SELECT 1 FROM functions f WHERE f.oid=p.oid)
                AND pg_catalog.has_function_privilege(session_user,p.oid,'EXECUTE'))
            AND NOT pg_catalog.has_schema_privilege(session_user,'tagekyc','CREATE')
            AND NOT EXISTS(SELECT 1 FROM pg_catalog.pg_class t
              JOIN pg_catalog.pg_namespace n ON n.oid=t.relnamespace
              WHERE n.nspname='tagekyc' AND t.relkind IN ('r','p','v','m','f') AND
                (pg_catalog.has_table_privilege(session_user,t.oid,'SELECT,INSERT,UPDATE,DELETE,TRUNCATE,REFERENCES,TRIGGER')
                 OR pg_catalog.has_any_column_privilege(session_user,t.oid,'SELECT,INSERT,UPDATE,REFERENCES')))
            """, connection);
        sql.Parameters.AddWithValue(StageRights.Select(x => "tagekyc." + x.Signature).ToArray());
        sql.Parameters.AddWithValue(StageRights.Select(x => (x.Roles & (1 << role)) != 0).ToArray());
        sql.Parameters.AddWithValue(StageRights.Select(x => x.Signature[..x.Signature.IndexOf('(')]).Concat(new[]
        {
            // Broker-only and deployer-only admission boundaries are not custody
            // stage authority, even if someone grants a LOGIN direct EXECUTE.
            "capture_runtime_read_bound_raw_ingress", "raw_export_begin_retained_source_ingress_with_authority",
            "complete_raw_export_source_ingress_claim_with_r2_handoff", "raw_export_append_retained_authority_snapshot",
            "raw_export_begin_source_ingress_with_authority_core", "raw_export_reenter_retained_source"
        }).ToArray());
        if (await sql.ExecuteScalarAsync(ct) is not true) throw Invalid("STAGE_PRIVILEGES");
    }

    private static ServiceProvider Build(string connection, ProvisionalObjectCustodyOptions options, int role)
    {
        var services = new ServiceCollection();
        services.AddSingleton(_ => NpgsqlDataSource.Create(connection));
        // The role root owns the pool and the child owns its connection. Passing
        // a host-specific data source into EF options would retain every root in
        // EF's process-wide internal-provider cache after the root is disposed.
        services.AddScoped(sp => sp.GetRequiredService<NpgsqlDataSource>().CreateConnection());
        services.AddDbContext<TagEkycDbContext>((sp, builder) => builder.UseNpgsql(sp.GetRequiredService<NpgsqlConnection>()));
        services.AddSingleton(options);
        services.AddScoped<CustodyRoleReadinessValidator>();
        services.AddScoped<ProvisionalObjectCustodyRepository>();
        services.AddScoped<ProvisionalObjectCustodyReadinessValidator>();
        // Explicit factories preserve the existing internal provider constructors.
        switch (role)
        {
            case 0: services.AddScoped<IProvisionalObjectWriter>(_ => new S3CompatibleProvisionalObjectWriter(options)); break;
            case 1: services.AddScoped<IProvisionalObjectReconciler>(_ => new S3CompatibleProvisionalObjectReconciler(options)); break;
            case 2: services.AddScoped<IProvisionalObjectLifecycle>(_ => new S3CompatibleProvisionalObjectLifecycle(options)); break;
            default: throw Invalid("ROLE");
        }
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    private static ProvisionalObjectCapability Capability(int role) => role switch
    {
        0 => ProvisionalObjectCapability.Writer, 1 => ProvisionalObjectCapability.Reconciler,
        2 => ProvisionalObjectCapability.Lifecycle, _ => throw Invalid("ROLE")
    };
    private static string Login(int role) => role switch
    {
        0 => WriterLogin, 1 => ReconcilerLogin, 2 => LifecycleLogin, _ => throw Invalid("ROLE")
    };
    private static string DatabaseCapability(int role) => role switch
    {
        0 => "tagekyc_raw_export_custody_encryptor", 1 => "tagekyc_raw_export_reconciler",
        2 => "tagekyc_raw_export_lifecycle", _ => throw Invalid("ROLE")
    };
    private static Type ProviderType(int role) => role switch
    {
        0 => typeof(IProvisionalObjectWriter), 1 => typeof(IProvisionalObjectReconciler),
        2 => typeof(IProvisionalObjectLifecycle), _ => throw Invalid("ROLE")
    };
    private static InvalidOperationException Invalid(string reason) => new("RAW_INGRESS_CUSTODY_SCOPE_" + reason);

    public async ValueTask DisposeAsync()
    {
        foreach (var owner in owners) await owner.DisposeAsync();
    }

    internal sealed class RoleScope(AsyncServiceScope scope) : IAsyncDisposable
    {
        internal IServiceProvider Services => scope.ServiceProvider;
        public ValueTask DisposeAsync() => scope.DisposeAsync();
    }
}
