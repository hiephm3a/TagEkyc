using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TagEkyc.Contracts.RawExport;

namespace TagEkyc.Infrastructure.RawExport;

public static class DurableKeyCustodyServiceCollectionExtensions
{
    public static IServiceCollection AddTagEkycDurableKeyCustody(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        var topology = DurableKeyTopologyOptions.Resolve(configuration);
        services.TryAddSingleton(topology);
        services.TryAddSingleton(DurableKeyCustodyOptions.Resolve(configuration));
        if (topology.Topology == DurableKeyTopology.DurableKey)
        {
            services.TryAddScoped<CsprngReadinessValidator>();
            services.TryAddScoped<IDurableKeyReadinessValidator, CustodyRoleReadinessValidator>();
            services.TryAddScoped<IDurableKeyReadinessValidator, DurableKeyProviderReadinessValidator>();
            services.TryAddScoped<IDurableKeyReadinessValidator, KeyReservationReadinessValidator>();
            services.TryAddScoped<PostgresAttemptKeyReservationProvider>();
            services.TryAddScoped<IAttemptKeyReservationProvisioningOperation>(sp =>
                sp.GetRequiredService<PostgresAttemptKeyReservationProvider>());
            services.TryAddScoped<PostgresKeyProviderOperationMap>();
            services.TryAddScoped<AttemptKeyRecoveryContextReader>();
            services.TryAddScoped<AttemptAeadEncryptionOperationService>();
            services.TryAddScoped<AttemptAeadVerificationOperationService>();
            services.TryAddScoped<IAttemptAeadEncryptionOperation>(sp =>
                sp.GetRequiredService<AttemptAeadEncryptionOperationService>());
            services.TryAddScoped<IAttemptAeadVerificationOperation>(sp =>
                sp.GetRequiredService<AttemptAeadVerificationOperationService>());
        }
        return services;
    }
}
