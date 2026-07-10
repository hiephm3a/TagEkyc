using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Infrastructure.Auth;
using TagEkyc.Infrastructure.Persistence;
using TagEkyc.Infrastructure.Signing;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip86DecisionThresholdConfigTests(PostgresPersistenceFixture postgres) : IAsyncLifetime
{
    public Task InitializeAsync() => postgres.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public void Production_rejects_missing_face_match_threshold_first()
    {
        using var factory = ProductionFactory(builder =>
        {
            ConfigureProductionDb(builder, postgres.ConnectionString);
            ConfigureProductionTrialP12(builder);
            ConfigureProductionApiKeyStore(builder);
            ConfigureRetention(builder);
        });

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        var rendered = exception.ToString();
        Assert.Contains("PROD_THRESHOLD_FACE_MATCH_MISSING", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("PROD_THRESHOLD_LIVENESS_MISSING", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_rejects_missing_liveness_threshold_after_face_match_is_present()
    {
        using var factory = ProductionFactory(builder =>
        {
            ConfigureProductionDb(builder, postgres.ConnectionString);
            ConfigureProductionTrialP12(builder);
            ConfigureProductionApiKeyStore(builder);
            ConfigureRetention(builder);
            ConfigureDecisionThresholds(builder, faceMatch: "0.80", liveness: null);
        });

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        var rendered = exception.ToString();
        Assert.Contains("PROD_THRESHOLD_LIVENESS_MISSING", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("PROD_THRESHOLD_FACE_MATCH_MISSING", rendered, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("0.75")]
    [InlineData("1.5")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("abc")]
    public void Production_rejects_invalid_face_match_threshold_with_stable_code(string configuredValue)
    {
        using var factory = ProductionFactory(builder =>
        {
            ConfigureProductionDb(builder, postgres.ConnectionString);
            ConfigureProductionTrialP12(builder);
            ConfigureProductionApiKeyStore(builder);
            ConfigureRetention(builder);
            ConfigureDecisionThresholds(builder, faceMatch: configuredValue, liveness: "0.80");
        });

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        var rendered = exception.ToString();
        Assert.Contains("PROD_THRESHOLD_FACE_MATCH_INVALID", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("PROD_THRESHOLD_LIVENESS_INVALID", rendered, StringComparison.Ordinal);
        if (configuredValue.Length > 2)
        {
            Assert.DoesNotContain(configuredValue, rendered, StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData("0.75")]
    [InlineData("1.5")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("abc")]
    public void Production_rejects_invalid_liveness_threshold_with_stable_code(string configuredValue)
    {
        using var factory = ProductionFactory(builder =>
        {
            ConfigureProductionDb(builder, postgres.ConnectionString);
            ConfigureProductionTrialP12(builder);
            ConfigureProductionApiKeyStore(builder);
            ConfigureRetention(builder);
            ConfigureDecisionThresholds(builder, faceMatch: "0.80", liveness: configuredValue);
        });

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        var rendered = exception.ToString();
        Assert.Contains("PROD_THRESHOLD_LIVENESS_INVALID", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("PROD_THRESHOLD_FACE_MATCH_INVALID", rendered, StringComparison.Ordinal);
        if (configuredValue.Length > 2)
        {
            Assert.DoesNotContain(configuredValue, rendered, StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData("0.80")]
    [InlineData("0.8")]
    public async Task Production_accepts_approved_threshold_value_and_binds_options(string configuredValue)
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
            ConfigureRetention(builder);
            ConfigureDecisionThresholds(builder, configuredValue, configuredValue);
        });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");
        var options = factory.Services.GetRequiredService<IOptions<DecisionThresholdOptions>>().Value;

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal(0.80m, options.FaceMatch);
        Assert.Equal(0.80m, options.Liveness);
    }

    [Fact]
    public void Signing_configuration_error_is_not_masked_by_threshold_config()
    {
        using var factory = ProductionFactory(builder =>
        {
            ConfigureProductionDb(builder, postgres.ConnectionString);
            ConfigureProductionApiKeyStore(builder);
            ConfigureRetention(builder);
            ConfigureDecisionThresholds(builder, faceMatch: "0.75", liveness: "0.75");
            builder.UseSetting("TagEkyc:EvidenceSigning:Backend", EvidenceSigningBackends.LocalDev);
        });

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        var rendered = exception.ToString();
        Assert.Contains("PROD_SIGNING_LOCALDEV_BACKEND_FORBIDDEN", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("PROD_THRESHOLD_", rendered, StringComparison.Ordinal);
    }

    private static WebApplicationFactory<Program> ProductionFactory(Action<IWebHostBuilder> configure) =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("environment", "Production");
                builder.UseSetting("TagEkyc:Persistence:Provider", "Postgres");
                configure(builder);
            });

    private static void ConfigureProductionDb(IWebHostBuilder builder, string connectionString)
    {
        var secretName = $"TAGEKYC_TIP86_DB_{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable(secretName, connectionString);
        builder.UseSetting("TagEkyc:Persistence:ConnectionStringSecretRef", $"env:{secretName}");
    }

    private static void ConfigureProductionTrialP12(IWebHostBuilder builder)
    {
        var password = $"tip86-password-{Guid.NewGuid():N}";
        var secretName = $"TAGEKYC_TIP86_P12_{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable(secretName, password);
        var p12Path = Path.Combine(Path.GetTempPath(), $"tagekyc-tip86-{Guid.NewGuid():N}.p12");
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

    private static void ConfigureRetention(IWebHostBuilder builder) =>
        builder.UseSetting("TagEkyc:Retention:RegulatedEvidenceRetentionDays", "30");

    private static void ConfigureDecisionThresholds(
        IWebHostBuilder builder,
        string? faceMatch = "0.80",
        string? liveness = "0.80")
    {
        if (faceMatch is not null)
        {
            builder.UseSetting("TagEkyc:DecisionThresholds:FaceMatch", faceMatch);
        }

        if (liveness is not null)
        {
            builder.UseSetting("TagEkyc:DecisionThresholds:Liveness", liveness);
        }
    }

    private static byte[] CreateP12(string password)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest("CN=TagEkyc TIP-86 Test", key, HashAlgorithmName.SHA256);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(1));
        return certificate.Export(X509ContentType.Pkcs12, password);
    }
}
