using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TagEkyc.Application.Ports;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.Infrastructure.Persistence;

public static class TagEkycPersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddTagEkycPostgresPersistence(
        this IServiceCollection services,
        string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Postgres persistence requires a non-empty connection string.");
        }

        services.AddDbContext<TagEkycDbContext>(options => options.UseNpgsql(connectionString));
        services.TryAddSingleton(RawExportJobLeaseOptions.Resolve(null));
        services.AddScoped<IVerificationSessionRepository, EfVerificationSessionRepository>();
        services.AddScoped<ICaptureArtifactRepository, EfCaptureArtifactRepository>();
        services.AddScoped<IEvidenceResultRepository, EfEvidenceResultRepository>();
        services.AddScoped<IVerificationDecisionRepository, EfVerificationDecisionRepository>();
        services.AddScoped<IEvidencePackageRepository, EfEvidencePackageRepository>();
        services.AddScoped<IInternalEvidenceManifestRepository, EfEvidenceManifestRepository>();
        services.AddScoped<IAuditEventRepository, EfAuditEventRepository>();
        services.AddScoped<IRawExportPolicyRepository, EfRawExportPolicyRepository>();
        services.AddScoped<IRawExportAuthorizationProjectionReader, EfRawExportAuthorizationProjectionReader>();
        services.AddScoped<IRawExportControlPlaneRepository, EfRawExportControlPlaneRepository>();
        services.AddScoped<IRawExportSubjectConsentRepository, EfRawExportSubjectConsentRepository>();
        services.AddScoped<IRawExportAuthorizationRepository, EfRawExportAuthorizationRepository>();
        services.AddScoped<IRawExportJobRepository, EfRawExportJobRepository>();
        services.AddScoped<RawExportRuntimePrivilegeValidator>();
        services.AddScoped<RawExportControlPlaneReadinessValidator>();
        services.AddScoped<RawExportSubjectConsentReadinessValidator>();
        services.AddScoped<RawExportJobReadinessValidator>();
        services.AddScoped<IVerificationFinalizationBoundary, EfVerificationFinalizationBoundary>();
        services.AddScoped<EfAppendIdempotencyBoundary>();
        services.AddScoped<IAppendIdempotencyRepository>(sp => sp.GetRequiredService<EfAppendIdempotencyBoundary>());
        services.AddScoped<IAppendIdempotencyBoundary>(sp => sp.GetRequiredService<EfAppendIdempotencyBoundary>());

        return services;
    }
}
