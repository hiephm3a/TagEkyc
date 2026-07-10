using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TagEkyc.Infrastructure.Auth;
using TagEkyc.Infrastructure.Persistence;
using TagEkyc.Infrastructure.Retention;
using TagEkyc.Infrastructure.Signing;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip83E3RetentionConfigTests(PostgresPersistenceFixture postgres) : IAsyncLifetime
{
    public Task InitializeAsync() => postgres.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public void Production_rejects_missing_retention_policy()
    {
        using var factory = ProductionFactory(builder =>
        {
            ConfigureProductionDb(builder, postgres.ConnectionString);
            ConfigureProductionTrialP12(builder);
            ConfigureProductionApiKeyStore(builder);
        });

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        var rendered = exception.ToString();
        Assert.Contains("PROD_RETENTION_POLICY_MISSING", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("PROD_RETENTION_WINDOW_INVALID", rendered, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("999999999")]
    public void Production_rejects_invalid_retention_window_with_stable_code(string configuredValue)
    {
        using var factory = ProductionFactory(builder =>
        {
            ConfigureProductionDb(builder, postgres.ConnectionString);
            ConfigureProductionTrialP12(builder);
            ConfigureProductionApiKeyStore(builder);
            builder.UseSetting("TagEkyc:Retention:RegulatedEvidenceRetentionDays", configuredValue);
        });

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        var rendered = exception.ToString();
        Assert.Contains("PROD_RETENTION_WINDOW_INVALID", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("PROD_RETENTION_POLICY_MISSING", rendered, StringComparison.Ordinal);
        if (configuredValue.Length > 2)
        {
            Assert.DoesNotContain(configuredValue, rendered, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Production_accepts_valid_retention_window_and_binds_options()
    {
        await Tip84BTestSupport.SeedActiveApiKeyAsync(
            postgres,
            Tip84BTestSupport.PresentedKey,
            Tip84BTestSupport.Pepper);
        using var factory = ProductionFactory(builder =>
        {
            ConfigureProductionDb(builder, postgres.ConnectionString);
            ConfigureProductionTrialP12(builder);
            ConfigureProductionApiKeyStore(builder);
            builder.UseSetting("TagEkyc:Retention:RegulatedEvidenceRetentionDays", "30");
        });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");
        var options = factory.Services.GetRequiredService<IOptions<RetentionOptions>>().Value;

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal(30, options.RegulatedEvidenceRetentionDays);
    }

    [Fact]
    public async Task Development_starts_without_retention_policy()
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("environment", "Development");
                builder.UseSetting("TagEkyc:Persistence:Provider", "InMemory");
            });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public void Signing_configuration_error_is_not_masked_by_retention_config()
    {
        using var factory = ProductionFactory(builder =>
        {
            ConfigureProductionDb(builder, postgres.ConnectionString);
            ConfigureProductionApiKeyStore(builder);
            builder.UseSetting("TagEkyc:EvidenceSigning:Backend", EvidenceSigningBackends.LocalDev);
        });

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        var rendered = exception.ToString();
        Assert.Contains("PROD_SIGNING_LOCALDEV_BACKEND_FORBIDDEN", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("PROD_RETENTION_", rendered, StringComparison.Ordinal);
    }

    private static WebApplicationFactory<Program> ProductionFactory(Action<IWebHostBuilder> configure) =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("environment", "Production");
                builder.UseSetting("TagEkyc:Persistence:Provider", "Postgres");
                configure(builder);
                ConfigureDecisionThresholds(builder);
            });

    private static void ConfigureProductionDb(IWebHostBuilder builder, string connectionString)
    {
        var secretName = $"TAGEKYC_TIP83E3_DB_{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable(secretName, connectionString);
        builder.UseSetting("TagEkyc:Persistence:ConnectionStringSecretRef", $"env:{secretName}");
    }

    private static void ConfigureProductionTrialP12(IWebHostBuilder builder)
    {
        var password = $"tip83e3-password-{Guid.NewGuid():N}";
        var secretName = $"TAGEKYC_TIP83E3_P12_{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable(secretName, password);
        var p12Path = Path.Combine(Path.GetTempPath(), $"tagekyc-tip83e3-{Guid.NewGuid():N}.p12");
        File.WriteAllBytes(p12Path, CreateP12(password));
        builder.UseSetting("TagEkyc:EvidenceSigning:Backend", EvidenceSigningBackends.ProductionTrialP12);
        builder.UseSetting("TagEkyc:EvidenceSigning:P12Path", p12Path);
        builder.UseSetting("TagEkyc:EvidenceSigning:P12PasswordSecretRef", $"env:{secretName}");
        builder.UseSetting("TagEkyc:EvidenceSigning:KeyId", "tagekyc-es256-2026-v1");
    }

    private static void ConfigureProductionApiKeyStore(IWebHostBuilder builder)
    {
        builder.UseSetting("TagEkyc:ApiKeyStore:Backend", ApiKeyStoreBackends.Postgres);
        builder.UseSetting("TagEkyc:ApiKeyStore:PepperSecretRef", Tip84BTestSupport.PepperSecretRef());
    }

    private static void ConfigureDecisionThresholds(IWebHostBuilder builder)
    {
        builder.UseSetting("TagEkyc:DecisionThresholds:FaceMatch", "0.80");
        builder.UseSetting("TagEkyc:DecisionThresholds:Liveness", "0.80");
    }

    private static byte[] CreateP12(string password)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest("CN=TagEkyc TIP-83E-3 Test", key, HashAlgorithmName.SHA256);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(1));
        return certificate.Export(X509ContentType.Pkcs12, password);
    }
}
