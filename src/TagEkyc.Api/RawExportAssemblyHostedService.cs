using TagEkyc.Application.Ports;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.Api;

// Product caller for the internal C1 assembly command. Disabled is the default;
// DurableWorker requires an explicitly supplied work source, role-scoped
// connections, authenticator and C2 provider. No fixture or provider fallback
// is selected by this host.
public sealed class RawExportAssemblyHostedService(
    IServiceScopeFactory scopes,
    RawExportAssemblyOptions options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (options.Topology != RawExportAssemblyTopology.DurableWorker)
            return;
        if (options.PollIntervalMilliseconds is not (>= 10 and <= 60_000))
            throw new InvalidOperationException(RawExportAssemblyOptions.ConfigInvalid);

        var delay = TimeSpan.FromMilliseconds(options.PollIntervalMilliseconds.Value);
        while (!stoppingToken.IsCancellationRequested)
        {
            await using var scope = scopes.CreateAsyncScope();
            var source = scope.ServiceProvider.GetRequiredService<IRawExportAssemblyWorkSource>();
            var orchestrator = scope.ServiceProvider.GetRequiredService<IRawExportAssemblyOrchestrator>();
            var request = await source.TryAcquireAsync(stoppingToken).ConfigureAwait(false);
            if (request is null)
            {
                await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
                continue;
            }

            var result = await orchestrator.ExecuteAsync(request, stoppingToken).ConfigureAwait(false);
            await source.RecordAsync(request, result, stoppingToken).ConfigureAwait(false);
        }
    }
}
