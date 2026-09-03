using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TagEkyc.Contracts.RawExport;

namespace TagEkyc.Infrastructure.RawExport;

public static class AttemptKeyServiceCollectionExtensions
{
    public static IServiceCollection AddTagEkycAttemptKeyProvider(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var state = RawExportAttemptKeyProfileState.Resolve(
            configuration,
            isProduction: false);
        if (string.Equals(
                state.Profile,
                "Fixture",
                StringComparison.Ordinal))
        {
            services.TryAddSingleton<FixtureAttemptKekCatalog>();
            services.TryAddSingleton<FixtureWrappedAttemptKeyStore>();
            services.TryAddSingleton<
                IAttemptKeyProvider,
                FixtureAttemptKeyProvider>();
        }

        return services;
    }
}
