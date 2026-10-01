using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Infrastructure.ProtectedValues;

namespace TagEkyc.Infrastructure.RawExport;

public static class RawExportSourceClaimComparisonServiceCollectionExtensions
{
    public static IServiceCollection AddTagEkycRawExportSourceClaimComparison(
        this IServiceCollection services,
        IConfiguration configuration) =>
        services.AddTagEkycRawExportSourceClaimComparison(
            configuration, isProduction: false);

    public static IServiceCollection AddTagEkycRawExportSourceClaimComparison(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isProduction)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        if (isProduction)
        {
            services.AddTagEkycProductionRawExportClaimProviders(
                configuration);
            services.AddTagEkycCustodyProfiles(configuration, isProduction: true);
            RawExportClaimProviderProductionGuard.RequireQualified(services);
        }
        else
        {
            services.AddTagEkycContentCommitment(configuration);
            services.AddTagEkycSubjectRefToken(configuration);
            services.AddTagEkycCustodyProfiles(configuration);
        }
        services.TryAddScoped<
            IRawExportSourceClaimComparisonBroker,
            RawExportSourceClaimComparisonBroker>();
        return services;
    }
}

internal static class RawExportClaimProviderProductionGuard
{
    internal const string ProvidersMissing =
        "PROD_RAW_EXPORT_CLAIM_PROVIDERS_MISSING";
    internal const string FixtureContentCommitment =
        "PROD_RAW_EXPORT_CONTENT_COMMITMENT_FIXTURE_ACTIVE";
    internal const string FixtureSubjectRefToken =
        "PROD_RAW_EXPORT_SUBJECT_REF_TOKEN_FIXTURE_ACTIVE";

    internal static void RequireQualified(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (services.Any(descriptor =>
                descriptor.ImplementationType == typeof(FixtureContentCommitmentCatalog)
                || descriptor.ImplementationType == typeof(InProcessContentCommitmentService)))
            throw new InvalidOperationException(FixtureContentCommitment);

        if (services.Any(descriptor =>
                descriptor.ServiceType == typeof(FixtureSubjectTokenCatalog)))
            throw new InvalidOperationException(FixtureSubjectRefToken);

        if (services.Any(descriptor =>
                descriptor.ImplementationType == typeof(FixtureCustodyProfileProvider)
                || descriptor.ServiceType == typeof(FixtureSourceEncryptionProfileCatalog)
                || descriptor.ServiceType == typeof(FixtureKekReferenceCatalog)))
            throw new InvalidOperationException(
                RawExportCustodyProfileReadinessValidator.FixtureActive);

        if (!HasExactSingleton<IContentCommitmentService,
                OpenBaoContentCommitmentService>(services)
            || !HasExactSingleton<ISubjectRefTokenService,
                OpenBaoSubjectRefTokenService>(services)
            || !services.Any(descriptor =>
                descriptor.ServiceType == typeof(RawExportClaimProviderReadinessValidator)))
            throw new InvalidOperationException(ProvidersMissing);

        if (!services.Any(descriptor =>
                descriptor.ServiceType == typeof(ICustodyProfileProvider)
                && descriptor.ImplementationType == typeof(OpenBaoProductionCustodyProfileProvider)))
            throw new InvalidOperationException(
                RawExportCustodyProfileReadinessValidator.ProfileMissing);
    }

    private static bool HasExactSingleton<TService, TImplementation>(
        IServiceCollection services) where TImplementation : class, TService
    {
        var registrations = services
            .Where(descriptor => descriptor.ServiceType == typeof(TService))
            .ToArray();
        return registrations.Length == 1
               && registrations[0].Lifetime == ServiceLifetime.Singleton
               && registrations[0].ImplementationType == typeof(TImplementation);
    }
}
