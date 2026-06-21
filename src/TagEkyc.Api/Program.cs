using System.Reflection;
using System.Text.Json.Serialization;
using TagEkyc.Api;
using TagEkyc.Api.LocalDev;
using TagEkyc.Application.LocalDev;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Infrastructure.Persistence;
using ApplicationMarker = TagEkyc.Application.AssemblyMarker;
using TagEkyc.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddSingleton<LocalDevRuntimePolicySource>();
builder.Services.AddSingleton<ILocalDevClientPolicyProvider>(sp => sp.GetRequiredService<LocalDevRuntimePolicySource>());
builder.Services.AddSingleton<LocalDevApiKeyValidator>();
builder.Services.AddSingleton<ILocalDevApiKeyAuthenticator, LocalDevApiKeyAuthenticator>();
builder.Services.AddSingleton<LocalDevInMemoryMetadataReferenceRegistry>();
builder.Services.AddSingleton<IMetadataReferenceRegistry>(sp => sp.GetRequiredService<LocalDevInMemoryMetadataReferenceRegistry>());
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
    builder.Services.AddSingleton<LocalDevInMemoryVerificationFinalizationBoundary>();
    builder.Services.AddSingleton<IVerificationFinalizationBoundary>(sp => sp.GetRequiredService<LocalDevInMemoryVerificationFinalizationBoundary>());
}

public partial class Program;
