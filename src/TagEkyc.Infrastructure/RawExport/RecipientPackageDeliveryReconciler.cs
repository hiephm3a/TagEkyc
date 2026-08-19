using Microsoft.Extensions.DependencyInjection;

namespace TagEkyc.Infrastructure.RawExport;

public sealed class RecipientPackageDeliveryReconciler(
    IServiceScopeFactory scopes,
    RecipientPackageDeliveryOptions options)
{
    public async Task RunAsync(CancellationToken stoppingToken)
    {
        if (options.Topology != RecipientPackageDeliveryTopology.S3CompatibleDurable || !options.IsSyntacticallyValid)
            return;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var repository = scope.ServiceProvider.GetRequiredService<RecipientPackageDeliveryRepository>();
                var result = await repository.ReconcileNextAsync(stoppingToken).ConfigureAwait(false);
                if (result.Outcome == "None")
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch
            {
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken).ConfigureAwait(false);
            }
        }
    }
}
