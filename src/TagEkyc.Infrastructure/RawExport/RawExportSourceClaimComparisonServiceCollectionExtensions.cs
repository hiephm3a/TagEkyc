using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TagEkyc.Contracts.RawExport;

namespace TagEkyc.Infrastructure.RawExport;

public static class RawExportSourceClaimComparisonServiceCollectionExtensions
{
    public static IServiceCollection AddTagEkycRawExportSourceClaimComparison(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddTagEkycContentCommitment(configuration);
        services.AddTagEkycSubjectRefToken(configuration);
        services.AddTagEkycCustodyProfiles(configuration);
        services.TryAddScoped<
            IRawExportSourceClaimComparisonBroker,
            RawExportSourceClaimComparisonBroker>();
        return services;
    }
}
