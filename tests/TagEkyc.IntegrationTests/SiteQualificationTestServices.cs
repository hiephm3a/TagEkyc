using Microsoft.Extensions.DependencyInjection;
using TagEkyc.Api;
using TagEkyc.Application.CaptureRuntime;

namespace TagEkyc.IntegrationTests;

internal static class SiteQualificationTestServices
{
    public static IServiceCollection AddCurrentSiteQualificationForRawIngressTests(
        this IServiceCollection services)
    {
        services.AddSingleton<ICaptureRuntimeActivationEvidenceSealProvider>(
            new ActivationEvidenceTestSeals.Provider(ActivationEvidenceTestSeals.Valid(0)));
        services.AddSingleton<ICaptureRuntimeSiteTransportQualificationProvider>(
            new ActivationEvidenceTestSeals.QualificationProvider());
        services.AddSingleton<ICaptureRuntimeSiteTransportQualificationSettingsProvider>(
            new ActivationEvidenceTestSeals.QualificationSettingsProvider());
        services.AddSingleton<ISiteRawIngressTransportQualificationRuntimeGate,
            SiteRawIngressTransportQualificationRuntimeGate>();
        return services;
    }
}
