using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Npgsql;
using TagEkyc.Application.CaptureRuntime;
using TagEkyc.Application.Ports;
using TagEkyc.Application.RawExport;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.Infrastructure.RawExport;

// ASP.NET-free adapter: the thin host supplies socket/header facts, never
// Forwarded-derived addresses. Its stream contains private JSON metadata only.
public sealed record RawIngressBrokerNetworkRequest(
    IPAddress? RemoteAddress, IPAddress? LocalAddress, int LocalPort, string Host,
    string Method, string Path, bool HasQuery, string ContentType, long? ContentLength,
    bool EncodedOrChunked, bool AmbiguousHeaders);
public sealed record RawIngressBrokerReply(int StatusCode, byte[] Body);

public sealed class RawIngressBrokerTransport(RawIngressBrokerOptions options, Func<IRawIngressMetadataBroker> broker)
{
    public async Task<RawIngressBrokerReply> HandleAsync(RawIngressBrokerNetworkRequest request,
        Stream metadata, CancellationToken cancellationToken)
    {
        if (!options.IsAllowedPeer(request.RemoteAddress)
            || !options.IsBoundEndpoint(request.LocalAddress, request.LocalPort, request.Host)) return Empty(403);
        if (request.Method != "POST" || request.Path != RawIngressBrokerOptions.AdmitPath || request.HasQuery
            || request.ContentType != "application/json" || request.EncodedOrChunked || request.AmbiguousHeaders
            || request.ContentLength is not (>= 1 and <= RawIngressBrokerProtocol.RequestLimit)) return Empty(400);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(options.RequestTimeoutMilliseconds);
        CaptureRuntimeRawIngressAdmissionContext input;
        try
        {
            input = RawIngressBrokerProtocol.ReadRequest(await RawIngressBrokerProtocol.ReadExactlyBounded(metadata,
                checked((int)request.ContentLength.Value), deadline.Token));
        }
        catch (Exception e) when (e is JsonException or DecoderFallbackException or InvalidOperationException
            or FormatException or OverflowException or IOException) { return Empty(400); }
        catch (OperationCanceledException) { return Empty(503); }
        try
        {
            // Lazy resolution follows network + complete bounded syntax checks.
            // Missing providers never select the fixture catalog as a fallback.
            var result = await broker().AdmitAsync(input, deadline.Token);
            return new(200, RawIngressBrokerProtocol.WriteResponse(result));
        }
        catch (Exception) { return Empty(503); }
    }
    private static RawIngressBrokerReply Empty(int status) => new(status, []);
}

public static class CaptureRuntimeRawIngressComposition
{
    // A3 deliberately does not source these owners from an HTTP request or from
    // the ordinary runtime connection. A later production authority must supply
    // the six exact owners; the synthetic A3 host supplies fixture-only values.
    public sealed record RuntimeOwners(
        string WriterConnectionString, ProvisionalObjectCustodyOptions WriterObject,
        string ReconcilerConnectionString, ProvisionalObjectCustodyOptions ReconcilerObject,
        string LifecycleConnectionString, ProvisionalObjectCustodyOptions LifecycleObject,
        long MaximumPlaintextWindowBytesPerStream);

    public static IServiceCollection AddTagEkycCaptureRuntimeRawIngress(
        this IServiceCollection services, RawIngressBrokerOptions brokerOptions, RuntimeOwners owners)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(brokerOptions);
        ArgumentNullException.ThrowIfNull(owners);
        if (owners.MaximumPlaintextWindowBytesPerStream <= 0)
            throw new InvalidOperationException("RAW_EXPORT_SOURCE_CAPACITY_CONFIGURATION_INVALID");

        services.TryAddSingleton(brokerOptions);
        services.TryAddSingleton(owners);
        return AddA3Graph(services);
    }

    // The ordinary API host registers the graph even in Prepared, but does not
    // choose owner credentials, object stores, or provider implementations.
    // Activated readiness resolves those explicit dependencies and fails closed
    // when they have not been supplied by an authorized host configuration.
    public static IServiceCollection AddTagEkycCaptureRuntimeRawIngressHostGraph(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return AddA3Graph(services);
    }

    private static IServiceCollection AddA3Graph(IServiceCollection services)
    {
        services.TryAddSingleton<RawSourceRetentionProfileValidator>(sp =>
            new(sp.GetRequiredService<IConfiguration>()));
        services.TryAddSingleton<IRawSourceRetentionProfileProvider>(sp =>
            sp.GetRequiredService<RawSourceRetentionProfileValidator>());
        services.TryAddSingleton<ConfiguredRawExportCaptureAcceptancePolicyProvider>(sp =>
            new(sp.GetRequiredService<IConfiguration>()));
        services.TryAddSingleton<IRawExportCaptureAcceptancePolicyProvider>(sp =>
            sp.GetRequiredService<ConfiguredRawExportCaptureAcceptancePolicyProvider>());
        services.TryAddSingleton(sp => new RawSourceRetentionReadinessValidator(
            sp.GetRequiredService<RuntimeOwners>().ReconcilerConnectionString,
            sp.GetRequiredService<RawSourceRetentionProfileValidator>(),
            sp.GetRequiredService<ConfiguredRawExportCaptureAcceptancePolicyProvider>()));
        services.TryAddSingleton(sp =>
        {
            var owners = sp.GetRequiredService<RuntimeOwners>();
            return new CaptureRuntimeCustodyProviderScopes(
                owners.WriterConnectionString, owners.WriterObject,
                owners.ReconcilerConnectionString, owners.ReconcilerObject,
                owners.LifecycleConnectionString, owners.LifecycleObject);
        });
        services.TryAddSingleton<RawIngressBrokerHttpClient>(sp =>
            new(sp.GetRequiredService<RawIngressBrokerOptions>()));
        services.TryAddSingleton<IRawIngressMetadataBroker>(sp =>
            sp.GetRequiredService<RawIngressBrokerHttpClient>());
        services.TryAddSingleton<ICaptureRuntimeRawIngressBodyPipeline>(sp =>
            new CaptureRuntimeRawIngressBodyPipeline(
                sp.GetRequiredService<CaptureRuntimeCustodyProviderScopes>(),
                sp.GetRequiredService<IKekOperationProvider>(),
                sp.GetRequiredService<IKekProvisioningRecoveryOperation>(),
                sp.GetRequiredService<IContentCommitmentService>(),
                sp.GetRequiredService<DurableKeyCustodyOptions>(),
                sp.GetRequiredService<RawIngressBrokerOptions>(),
                sp.GetRequiredService<ICaptureRuntimeHostLifetime>().Stopping));
        services.TryAddScoped<ICaptureRuntimeRawIngressAdmission>(sp =>
            new DeferredCaptureRuntimeRawIngressAdmission(sp));
        services.TryAddSingleton<CaptureRuntimeA3Runtime>();
        services.TryAddSingleton<ICaptureRuntimeA3Readiness>(sp => sp.GetRequiredService<CaptureRuntimeA3Runtime>());
        services.TryAddSingleton<ICaptureRuntimeA3Worker>(sp => sp.GetRequiredService<CaptureRuntimeA3Runtime>());
        return services;
    }

    public static IServiceCollection AddTagEkycRawIngressBrokerTransport(this IServiceCollection services,
        RawIngressBrokerOptions options)
    {
        services.AddSingleton(options);
        services.AddScoped<IRawIngressMetadataBroker>(sp =>
        {
            var source = sp.GetRequiredService<NpgsqlDataSource>();
            var connection = new NpgsqlConnectionStringBuilder(source.ConnectionString);
            if (connection.Username != "tagekyc_raw_export_claim_broker_login" || !string.IsNullOrEmpty(connection.Options))
                throw new InvalidOperationException("RAW_INGRESS_BROKER_LOGIN_INVALID");
            var commitment = sp.GetRequiredService<IContentCommitmentService>();
            var subject = sp.GetRequiredService<ISubjectRefTokenService>();
            var profiles = QualifiedBrokerProfiles.Capture(sp.GetRequiredService<ICustodyProfileProvider>());
            return new QualifiedRawIngressBroker(source, commitment, subject, options,
                new RawIngressBrokerTransactionFacade(source,
                    new RetainedSourceClaimPreflight(commitment, subject), profiles, options.TransactionSettings()));
        });
        services.AddScoped(sp => new RawIngressBrokerTransport(options, () => sp.GetRequiredService<IRawIngressMetadataBroker>()));
        return services;
    }
}

// Prepared startup may resolve these two public ports, but must not construct
// role pools or provider clients. Actual dependencies are resolved only from
// Activated readiness, a request, or the Activated worker.
internal sealed class DeferredCaptureRuntimeRawIngressAdmission(
    IServiceProvider services)
    : ICaptureRuntimeRawIngressAdmission
{
    public ValueTask<CaptureRuntimeRawIngressAdmissionResult> AdmitAsync(
        CaptureRuntimeRawIngressAdmissionContext context, Stream body, CancellationToken cancellationToken) =>
        new TagEkyc.Application.RawExport.CaptureRuntimeRawIngressAdmissionService(
            services.GetRequiredService<IRawExportIngressCapacity>(),
            services.GetRequiredService<IRawIngressMetadataBroker>(),
            services.GetRequiredService<ICaptureRuntimeRawIngressBodyPipeline>(),
            services.GetRequiredService<CaptureRuntimeRawIngressComposition.RuntimeOwners>()
                .MaximumPlaintextWindowBytesPerStream).AdmitAsync(context, body, cancellationToken);
}

internal sealed class CaptureRuntimeA3Runtime(IServiceProvider services)
    : ICaptureRuntimeA3Readiness, ICaptureRuntimeA3Worker
{
    public async Task<bool> IsReadyAsync(CancellationToken ct)
    {
        try
        {
            var owners = services.GetRequiredService<CaptureRuntimeRawIngressComposition.RuntimeOwners>();
            if (owners.MaximumPlaintextWindowBytesPerStream <= 0) return false;
            // The ordinary host's /readiness check is not the Activated route
            // selector. Reject invalid custody bounds before routes or worker run.
            if (services.GetRequiredService<CustodyTimeBoundsState>().Value is null) return false;
            var keyOptions = services.GetRequiredService<DurableKeyCustodyOptions>();
            var keyProvider = services.GetRequiredService<IKekOperationProvider>();
            if (!keyOptions.IsValid || keyProvider is not IDurableKekProviderCapabilitySource capabilities
                || !capabilities.Capabilities.IsDurable || !capabilities.Capabilities.IsKekQualified
                || !capabilities.Capabilities.SupportsRecovery || !capabilities.Capabilities.SupportsCleanup)
                return false;
            _ = services.GetRequiredService<IKekProvisioningRecoveryOperation>();
            _ = services.GetRequiredService<IContentCommitmentService>();
            _ = services.GetRequiredService<IRawIngressMetadataBroker>();
            _ = services.GetRequiredService<IRawExportIngressCapacity>();
            _ = services.GetRequiredService<IRawExportCaptureAcceptancePolicyProvider>();
            _ = services.GetRequiredService<ICaptureRuntimeHostLifetime>();
            _ = services.GetRequiredService<ICaptureRuntimeRawIngressBodyPipeline>();
            if (!await services.GetRequiredService<RawSourceRetentionReadinessValidator>()
                .IsReadyAsync(ct).ConfigureAwait(false)) return false;
            var scopes = services.GetRequiredService<CaptureRuntimeCustodyProviderScopes>();
            await using var writer = await scopes.OpenWriterAsync(ct).ConfigureAwait(false);
            await using var reconciler = await scopes.OpenReconcilerAsync(ct).ConfigureAwait(false);
            await using var lifecycle = await scopes.OpenLifecycleAsync(ct).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch { return false; }
    }

    public Task RunAsync(CancellationToken stoppingToken)
    {
        var scopes = services.GetRequiredService<CaptureRuntimeCustodyProviderScopes>();
        var keyRecovery = services.GetRequiredService<IKekProvisioningRecoveryOperation>();
        var keyProvider = services.GetRequiredService<IKekOperationProvider>();
        var contentCommitments = services.GetRequiredService<IContentCommitmentService>();
        var keyOptions = services.GetRequiredService<DurableKeyCustodyOptions>();
        var brokerOptions = services.GetRequiredService<RawIngressBrokerOptions>();
        var logger = services.GetRequiredService<ILogger<CaptureRuntimeSourceContinuationWorker>>();
        var pipeline = new CaptureRuntimeSourcePipeline(scopes, keyRecovery, null,
            keyProvider, contentCommitments, keyOptions);
        return new CaptureRuntimeSourceContinuationWorker(pipeline, brokerOptions, logger)
            .RunAsync(stoppingToken);
    }
}

internal sealed record QualifiedBrokerProfiles(SourceEncryptionProfileBundle ActiveSourceEncryptionProfile,
    KekReferenceBundle ActiveKekReference, CustodyTimeBounds TimeBounds) : ICustodyProfileProvider
{
    internal static QualifiedBrokerProfiles Capture(ICustodyProfileProvider provider)
    {
        var p = provider.ActiveSourceEncryptionProfile;
        var k = provider.ActiveKekReference;
        var t = provider.TimeBounds;
        if (p is null || k is null || t is null
            || new[] { p.StorageProfileId, p.SourceEncryptionProfileId, p.EncryptionSuiteId,
                p.NonceStrategyId, k.KeyProviderId, k.KekId }.Any(string.IsNullOrWhiteSpace)
            || p.SourceEncryptionProfileVersion < 1 || p.EncryptionFramingVersion < 1 || p.ChunkSize < 1
            || k.KekVersion < 1 || k.KekFingerprint is not { Length: 64 }
            || k.KekFingerprint.Any(c => c is not (>= '0' and <= '9' or >= 'a' and <= 'f'))
            || !WholeUnits(t.SafetyMargin, TimeSpan.TicksPerMillisecond, 30000)
            || !WholeUnits(t.MaxRemainingContinuationWindow, TimeSpan.TicksPerSecond, 3600)
            || !WholeUnits(t.EncryptionAttemptDeadline, TimeSpan.TicksPerSecond, 3600)
            || !WholeUnits(t.OwnershipLeaseDuration, TimeSpan.TicksPerSecond, 3600))
            throw new InvalidOperationException("RAW_INGRESS_BROKER_PROFILE_NOT_READY");
        _ = RetainedSourceClaimPreflight.ComputeProfileDigests(p);
        // Freeze this request's exact qualified catalog projection. B cannot
        // observe a second, changed getter result after passing readiness.
        return new(p, k, t);
    }
    private static bool WholeUnits(TimeSpan value, long unit, int max) =>
        value.Ticks % unit == 0 && value.Ticks / unit >= 1 && value.Ticks / unit <= max;
}

// This read-only host qualification precedes B. It neither performs business
// lookups nor adds a fourth call inside the B-R/B-B/B-C transaction. Do not cache
// a successful role/provider check across requests or impersonate a DB role.
internal sealed class QualifiedRawIngressBroker(NpgsqlDataSource source,
    IContentCommitmentService commitment, ISubjectRefTokenService subject,
    RawIngressBrokerOptions options, IRawIngressMetadataBroker inner) : IRawIngressMetadataBroker
{
    internal const string Login = "tagekyc_raw_export_claim_broker_login";
    // Source-frozen stage families from DurableKeyProd, DurableObjectCustody,
    // R3 staging and source-finalization/cleanup grants, plus the C6B14 core.
    // Names intentionally cover every overload, regardless of current ACLs.
    internal static readonly string[] ForbiddenFunctions =
    [
        "raw_export_begin_source_ingress_with_authority_core",
        "raw_export_reenter_retained_source",
        "raw_export_acknowledge_key_provider_cleanup", "raw_export_activate_attempt_key_reservation",
        "raw_export_arm_provisional_object_put", "raw_export_begin_provisional_object_custody",
        "raw_export_commit_staged_source", "raw_export_complete_source_cleanup_item",
        "raw_export_finalize_abandon_attempt_key_reservation", "raw_export_finalize_source_cleanup",
        "raw_export_inspect_attempt_key_reservation", "raw_export_mark_attempt_key_preparation_expired",
        "raw_export_mark_key_provider_cleanup_required", "raw_export_mark_provisional_object_cleanup_required",
        "raw_export_mark_provisional_object_verified", "raw_export_prepare_attempt_key_reservation",
        "raw_export_publish_available_source", "raw_export_read_active_attempt_key_envelope",
        "raw_export_read_current_attempt_key_recovery_context", "raw_export_read_next_source_cleanup_item",
        "raw_export_read_provisional_object_lifecycle_context", "raw_export_read_provisional_object_reconcile_context",
        "raw_export_read_source_encryption_context", "raw_export_read_source_verification_context",
        "raw_export_record_key_provider_cleanup_observation", "raw_export_record_key_provider_wrapped_result",
        "raw_export_record_provisional_object_absence_confirmed", "raw_export_record_provisional_object_delete_acknowledged",
        "raw_export_record_provisional_object_not_armed", "raw_export_record_provisional_object_put_result",
        "raw_export_record_provisional_object_quarantined", "raw_export_record_recovered_key_provider_result",
        "raw_export_request_abandon_attempt_key_reservation", "raw_export_resolve_attempt_key_provider_outcome",
        "raw_export_resolve_provisional_object_put_outcome", "raw_export_revoke_attempt_key_reservation",
        "raw_export_stage_verified_source_ciphertext", "raw_export_terminate_source_encryption_attempt",
        "raw_export_terminate_retained_r2_before_provider_start",
        "raw_export_record_retained_r2_terminal_intent", "raw_export_finalize_retained_r2_terminal",
        "raw_export_read_retained_source_continuation", "raw_export_list_retained_source_continuations"
    ];
    internal static readonly string[] Functions =
    [
        "tagekyc.capture_runtime_read_bound_raw_ingress(uuid,uuid,uuid,bigint,uuid,bigint,uuid,uuid,integer,text,bigint,timestamptz)",
        "tagekyc.raw_export_begin_retained_source_ingress_with_authority(uuid,uuid,text,text,text,uuid,uuid,uuid,integer,text,text,bigint,text,timestamptz,timestamptz,timestamptz,integer,text,integer,uuid,integer,integer,uuid,bigint)",
        "tagekyc.complete_raw_export_source_ingress_claim_with_r2_handoff(uuid,text,text,text,uuid,bigint,bigint,text,timestamptz,text,bytea,integer,text,integer,bytea,integer,text,integer,bytea,bigint,text,timestamptz,timestamptz,timestamptz,integer,text,text,integer,text,integer,text,bytea,integer,bytea,text,text,integer,text,integer,integer,integer,integer)"
    ];

    public async Task<RawIngressBrokerResult> AdmitAsync(CaptureRuntimeRawIngressAdmissionContext input,
        CancellationToken cancellationToken)
    {
        try { await QualifyAsync(cancellationToken); }
        catch (RawIngressProviderCapabilityUnavailableException)
        {
            // The active selector failed the public, no-patient-data probe.
            // No B transaction or body read has begun, so O05 is exact here.
            return new RawIngressBrokerResult.Final(new(
                RawExportSourceIngressCodes.CapabilityUnavailable));
        }
        return await inner.AdmitAsync(input, cancellationToken);
    }

    internal async Task QualifyAsync(CancellationToken cancellationToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(options.RequestTimeoutMilliseconds);
        var ct = deadline.Token;
        await using (var connection = await source.OpenConnectionAsync(ct))
        await using (var sql = new NpgsqlCommand("""
            WITH actor AS (SELECT * FROM pg_catalog.pg_roles WHERE rolname=session_user),
            capabilities AS (SELECT * FROM pg_catalog.pg_roles
              WHERE rolname IN ('tagekyc_runtime','tagekyc_raw_export_claim_broker')),
            functions AS (SELECT p.* FROM unnest($2::text[]) expected(signature)
              JOIN pg_catalog.pg_proc p ON p.oid=pg_catalog.to_regprocedure(expected.signature))
            SELECT
              session_user=$1 AND current_user=session_user
              AND EXISTS(SELECT 1 FROM actor WHERE rolcanlogin AND rolinherit AND NOT
                (rolsuper OR rolcreatedb OR rolcreaterole OR rolreplication OR rolbypassrls))
              AND (SELECT count(*) FROM capabilities WHERE NOT rolcanlogin AND rolinherit AND NOT
                (rolsuper OR rolcreatedb OR rolcreaterole OR rolreplication OR rolbypassrls))=2
              AND (SELECT array_agg(rolname::text ORDER BY rolname) FROM pg_catalog.pg_roles
                WHERE rolname<>session_user AND pg_catalog.pg_has_role(session_user,oid,'MEMBER'))
                =ARRAY['tagekyc_raw_export_claim_broker','tagekyc_runtime']::text[]
              AND NOT EXISTS(SELECT 1 FROM pg_catalog.pg_auth_members m
                JOIN actor a ON a.oid=m.member WHERE m.admin_option OR NOT m.inherit_option)
              AND NOT EXISTS(SELECT 1 FROM pg_catalog.pg_auth_members m
                JOIN actor a ON a.oid=m.roleid)
              AND pg_catalog.has_schema_privilege(session_user,'tagekyc','USAGE')
              AND NOT pg_catalog.has_schema_privilege(session_user,'tagekyc','CREATE'),
              (SELECT count(*) FROM functions)=3
              AND NOT EXISTS(SELECT 1 FROM functions p WHERE NOT p.prosecdef
                OR pg_catalog.pg_get_userbyid(p.proowner)<>'tagekyc_raw_export_deployer'
                OR p.proconfig IS DISTINCT FROM ARRAY['search_path=pg_catalog']::text[]
                OR NOT pg_catalog.has_function_privilege(session_user,p.oid,'EXECUTE')
                OR NOT EXISTS(SELECT 1 FROM pg_catalog.aclexplode(p.proacl) acl
                  WHERE acl.grantee='tagekyc_raw_export_claim_broker'::regrole
                    AND acl.privilege_type='EXECUTE' AND NOT acl.is_grantable)
                OR EXISTS(SELECT 1 FROM pg_catalog.aclexplode(COALESCE(p.proacl,
                    pg_catalog.acldefault('f',p.proowner))) acl
                  WHERE acl.grantee NOT IN (p.proowner,'tagekyc_raw_export_claim_broker'::regrole)
                    OR (acl.grantee<>p.proowner AND acl.is_grantable))
                OR EXISTS(SELECT 1 FROM pg_catalog.pg_roles other WHERE
                  (other.rolname IN ('tagekyc_runtime','tagekyc_capture_runtime_application',
                    'tagekyc_capture_runtime_authenticator','tagekyc_capture_runtime_operator',
                    'tagekyc_raw_export_custody_encryptor','tagekyc_raw_export_reconciler',
                    'tagekyc_raw_export_lifecycle')
                   OR (NOT other.rolsuper AND NOT pg_catalog.pg_has_role(other.oid,p.proowner,'MEMBER')))
                  AND other.rolname NOT IN ('tagekyc_raw_export_claim_broker',session_user)
                  AND (pg_catalog.has_function_privilege(other.oid,p.oid,'EXECUTE')
                    OR pg_catalog.pg_has_role(other.oid,p.proowner,'MEMBER')
                    OR pg_catalog.pg_has_role(other.oid,'tagekyc_raw_export_claim_broker','MEMBER'))))
              AND NOT EXISTS(SELECT 1 FROM pg_catalog.pg_proc p
                JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
                WHERE n.nspname='tagekyc' AND p.proname=ANY($3::text[])
                  AND pg_catalog.has_function_privilege(session_user,p.oid,'EXECUTE')),
              NOT EXISTS(SELECT 1 FROM pg_catalog.pg_class t
                JOIN pg_catalog.pg_namespace n ON n.oid=t.relnamespace CROSS JOIN actor a
                WHERE n.nspname='tagekyc' AND t.relkind IN ('r','p','v','m','f','S') AND
                  (t.relowner=a.oid
                    OR EXISTS(SELECT 1 FROM pg_catalog.aclexplode(t.relacl) acl WHERE acl.grantee=a.oid)
                    OR EXISTS(SELECT 1 FROM pg_catalog.pg_attribute col,
                        LATERAL pg_catalog.aclexplode(col.attacl) acl
                      WHERE col.attrelid=t.oid AND acl.grantee=a.oid)
                    OR (t.relkind<>'S' AND pg_catalog.has_table_privilege(session_user,t.oid,
                      'INSERT,UPDATE,DELETE,TRUNCATE,REFERENCES,TRIGGER'))
                    OR (t.relkind<>'S' AND pg_catalog.has_any_column_privilege(session_user,t.oid,
                      'INSERT,UPDATE,REFERENCES'))))
            """, connection))
        {
            sql.Parameters.AddWithValue(Login);
            sql.Parameters.AddWithValue(Functions);
            sql.Parameters.AddWithValue(ForbiddenFunctions);
            await using var row = await sql.ExecuteReaderAsync(ct);
            if (!await row.ReadAsync(ct) || row.FieldCount != 3
                || Enumerable.Range(0, 3).Any(i => row.IsDBNull(i) || !row.GetBoolean(i)))
                throw new InvalidOperationException("RAW_INGRESS_BROKER_DATABASE_NOT_READY");
        }
        // Public, non-patient probe bytes; no durable fingerprint or secret is
        // produced by this health check. Resolve the configured versions through
        // the real keyed services, never silently select a fixture/latest key.
        var mac = await commitment.ComputeAsync(new(options.CommitmentSelectorId, options.CommitmentSelectorVersion),
            ReadOnlyMemory<byte>.Empty, ct);
        var token = await subject.ComputeAsync(new(options.SubjectTokenSelectorId, options.SubjectTokenSelectorVersion),
            ReadOnlyMemory<byte>.Empty, ct);
        if (!mac.IsSuccess || !token.IsSuccess)
            throw new RawIngressProviderCapabilityUnavailableException(
                "RAW_INGRESS_BROKER_PROVIDER_NOT_READY");
    }
}
