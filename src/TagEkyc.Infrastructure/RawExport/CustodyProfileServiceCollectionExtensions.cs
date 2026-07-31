using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace TagEkyc.Infrastructure.RawExport;

public static class CustodyProfileServiceCollectionExtensions
{
    public static IServiceCollection AddTagEkycCustodyProfiles(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.TryAddSingleton(
            CustodyTimeBoundsState.Resolve(configuration));
        services.TryAddSingleton<
            FixtureSourceEncryptionProfileCatalog>();
        services.TryAddSingleton<FixtureKekReferenceCatalog>();
        services.TryAddSingleton<
            ICustodyProfileProvider,
            FixtureCustodyProfileProvider>();

        return services;
    }
}
