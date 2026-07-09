using TagEkyc.Infrastructure.Auth;

namespace TagEkyc.Api;

internal sealed class ApiKeyStoreProductionReadinessHostedService(IServiceScopeFactory scopeFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ApiKeyStoreProductionReadinessValidator>()
            .ValidateAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
