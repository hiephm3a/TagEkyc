using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Infrastructure.ProtectedValues;

namespace TagEkyc.Infrastructure.RawExport;

public static class ContentCommitmentServiceCollectionExtensions
{
    public static IServiceCollection AddTagEkycContentCommitment(
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
        services.TryAddSingleton<
            IProtectedValueCatalog,
            FixtureContentCommitmentCatalog>();
        services.TryAddSingleton(new ProtectedValueResolverOptions());
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton(serviceProvider =>
            ProtectedValueProviderRegistry.CreateTerminal(
                serviceProvider.GetServices<IProtectedValueProvider>()));
        services.TryAddTransient<
            IProtectedValueResolver,
            ProtectedValueResolver>();
        services.TryAddTransient<
            IContentCommitmentService,
            InProcessContentCommitmentService>();

        return services;
    }
}
