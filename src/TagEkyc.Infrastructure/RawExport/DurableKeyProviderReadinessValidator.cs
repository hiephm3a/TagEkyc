using Microsoft.EntityFrameworkCore;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.Infrastructure.RawExport;

public sealed class DurableKeyProviderReadinessValidator(
    DurableKeyTopologyOptions topology,
    DurableKeyCustodyOptions options,
    CsprngReadinessValidator csprng,
    TagEkycDbContext db,
    IServiceProvider services) : IDurableKeyReadinessValidator
{
    public static readonly string[] Codes =
    [
        "PROD_RAW_EXPORT_PROVIDER_TOPOLOGY_INVALID",
        "PROD_RAW_EXPORT_KEY_PROVIDER_DURABILITY_UNSUPPORTED",
        CsprngReadinessValidator.Code,
        "PROD_RAW_EXPORT_KEK_NOT_QUALIFIED",
        "PROD_RAW_EXPORT_HASH_HELPER_ACL_INVALID",
        "PROD_RAW_EXPORT_KEY_RETRY_CONFIG_INVALID",
    ];

    public async Task ValidateAsync(CancellationToken cancellationToken)
    {
        if (topology.Topology != DurableKeyTopology.DurableKey)
            throw new DurableKeyReadinessException(Codes[0]);
        var provider = services.GetService(typeof(IKekOperationProvider));
        var recovery = services.GetService(typeof(IKekProvisioningRecoveryOperation));
        if (provider is not IDurableKekProviderCapabilitySource capabilitySource
            || !capabilitySource.Capabilities.IsDurable
            || recovery is null
            || !capabilitySource.Capabilities.SupportsRecovery
            || !capabilitySource.Capabilities.SupportsCleanup)
            throw new DurableKeyReadinessException(Codes[1]);
        await csprng.ValidateAsync(cancellationToken);
        if (!capabilitySource.Capabilities.IsKekQualified)
            throw new DurableKeyReadinessException(Codes[3]);
        var hashAclIsExact = await db.Database.SqlQueryRaw<bool>("""
            SELECT p.proowner=(SELECT oid FROM pg_catalog.pg_roles WHERE rolname='tagekyc_raw_export_deployer')
               AND NOT pg_catalog.has_function_privilege('public',p.oid,'EXECUTE')
               AND NOT pg_catalog.has_function_privilege('tagekyc_runtime',p.oid,'EXECUTE')
               AND NOT pg_catalog.has_function_privilege('tagekyc_raw_export_custody_encryptor',p.oid,'EXECUTE')
               AND NOT pg_catalog.has_function_privilege('tagekyc_raw_export_reconciler',p.oid,'EXECUTE')
               AND NOT pg_catalog.has_function_privilege('tagekyc_raw_export_lifecycle',p.oid,'EXECUTE')
               AS "Value"
            FROM pg_catalog.pg_proc p
            WHERE p.oid='tagekyc.raw_export_c1_hash_canonical(text,text[])'::regprocedure
            """).SingleOrDefaultAsync(cancellationToken);
        if (!hashAclIsExact) throw new DurableKeyReadinessException(Codes[4]);
        ValidateRetryConfiguration(options);
    }

    public static void ValidateRetryConfiguration(DurableKeyCustodyOptions candidate)
    {
        if (!candidate.IsValid)
            throw new DurableKeyReadinessException(Codes[5]);
    }
}
