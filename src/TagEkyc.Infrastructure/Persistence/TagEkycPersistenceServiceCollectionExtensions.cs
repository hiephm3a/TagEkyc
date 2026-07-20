using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TagEkyc.Application.Ports;

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
        services.AddScoped<IVerificationSessionRepository, EfVerificationSessionRepository>();
        services.AddScoped<ICaptureArtifactRepository, EfCaptureArtifactRepository>();
        services.AddScoped<IEvidenceResultRepository, EfEvidenceResultRepository>();
        services.AddScoped<IVerificationDecisionRepository, EfVerificationDecisionRepository>();
        services.AddScoped<IEvidencePackageRepository, EfEvidencePackageRepository>();
        services.AddScoped<IInternalEvidenceManifestRepository, EfEvidenceManifestRepository>();
        services.AddScoped<IAuditEventRepository, EfAuditEventRepository>();
        services.AddScoped<IRawExportPolicyRepository, EfRawExportPolicyRepository>();
        services.AddScoped<IRawExportControlPlaneRepository, EfRawExportControlPlaneRepository>();
        services.AddScoped<IRawExportSubjectConsentRepository, EfRawExportSubjectConsentRepository>();
        services.AddScoped<RawExportRuntimePrivilegeValidator>();
        services.AddScoped<RawExportControlPlaneReadinessValidator>();
        services.AddScoped<RawExportSubjectConsentReadinessValidator>();
        services.AddScoped<IVerificationFinalizationBoundary, EfVerificationFinalizationBoundary>();
        services.AddScoped<EfAppendIdempotencyBoundary>();
        services.AddScoped<IAppendIdempotencyRepository>(sp => sp.GetRequiredService<EfAppendIdempotencyBoundary>());
        services.AddScoped<IAppendIdempotencyBoundary>(sp => sp.GetRequiredService<EfAppendIdempotencyBoundary>());

        return services;
    }
}
