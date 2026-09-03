using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using TagEkyc.Api;
using TagEkyc.Application.Ports;
using TagEkyc.Domain;
using TagEkyc.Infrastructure.Auth;
using TagEkyc.Infrastructure.Persistence;
using TagEkyc.Infrastructure.RawExport;
using TagEkyc.Infrastructure.Signing;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88AE2PolicyPermitTtlConfigTests(PostgresPersistenceFixture postgres)
{
    [Theory]
    [InlineData(null, null, true, 60, 900)]
    [InlineData("60", "900", true, 60, 900)]
    [InlineData("1", "3600", true, 1, 3600)]
    [InlineData("60", null, false, 0, 0)]
    [InlineData(null, "900", false, 0, 0)]
    [InlineData("abc", "900", false, 0, 0)]
    [InlineData("60", "abc", false, 0, 0)]
    [InlineData("0", "900", false, 0, 0)]
    [InlineData("-1", "900", false, 0, 0)]
    [InlineData("900", "60", false, 0, 0)]
    [InlineData("60", "3601", false, 0, 0)]
    [InlineData("999999999999", "900", false, 0, 0)]
    public void Permit_ttl_bounds_resolve_once_into_valid_or_invalid_state(
        string? min,
        string? max,
        bool expectedValid,
        int expectedMin,
        int expectedMax)
    {
        var state = RawExportPermitTtlOptions.Resolve(min, max);

        Assert.Equal(expectedValid, state.IsValid);
        Assert.Equal(expectedMin, state.MinSeconds);
        Assert.Equal(expectedMax, state.MaxSeconds);
    }

    [Fact]
    public async Task Readiness_reports_invalid_permit_ttl_bounds_without_startup_failfast_or_value_leak()
    {
        var productionRegistrationSeen = false;
        using var factory = ProductionFactory(builder =>
        {
            builder.UseSetting($"{RawExportPermitTtlOptions.SectionName}:{RawExportPermitTtlOptions.PermitTtlMinSecondsKey}", "60");
            builder.UseSetting($"{RawExportPermitTtlOptions.SectionName}:{RawExportPermitTtlOptions.PermitTtlMaxSecondsKey}", "3601");
        }, services =>
        {
            productionRegistrationSeen = services.Any(descriptor =>
                descriptor.ServiceType == typeof(IReadinessCheck) &&
                descriptor.ImplementationType == typeof(RawExportPermitTtlReadinessCheck));
        });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/readiness");
        var body = await response.Content.ReadAsStringAsync();
        var codes = CodesFromBody(body);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, (int)response.StatusCode);
        Assert.True(productionRegistrationSeen);
        Assert.Equal([RawExportPermitTtlBoundsState.InvalidCode], codes);
        Assert.DoesNotContain("3601", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Readiness_accepts_absent_bounds_as_immutable_default_state()
    {
        using var factory = ProductionFactory(_ => { });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/readiness");
        var bounds = factory.Services.GetRequiredService<RawExportPermitTtlBoundsState>();

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal(RawExportPermitTtlOptions.DefaultMinSeconds, bounds.MinSeconds);
        Assert.Equal(RawExportPermitTtlOptions.DefaultMaxSeconds, bounds.MaxSeconds);
    }

    [Fact]
    public async Task Catalog_approve_revalidates_persisted_ttl_against_rebuilt_provider_bounds()
    {
        await postgres.ResetDatabaseAsync();
        var policyId = Guid.Parse("88ae2000-0000-5000-8000-000000000001");

        using (var factoryA = ProductionFactory(builder =>
               {
                   builder.UseSetting($"{RawExportPermitTtlOptions.SectionName}:{RawExportPermitTtlOptions.PermitTtlMinSecondsKey}", "60");
                   builder.UseSetting($"{RawExportPermitTtlOptions.SectionName}:{RawExportPermitTtlOptions.PermitTtlMaxSecondsKey}", "900");
               },
               connectionString: postgres.ConnectionString))
        {
            await using var scopeA = factoryA.Services.CreateAsyncScope();
            var repositoryA = scopeA.ServiceProvider.GetRequiredService<IRawExportPolicyRepository>();

            var draft = await repositoryA.AddVersionAsync(new AddRawExportPolicyVersionCommand(
                policyId,
                0,
                RawExportMode.ExternalExportOnlyNoRetain,
                "ttl-policy",
                null,
                "NO_RETAIN",
                RawExportConsentRequirement.Required,
                null,
                null,
                "Processor",
                "controller",
                "VN",
                "VN",
                "VN",
                null,
                null,
                new HashSet<RawExportRawClass> { RawExportRawClass.LiveSelfieImage },
                300));

            Assert.Equal(300, draft.PermitTtlSeconds);
        }

        using var factoryB = ProductionFactory(builder =>
            {
                builder.UseSetting($"{RawExportPermitTtlOptions.SectionName}:{RawExportPermitTtlOptions.PermitTtlMinSecondsKey}", "60");
                builder.UseSetting($"{RawExportPermitTtlOptions.SectionName}:{RawExportPermitTtlOptions.PermitTtlMaxSecondsKey}", "120");
            },
            connectionString: postgres.ConnectionString);
        await using var scopeB = factoryB.Services.CreateAsyncScope();
        var repositoryB = scopeB.ServiceProvider.GetRequiredService<IRawExportPolicyRepository>();
        var db = scopeB.ServiceProvider.GetRequiredService<TagEkycDbContext>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repositoryB.CatalogApproveAsync(new CloseRawExportPolicyVersionCommand(
                policyId,
                1,
                "principal:catalog-author",
                "decision:approval:rebuilt-bounds")));

        Assert.Equal("RAW_EXPORT_POLICY_PERMIT_TTL_INVALID", exception.Message);
        Assert.False(await db.RawExportPolicyClosures.AnyAsync(row => row.PolicyId == policyId));
    }

    private static WebApplicationFactory<Program> ProductionFactory(
        Action<IWebHostBuilder> configure,
        Action<IServiceCollection>? inspectServices = null,
        string? connectionString = null) =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("environment", "Production");
                builder.UseSetting("TagEkyc:Persistence:Provider", "Postgres");
                builder.UseSetting("TagEkyc:Persistence:ConnectionStringSecretRef", SecretRef(connectionString ?? "Host=127.0.0.1;Database=unused;Username=unused;Password=db-secret"));
                builder.UseSetting("TagEkyc:ApiKeyStore:Backend", ApiKeyStoreBackends.Postgres);
                builder.UseSetting("TagEkyc:ApiKeyStore:PepperSecretRef", Tip84BTestSupport.PepperSecretRef());
                builder.UseSetting("TagEkyc:EvidenceSigning:Backend", EvidenceSigningBackends.ProductionTrialP12);
                builder.UseSetting("TagEkyc:Retention:RegulatedEvidenceRetentionDays", "30");
                builder.UseSetting("TagEkyc:DecisionThresholds:FaceMatch", "0.80");
                builder.UseSetting("TagEkyc:DecisionThresholds:Liveness", "0.80");
                configure(builder);
                builder.ConfigureTestServices(services =>
                {
                    inspectServices?.Invoke(services);
                    RemoveReadinessHostedServices(services);
                    services.RemoveAll<IReadinessCheck>();
                    services.AddScoped<IReadinessCheck, RawExportPermitTtlReadinessCheck>();
                });
            });

    private static void RemoveReadinessHostedServices(IServiceCollection services)
    {
        var readinessHostedServices = new HashSet<string>(StringComparer.Ordinal)
        {
            "PostgresProductionReadinessHostedService",
            "ApiKeyStoreProductionReadinessHostedService",
            "EvidenceSignerStartupValidationHostedService",
        };

        var descriptors = services
            .Where(descriptor => descriptor.ServiceType == typeof(IHostedService) &&
                                 descriptor.ImplementationType is not null &&
                                 readinessHostedServices.Contains(descriptor.ImplementationType.Name))
            .ToArray();
        foreach (var descriptor in descriptors)
        {
            services.Remove(descriptor);
        }
    }

    private static string[] CodesFromBody(string body)
    {
        using var document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty("codes").EnumerateArray()
            .Select(element => element.GetString()!)
            .ToArray();
    }

    private static string SecretRef(string value)
    {
        var name = $"TAGEKYC_TIP88AE2_SECRET_{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable(name, value);
        return $"env:{name}";
    }
}
