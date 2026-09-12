using TagEkyc.Application.Ports;
using TagEkyc.Application.RawExport;
using TagEkyc.Infrastructure.Persistence;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.Api;

public static class RawExportSourceIngressServiceRegistration
{
    // Prepared does not require the unfinished C6B broker/body pipeline. Keep
    // their real constructors lazy; never substitute a fake business provider.
    public static IServiceCollection AddPreparedRawExportSourceIngressServices(this IServiceCollection services)
    {
        services.AddScoped(sp => new RawExportSourceIngressApplicationService(
            sp.GetRequiredService<IRawExportIngressCapacity>(),
            sp.GetRequiredService<ICaptureAgentConfigurationProvider>(),
            sp.GetRequiredService<IRawExportCaptureAcceptanceResolver>(),
            sp.GetRequiredService<IRawExportSourceIngressBroker>(),
            sp.GetRequiredService<IRawExportSourceBodyPipeline>()));
        services.AddScoped<IRawExportCaptureAcceptanceResolver>(sp =>
            new RawExportCaptureAcceptanceResolver(sp.GetRequiredService<TagEkycDbContext>()));
        return services;
    }
}
