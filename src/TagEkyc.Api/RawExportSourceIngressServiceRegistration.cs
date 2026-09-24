using TagEkyc.Application.Ports;
using TagEkyc.Application.CaptureRuntime;
using TagEkyc.Application.RawExport;
using TagEkyc.Infrastructure.Persistence;
using TagEkyc.Infrastructure.CaptureRuntime;
using TagEkyc.Infrastructure.RawExport;
using Microsoft.Extensions.Hosting;
using TagEkyc.Application.VerificationSessions;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace TagEkyc.Api;

public static class RawExportSourceIngressServiceRegistration
{
    // Prepared does not require the unfinished C6B broker/body pipeline. Keep
    // their real constructors lazy; never substitute a fake business provider.
    public static IServiceCollection AddPreparedRawExportSourceIngressServices(this IServiceCollection services)
    {
        services.TryAddSingleton<RawSourceRetentionProfileValidator>(sp =>
            new(sp.GetRequiredService<IConfiguration>()));
        services.TryAddSingleton<IRawSourceRetentionProfileProvider>(sp =>
            sp.GetRequiredService<RawSourceRetentionProfileValidator>());
        services.AddScoped<IRawSourceRetentionGateway>(sp =>
            new RawSourceRetentionGateway(sp.GetRequiredService<ICaptureRuntimeDbContextFactory>()));
        services.AddScoped<IRawSourceConsentService, RawSourceConsentApplicationService>();
        services.TryAddSingleton<ConfiguredRawExportCaptureAcceptancePolicyProvider>(sp =>
            new(sp.GetRequiredService<IConfiguration>()));
        services.TryAddSingleton<IRawExportCaptureAcceptancePolicyProvider>(sp =>
            sp.GetRequiredService<ConfiguredRawExportCaptureAcceptancePolicyProvider>());
        services.AddScoped<IRawExportCaptureAcceptanceWriter>(sp =>
            new EfRawExportCaptureAcceptanceWriter(sp.GetRequiredService<TagEkycDbContext>()));
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

    // This host-only lifecycle is independent of Prepared business services.
    // It resolves the worker only after the one durable startup selection says
    // Activated, so Prepared never constructs unfinished A3/provider graphs.
    public static IServiceCollection AddCaptureRuntimeA3HostLifecycle(this IServiceCollection services)
    {
        services.AddSingleton<CaptureRuntimeRouteSelectionState>();
        services.AddSingleton<ICaptureRuntimeHostLifetime, CaptureRuntimeHostLifetime>();
        services.AddHostedService<CaptureRuntimeA3HostedService>();
        return services;
    }
}

internal sealed class CaptureRuntimeRouteSelectionState
{
    private CaptureRuntimeRouteSelection? selection;
    internal CaptureRuntimeRouteSelection? Selection => Volatile.Read(ref selection);
    internal void Set(CaptureRuntimeRouteSelection value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (Interlocked.CompareExchange(ref selection, value, null) is not null)
            throw new InvalidOperationException("CAPTURE_RUNTIME_ROUTE_SELECTION_ALREADY_SET");
    }
}

internal sealed class CaptureRuntimeHostLifetime(IHostApplicationLifetime lifetime) : ICaptureRuntimeHostLifetime
{
    public CancellationToken Stopping => lifetime.ApplicationStopping;
}

internal sealed class CaptureRuntimeA3HostedService(
    CaptureRuntimeRouteSelectionState routes, IServiceProvider services) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var selection = routes.Selection ?? throw new InvalidOperationException("CAPTURE_RUNTIME_ROUTE_SELECTION_MISSING");
        if (selection.State == CaptureRuntimeRouteState.Prepared) return;
        var worker = services.GetService<ICaptureRuntimeA3Worker>()
            ?? throw new InvalidOperationException("CAPTURE_RUNTIME_A3_WORKER_NOT_READY");
        await worker.RunAsync(stoppingToken).ConfigureAwait(false);
    }
}
