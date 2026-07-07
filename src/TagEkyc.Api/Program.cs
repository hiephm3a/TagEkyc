using System.Reflection;
using System.Text.Json.Serialization;
using TagEkyc.Api;
using TagEkyc.Api.LocalDev;
using TagEkyc.Application.LocalDev;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Infrastructure.Persistence;
using TagEkyc.Infrastructure.Signing;
using ApplicationMarker = TagEkyc.Application.AssemblyMarker;
using TagEkyc.Contracts;
using TagEkyc.Contracts.Common;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Insert(0, new VerificationProfileDtoJsonConverter());
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddSingleton<LocalDevRuntimePolicySource>();
builder.Services.AddSingleton<ILocalDevClientPolicyProvider>(sp => sp.GetRequiredService<LocalDevRuntimePolicySource>());
builder.Services.AddSingleton<LocalDevApiKeyValidator>();
builder.Services.AddSingleton<ILocalDevApiKeyAuthenticator, LocalDevApiKeyAuthenticator>();
builder.Services.AddSingleton<LocalDevInMemoryMetadataReferenceRegistry>();
builder.Services.AddSingleton<IMetadataReferenceRegistry>(sp => sp.GetRequiredService<LocalDevInMemoryMetadataReferenceRegistry>());
ConfigureEvidenceSigning(builder);
ConfigurePersistence(builder);
builder.Services.AddScoped<VerificationSessionApplicationService>();
builder.Services.AddScoped<IVerificationSessionCommands>(sp => sp.GetRequiredService<VerificationSessionApplicationService>());
builder.Services.AddScoped<IVerificationSessionQueries>(sp => sp.GetRequiredService<VerificationSessionApplicationService>());
builder.Services.AddScoped<VerificationEvidenceApplicationService>();
builder.Services.AddScoped<ICaptureArtifactCommands>(sp => sp.GetRequiredService<VerificationEvidenceApplicationService>());
builder.Services.AddScoped<ITrustedEvidenceResultCommands>(sp => sp.GetRequiredService<VerificationEvidenceApplicationService>());
builder.Services.AddScoped<VerificationCompletionApplicationService>();
builder.Services.AddScoped<IVerificationSessionCompletionCommands>(sp => sp.GetRequiredService<VerificationCompletionApplicationService>());
builder.Services.AddScoped<IEvidencePackageQueries>(sp => sp.GetRequiredService<VerificationCompletionApplicationService>());
builder.Services.AddScoped<ICompletionNotificationQueries>(sp => sp.GetRequiredService<VerificationCompletionApplicationService>());

var app = builder.Build();

app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    service = "TagEkyc.Api",
}));

app.MapGet("/build", () => Results.Ok(new
{
    service = "TagEkyc.Api",
    version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0",
    environment = app.Environment.EnvironmentName,
    applicationAssembly = typeof(ApplicationMarker).Assembly.GetName().Name,
}));

app.MapGet("/.well-known/jwks.json", (HttpContext httpContext, IEs256JwksProvider jwksProvider) =>
{
    httpContext.Response.Headers.CacheControl = "no-store";
    return Results.Ok(jwksProvider.GetJwks());
});

app.MapGet("/", () => Results.Ok(new SessionStatusPlaceholder(
    "skeleton",
    "STANDARD_EKYC_PROFILE",
    "CREATED",
    "NOT_AVAILABLE")));

app.MapVerificationSessionEndpoints();

app.Run();

static void ConfigurePersistence(WebApplicationBuilder builder)
{
    var options = builder.Configuration
        .GetSection(TagEkycPersistenceOptions.SectionName)
        .Get<TagEkycPersistenceOptions>() ?? new TagEkycPersistenceOptions();

    if (builder.Environment.IsProduction() && !options.IsPostgres)
    {
        throw new InvalidOperationException("Production requires TagEkyc:Persistence:Provider=Postgres.");
    }

    if (options.IsPostgres)
    {
        builder.Services.AddTagEkycPostgresPersistence(options.ConnectionString ?? string.Empty);
        return;
    }

    if (!options.IsInMemory)
    {
        throw new InvalidOperationException($"Unsupported TagEkyc persistence provider '{options.Provider}'.");
    }

    builder.Services.AddSingleton<LocalDevInMemoryVerificationSessionRepository>();
    builder.Services.AddSingleton<IVerificationSessionRepository>(sp => sp.GetRequiredService<LocalDevInMemoryVerificationSessionRepository>());
    builder.Services.AddSingleton<LocalDevInMemoryAuditEventRepository>();
    builder.Services.AddSingleton<IAuditEventRepository>(sp => sp.GetRequiredService<LocalDevInMemoryAuditEventRepository>());
    builder.Services.AddSingleton<LocalDevInMemoryCaptureArtifactRepository>();
    builder.Services.AddSingleton<ICaptureArtifactRepository>(sp => sp.GetRequiredService<LocalDevInMemoryCaptureArtifactRepository>());
    builder.Services.AddSingleton<LocalDevInMemoryEvidenceResultRepository>();
    builder.Services.AddSingleton<IEvidenceResultRepository>(sp => sp.GetRequiredService<LocalDevInMemoryEvidenceResultRepository>());
    builder.Services.AddSingleton<LocalDevInMemoryVerificationDecisionRepository>();
    builder.Services.AddSingleton<IVerificationDecisionRepository>(sp => sp.GetRequiredService<LocalDevInMemoryVerificationDecisionRepository>());
    builder.Services.AddSingleton<LocalDevInMemoryEvidencePackageRepository>();
    builder.Services.AddSingleton<IEvidencePackageRepository>(sp => sp.GetRequiredService<LocalDevInMemoryEvidencePackageRepository>());
    builder.Services.AddSingleton<LocalDevInMemoryEvidenceManifestRepository>();
    builder.Services.AddSingleton<IInternalEvidenceManifestRepository>(sp => sp.GetRequiredService<LocalDevInMemoryEvidenceManifestRepository>());
    builder.Services.AddSingleton<LocalDevInMemoryAppendIdempotencyStore>();
    builder.Services.AddSingleton<IAppendIdempotencyRepository>(sp => sp.GetRequiredService<LocalDevInMemoryAppendIdempotencyStore>());
    builder.Services.AddSingleton<IAppendIdempotencyBoundary>(sp => sp.GetRequiredService<LocalDevInMemoryAppendIdempotencyStore>());
    builder.Services.AddSingleton<LocalDevInMemoryVerificationFinalizationBoundary>();
    builder.Services.AddSingleton<IVerificationFinalizationBoundary>(sp => sp.GetRequiredService<LocalDevInMemoryVerificationFinalizationBoundary>());
}

static void ConfigureEvidenceSigning(WebApplicationBuilder builder)
{
    ValidateProductionTrialP12Configuration(builder);

    builder.Services
        .AddOptions<EvidenceSigningOptions>()
        .Bind(builder.Configuration.GetSection(EvidenceSigningOptions.SectionName))
        .Validate(options => ValidateEvidenceSigningBackend(options, builder.Environment.IsProduction()), "Invalid evidence signing backend configuration.")
        .ValidateOnStart();

    builder.Services
        .AddOptions<LocalDevEs256JwsEvidenceSignerOptions>()
        .Bind(builder.Configuration.GetSection(LocalDevEs256JwsEvidenceSignerOptions.SectionName));

    builder.Services
        .AddOptions<ProductionTrialP12Es256JwsEvidenceSignerOptions>()
        .Bind(builder.Configuration.GetSection(EvidenceSigningOptions.SectionName));

    builder.Services
        .AddOptions<Pkcs11Es256JwsEvidenceSignerOptions>()
        .Bind(builder.Configuration.GetSection(Pkcs11Es256JwsEvidenceSignerOptions.SectionName));

    builder.Services.AddSingleton<IEvidenceSigner>(sp =>
    {
        var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<EvidenceSigningOptions>>().Value;
        var backend = string.IsNullOrWhiteSpace(options.Backend)
            ? EvidenceSigningBackends.LocalDev
            : options.Backend;

        return backend switch
        {
            EvidenceSigningBackends.LocalDev => new LocalDevEs256JwsEvidenceSigner(
                sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<LocalDevEs256JwsEvidenceSignerOptions>>().Value),
            EvidenceSigningBackends.ProductionTrialP12 => new ProductionTrialP12Es256JwsEvidenceSigner(
                sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ProductionTrialP12Es256JwsEvidenceSignerOptions>>().Value),
            EvidenceSigningBackends.Pkcs11 => new Pkcs11Es256JwsEvidenceSigner(
                sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Pkcs11Es256JwsEvidenceSignerOptions>>().Value),
            _ => throw new InvalidOperationException("Unsupported evidence signing backend."),
        };
    });
    builder.Services.AddSingleton<IEs256PublicJwkSource>(sp =>
        sp.GetRequiredService<IEvidenceSigner>() as IEs256PublicJwkSource
        ?? throw new InvalidOperationException("Evidence signing backend does not expose a public ES256 JWK."));
    builder.Services.AddSingleton(sp =>
        builder.Configuration.GetSection($"{EvidenceSigningOptions.SectionName}:Jwks").Get<Es256JwksOptions>()
        ?? new Es256JwksOptions());
    builder.Services.AddSingleton<IEs256JwksProvider, Es256JwksProvider>();
    builder.Services.AddHostedService<EvidenceSignerStartupValidationHostedService>();
}

static bool ValidateEvidenceSigningBackend(EvidenceSigningOptions options, bool isProduction)
{
    if (!string.IsNullOrWhiteSpace(options.Backend) &&
        options.Backend is not EvidenceSigningBackends.LocalDev and not EvidenceSigningBackends.ProductionTrialP12 and not EvidenceSigningBackends.Pkcs11)
    {
        return false;
    }

    if (options.RequireHardwareSigner &&
        !string.Equals(options.Backend, EvidenceSigningBackends.Pkcs11, StringComparison.Ordinal))
    {
        return false;
    }

    if (isProduction &&
        options.Backend is not EvidenceSigningBackends.ProductionTrialP12 and not EvidenceSigningBackends.Pkcs11)
    {
        return false;
    }

    return true;
}

static void ValidateProductionTrialP12Configuration(WebApplicationBuilder builder)
{
    var backend = builder.Configuration[$"{EvidenceSigningOptions.SectionName}:Backend"];
    if (builder.Environment.IsProduction() &&
        string.Equals(backend, EvidenceSigningBackends.LocalDev, StringComparison.Ordinal))
    {
        throw new InvalidOperationException("PROD_SIGNING_LOCALDEV_BACKEND_FORBIDDEN");
    }

    if (string.Equals(backend, EvidenceSigningBackends.ProductionTrialP12, StringComparison.Ordinal) &&
        !string.IsNullOrWhiteSpace(builder.Configuration[$"{EvidenceSigningOptions.SectionName}:P12Password"]))
    {
        throw new InvalidOperationException("PROD_SIGNING_P12_PASSWORD_PLAINTEXT_FORBIDDEN");
    }
}

public partial class Program;
