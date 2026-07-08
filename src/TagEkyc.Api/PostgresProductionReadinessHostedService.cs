using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.Api;

internal sealed class PostgresProductionReadinessHostedService(IServiceScopeFactory scopeFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<PostgresProductionReadinessValidator>()
            .ValidateAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
