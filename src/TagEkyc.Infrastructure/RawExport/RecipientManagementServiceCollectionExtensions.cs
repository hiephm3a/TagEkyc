using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TagEkyc.Application.Ports;
using TagEkyc.Application.RawExport;
using TagEkyc.Infrastructure.Auth;

namespace TagEkyc.Infrastructure.RawExport;

public static class RecipientManagementServiceCollectionExtensions
{
    public static IServiceCollection AddTagEkycRecipientManagement(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isProduction)
    {
        var options = RecipientManagementConnectionFactory.Resolve(configuration, isProduction);
        services.AddSingleton(options);
        services.TryAddScoped<IRecipientPublicKeyProfileValidator, RecipientPublicKeyProfileValidator>();
        services.TryAddSingleton<IManagedApiKeyGenerator, RandomManagedApiKeyGenerator>();
        services.TryAddScoped<RecipientManagementReadinessValidator>();

        if (options.Topology == RecipientManagementTopology.PostgresDurable
            && options.IsSyntacticallyValid)
        {
            services.TryAddScoped<IManagedCredentialMaterialGenerator, ManagedCredentialMaterialGenerator>();
            services.TryAddScoped<IRecipientManagementConnectionFactory, RecipientManagementConnectionFactory>();
            services.TryAddScoped<RecipientManagementRepository>();
            services.TryAddScoped<IRecipientManagementGateway>(provider =>
                provider.GetRequiredService<RecipientManagementRepository>());
        }
        else
        {
            services.TryAddSingleton<IRecipientManagementGateway, UnavailableRecipientManagementGateway>();
        }

        services.TryAddScoped<RecipientManagementApplicationService>();
        services.TryAddScoped<IRecipientManagementApplicationService>(provider =>
            provider.GetRequiredService<RecipientManagementApplicationService>());
        return services;
    }

    private sealed class ManagedCredentialMaterialGenerator(
        IManagedApiKeyGenerator generator,
        ApiKeyStorePepper pepper) : IManagedCredentialMaterialGenerator
    {
        public ManagedCredentialMaterial Generate()
        {
            for (var attempt = 0; attempt < 32; attempt++)
            {
                var material = generator.Generate();
                if (ManagedApiKeyParser.Parse(material.PresentedKey)?.Prefix != material.Prefix)
                    continue;
                return new(
                    Guid.NewGuid(), material.PresentedKey, material.Prefix,
                    ApiKeyHasher.Hash(pepper.Value, material.PresentedKey));
            }
            throw new InvalidOperationException(
                "C5_MANAGED_CREDENTIAL_GENERATOR_GRAMMAR_EXHAUSTED");
        }
    }

    private sealed class UnavailableRecipientManagementGateway : IRecipientManagementGateway
    {
        public Task<Application.VerificationSessions.SessionOperationResult<RecipientManagementWriteResult<Contracts.RawExport.ManagedRecipientIdentityDto>>> EnrollRecipientAsync(
            Application.AuthenticatedClientContext actor, Contracts.RawExport.EnrollManagedRecipientRequest request,
            string idempotencyKey, CancellationToken cancellationToken) => Unavailable<RecipientManagementWriteResult<Contracts.RawExport.ManagedRecipientIdentityDto>>();
        public Task<Application.VerificationSessions.SessionOperationResult<RecipientManagementWriteResult<Contracts.RawExport.ManagedCredentialDto>>> IssueCredentialAsync(
            Application.AuthenticatedClientContext actor, Contracts.RawExport.IssueManagedRecipientCredentialRequest request,
            string idempotencyKey, CancellationToken cancellationToken) => Unavailable<RecipientManagementWriteResult<Contracts.RawExport.ManagedCredentialDto>>();
        public Task<Application.VerificationSessions.SessionOperationResult<RecipientManagementWriteResult<Contracts.RawExport.ManagedCredentialDto>>> ReplaceCredentialAsync(
            Application.AuthenticatedClientContext actor, Contracts.RawExport.ReplaceManagedRecipientCredentialRequest request,
            string idempotencyKey, CancellationToken cancellationToken) => Unavailable<RecipientManagementWriteResult<Contracts.RawExport.ManagedCredentialDto>>();
        public Task<Application.VerificationSessions.SessionOperationResult<RecipientManagementWriteResult<Contracts.RawExport.ManagedCredentialDto>>> RevokeCredentialAsync(
            Application.AuthenticatedClientContext actor, Contracts.RawExport.RevokeManagedRecipientCredentialRequest request,
            string idempotencyKey, CancellationToken cancellationToken) => Unavailable<RecipientManagementWriteResult<Contracts.RawExport.ManagedCredentialDto>>();
        public Task<Application.VerificationSessions.SessionOperationResult<RecipientManagementWriteResult<Contracts.RawExport.RecipientPublicKeyOperationResultDto>>> EnrollKeyAsync(
            Application.AuthenticatedClientContext actor, Contracts.RawExport.EnrollRecipientPublicKeyRequest request,
            ValidatedRecipientPublicKey key, string idempotencyKey, CancellationToken cancellationToken) => Unavailable<RecipientManagementWriteResult<Contracts.RawExport.RecipientPublicKeyOperationResultDto>>();
        public Task<Application.VerificationSessions.SessionOperationResult<RecipientManagementWriteResult<Contracts.RawExport.RecipientPublicKeyOperationResultDto>>> RotateKeyAsync(
            Application.AuthenticatedClientContext actor, Contracts.RawExport.RotateRecipientPublicKeyRequest request,
            ValidatedRecipientPublicKey key, string idempotencyKey, CancellationToken cancellationToken) => Unavailable<RecipientManagementWriteResult<Contracts.RawExport.RecipientPublicKeyOperationResultDto>>();
        public Task<Application.VerificationSessions.SessionOperationResult<RecipientManagementWriteResult<Contracts.RawExport.RecipientPublicKeyOperationResultDto>>> RevokeKeyAsync(
            Application.AuthenticatedClientContext actor, Contracts.RawExport.RevokeRecipientPublicKeyRequest request,
            string idempotencyKey, CancellationToken cancellationToken) => Unavailable<RecipientManagementWriteResult<Contracts.RawExport.RecipientPublicKeyOperationResultDto>>();
        public Task<Application.VerificationSessions.SessionOperationResult<Contracts.RawExport.ManagedRecipientReadinessDto>> ReadReadinessAsync(
            Application.AuthenticatedClientContext actor, Guid recipientClientApplicationId,
            CancellationToken cancellationToken) => Unavailable<Contracts.RawExport.ManagedRecipientReadinessDto>();

        private static Task<Application.VerificationSessions.SessionOperationResult<T>> Unavailable<T>() => Task.FromResult(
            Application.VerificationSessions.SessionOperationResult<T>.Failure(
                Contracts.RawExport.RecipientManagementErrorCodes.Unavailable,
                "Recipient management is temporarily unavailable.", 503));
    }
}
