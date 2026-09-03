using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TagEkyc.Application;
using TagEkyc.Application.Ports;
using TagEkyc.Application.RawExport;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.RawExport;

namespace TagEkyc.Infrastructure.RawExport;

public static class RecipientPackageDeliveryServiceCollectionExtensions
{
    public static IServiceCollection AddTagEkycRecipientPackageDelivery(
        this IServiceCollection services, IConfiguration configuration)
    {
        var options = RecipientPackageDeliveryOptions.Resolve(configuration);
        services.TryAddSingleton(options);
        services.TryAddScoped<RecipientPackageDeliveryReadinessValidator>();
        if (options.Topology != RecipientPackageDeliveryTopology.S3CompatibleDurable || !options.IsSyntacticallyValid)
        {
            services.TryAddSingleton<IRecipientPackageDeliveryGateway, UnavailableRecipientPackageDeliveryGateway>();
        }
        else
        {
            services.TryAddScoped<IRecipientPackageDeliveryConnectionFactory, RecipientPackageDeliveryConnectionFactory>();
            services.TryAddScoped<RecipientPackageDeliveryRepository>();
            services.TryAddScoped<RecipientPackageDeliveryObjectClientFactory>();
            services.TryAddScoped<S3CompatibleRecipientPackageDeliveryReader>();
            services.TryAddScoped<IRecipientPackageDeliveryReader>(p => p.GetRequiredService<S3CompatibleRecipientPackageDeliveryReader>());
            services.TryAddSingleton<RecipientPackageDeliverySpoolPool>();
            services.TryAddScoped<RecipientPackageDeliveryCoordinator>();
            services.TryAddScoped<IRecipientPackageDeliveryGateway>(p => p.GetRequiredService<RecipientPackageDeliveryCoordinator>());
            services.TryAddSingleton<RecipientPackageDeliveryReconciler>();
        }
        services.TryAddScoped<RecipientPackageDeliveryApplicationService>();
        services.TryAddScoped<IRecipientPackageDeliveryApplicationService>(p => p.GetRequiredService<RecipientPackageDeliveryApplicationService>());
        return services;
    }

    private sealed class UnavailableRecipientPackageDeliveryGateway : IRecipientPackageDeliveryGateway
    {
        public Task<SessionOperationResult<RecipientPackageDeliveryCreation>> CreateAsync(
            AuthenticatedClientContext actor, Guid packageId, string idempotencyKey,
            byte[] correlationDigest, CancellationToken cancellationToken) =>
            Task.FromResult(Unavailable<RecipientPackageDeliveryCreation>());

        public Task<SessionOperationResult<RecipientPackageDeliveryDto>> ReadAsync(
            AuthenticatedClientContext actor, Guid deliveryId, CancellationToken cancellationToken) =>
            Task.FromResult(Unavailable<RecipientPackageDeliveryDto>());

        public Task<SessionOperationResult<RecipientPackageDeliveryContentLease>> PrepareContentAsync(
            AuthenticatedClientContext actor, Guid deliveryId, byte[] correlationDigest,
            CancellationToken cancellationToken) =>
            Task.FromResult(Unavailable<RecipientPackageDeliveryContentLease>());

        private static SessionOperationResult<T> Unavailable<T>() =>
            SessionOperationResult<T>.Failure(
                RecipientPackageDeliveryErrorCodes.Unavailable,
                "Package delivery is temporarily unavailable.",
                503);
    }
}
