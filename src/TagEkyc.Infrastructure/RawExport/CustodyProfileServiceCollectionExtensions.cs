using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace TagEkyc.Infrastructure.RawExport;

public static class CustodyProfileServiceCollectionExtensions
{
    public static IServiceCollection AddTagEkycCustodyProfiles(
        this IServiceCollection services,
        IConfiguration configuration) =>
        services.AddTagEkycCustodyProfiles(configuration, isProduction: false);

    public static IServiceCollection AddTagEkycCustodyProfiles(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isProduction)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.TryAddSingleton(configuration);
        services.TryAddSingleton(
            CustodyTimeBoundsState.Resolve(configuration));

        // Production keeps the time-bound state so readiness can report the
        // configured posture, but fixture catalogs are never part of its graph.
        // RawExportCustodyProfileReadinessValidator remains the authority for
        // rejecting a configured Fixture profile without preventing the host,
        // health endpoint, or site-qualification surfaces from starting.
        if (isProduction)
        {
            if (string.Equals(
                    RawExportCustodyProfileState.Resolve(configuration, isProduction: true).Profile,
                    "OpenBao",
                    StringComparison.Ordinal))
            {
                services.TryAddSingleton(sp => OpenBaoKekOptions.Resolve(configuration));
                services.TryAddSingleton<ICustodyProfileProvider,
                    OpenBaoProductionCustodyProfileProvider>();
            }
            return services;
        }

        services.TryAddSingleton<
            FixtureSourceEncryptionProfileCatalog>();
        services.TryAddSingleton<FixtureKekReferenceCatalog>();
        services.TryAddSingleton<
            ICustodyProfileProvider,
            FixtureCustodyProfileProvider>();

        return services;
    }
}
