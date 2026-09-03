using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Infrastructure.ProtectedValues;

namespace TagEkyc.Infrastructure.RawExport;

public static class SubjectRefTokenServiceCollectionExtensions
{
    public static IServiceCollection AddTagEkycSubjectRefToken(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.TryAddSingleton(configuration);
        services.TryAddSingleton<ConfigurationProtectedValueProvider>();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<
                IProtectedValueProvider,
                ConfigurationProtectedValueProvider>());
        services.TryAddSingleton(new ProtectedValueResolverOptions());
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton(serviceProvider =>
            ProtectedValueProviderRegistry.CreateTerminal(
                serviceProvider.GetServices<IProtectedValueProvider>()));
        services.TryAddTransient<ISubjectRefTokenService>(
            serviceProvider =>
            {
                var resolver = new ProtectedValueResolver(
                    new FixtureSubjectTokenCatalog(),
                    serviceProvider.GetRequiredService<
                        ProtectedValueProviderRegistry>(),
                    serviceProvider.GetRequiredService<
                        ProtectedValueResolverOptions>(),
                    serviceProvider.GetRequiredService<TimeProvider>());
                return new InProcessSubjectRefTokenService(resolver);
            });

        return services;
    }
}
