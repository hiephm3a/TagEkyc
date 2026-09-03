using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace TagEkyc.Infrastructure.RawExport;

public static class ProvisionalObjectCustodyServiceCollectionExtensions
{
    public static IServiceCollection AddTagEkycProvisionalObjectCustody(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = ProvisionalObjectCustodyOptions.Resolve(configuration);
        services.TryAddSingleton(options);
        if (options.TopologyConfigurationValid
            && options.LimitsConfigurationValid
            && options.EndpointConfigurationValid
            && options.CredentialConfigurationValid
            && options.Topology == ProvisionalObjectTopology.Disabled)
            return services;
        services.TryAddScoped<ProvisionalObjectCustodyReadinessValidator>();
        if (!options.IsSyntacticallyValid
            || options.Topology != ProvisionalObjectTopology.S3CompatibleDurable
            || options.Capability is null)
            return services;
        services.TryAddScoped<ProvisionalObjectCustodyRepository>();
        switch (options.Capability)
        {
            case ProvisionalObjectCapability.Writer:
                services.TryAddScoped<IProvisionalObjectWriter, S3CompatibleProvisionalObjectWriter>();
                break;
            case ProvisionalObjectCapability.Reconciler:
                services.TryAddScoped<IProvisionalObjectReconciler, S3CompatibleProvisionalObjectReconciler>();
                break;
            case ProvisionalObjectCapability.Lifecycle:
                services.TryAddScoped<IProvisionalObjectLifecycle, S3CompatibleProvisionalObjectLifecycle>();
                break;
            case ProvisionalObjectCapability.PostureProbe:
                services.TryAddScoped<IProvisionalObjectPostureProbe, S3CompatibleProvisionalObjectPostureProbe>();
                break;
        }
        return services;
    }
}
