using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using TagEkyc.Application;
using TagEkyc.Application.LocalDev;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Domain;
using TagEkyc.Infrastructure.Auth;
using TagEkyc.Infrastructure.Persistence;
using TagEkyc.Infrastructure.Persistence.Entities;
using TagEkyc.Infrastructure.Signing;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip84BHashedApiKeyStoreTests(PostgresPersistenceFixture postgres) : IAsyncLifetime
{
    private static readonly byte[] Pepper = Tip84BTestSupport.Pepper;
    private const string PresentedKey = Tip84BTestSupport.PresentedKey;

    public Task InitializeAsync() => postgres.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Postgres_store_accepts_exact_key_and_rejects_trimmed_variant()
    {
        await SeedApiKeyAsync(PresentedKey, "Active");
        await using var db = postgres.CreateDbContext();
        var store = new PostgresHashedApiKeyStore(db, new ApiKeyStorePepper(Pepper));

        var exact = await store.FindByPresentedKeyAsync(PresentedKey);
        var withSpace = await store.FindByPresentedKeyAsync($" {PresentedKey}");

        Assert.NotNull(exact);
        Assert.Equal("abcdefghijklmnop", exact.KeyPrefix);
        Assert.Null(withSpace);
    }

    [Theory]
    [InlineData("")]
    [InlineData("localdev-business-key")]
    [InlineData("tek_short_AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    [InlineData("tek_abcdefghijklmnop_short")]
    [InlineData("tek_abcdefghijklmno+_AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    public async Task Malformed_key_returns_null_without_throwing(string presentedKey)
    {
        await using var db = postgres.CreateDbContext();
        var store = new PostgresHashedApiKeyStore(db, new ApiKeyStorePepper(Pepper));

        var resolved = await store.FindByPresentedKeyAsync(presentedKey);

        Assert.Null(resolved);
    }

    [Theory]
    [InlineData("Active", ApiKeyStatus.Active)]
    [InlineData("Expired", ApiKeyStatus.Expired)]
    [InlineData("Revoked", ApiKeyStatus.Revoked)]
    [InlineData("Suspended", ApiKeyStatus.Revoked)]
    [InlineData("RotatedReplaced", ApiKeyStatus.Revoked)]
    [InlineData("FutureStatus", ApiKeyStatus.Revoked)]
    public async Task Credential_status_down_maps_fail_closed(string credentialStatus, ApiKeyStatus expected)
    {
        await SeedApiKeyAsync(PresentedKey, credentialStatus);
        await using var db = postgres.CreateDbContext();
        var store = new PostgresHashedApiKeyStore(db, new ApiKeyStorePepper(Pepper));

        var resolved = await store.FindByPresentedKeyAsync(PresentedKey);

        Assert.NotNull(resolved);
        Assert.Equal(expected, resolved.Status);
    }

    [Fact]
    public async Task Check_constraint_rejects_non_32_byte_key_hash()
    {
        await using var db = postgres.CreateDbContext();
        db.ApiKeys.Add(Tip84BTestSupport.ApiKeyRow(PresentedKey, [1, 2, 3], "Active"));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Provisioning_inserts_hash_only_and_returns_plaintext_once_to_caller()
    {
        await using var db = postgres.CreateDbContext();
        var service = new ApiKeyProvisioningService(
            db,
            new ApiKeyStorePepper(Pepper),
            new LocalDevRuntimePolicySource(),
            new FixedGenerator(PresentedKey));

        var result = await service.ProvisionAsync(new ApiKeyProvisioningCommand(
            LocalDevRuntimePolicySource.BusinessClientId,
            AuthenticatedCallerCategory.BusinessConsumer,
            new HashSet<string> { "business.session.read" }));
        var row = await db.ApiKeys.SingleAsync();

        Assert.Equal(PresentedKey, result.PresentedKey);
        Assert.Equal("abcdefghijklmnop", row.KeyPrefix);
        Assert.Equal(32, row.KeyHash.Length);
        Assert.NotEqual(Encoding.UTF8.GetBytes(PresentedKey), row.KeyHash);
        Assert.Equal(ManagedApiKeyConstants.CredentialType, row.CredentialType);
    }

    [Fact]
    public async Task Provisioning_retries_duplicate_prefix_without_printing_failed_key()
    {
        await SeedApiKeyAsync(PresentedKey, "Active");
        await using var db = postgres.CreateDbContext();
        var service = new ApiKeyProvisioningService(
            db,
            new ApiKeyStorePepper(Pepper),
            new LocalDevRuntimePolicySource(),
            new SequenceGenerator(
                PresentedKey,
                "tek_qrstuvwxyzABCDEF_BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB"));

        var result = await service.ProvisionAsync(new ApiKeyProvisioningCommand(
            LocalDevRuntimePolicySource.BusinessClientId,
            AuthenticatedCallerCategory.BusinessConsumer,
            new HashSet<string> { "business.session.read" }));

        Assert.Equal("qrstuvwxyzABCDEF", result.KeyPrefix);
        Assert.Equal(2, await db.ApiKeys.CountAsync());
    }

    [Fact]
    public async Task Readiness_rejects_missing_api_keys_table()
    {
        await using var db = postgres.CreateDbContext();
        await db.Database.ExecuteSqlRawAsync("DROP TABLE tagekyc.api_keys");

        var exception = await Assert.ThrowsAsync<PostgresProductionReadinessException>(() =>
            new PostgresProductionReadinessValidator(db).ValidateAsync(CancellationToken.None));

        Assert.Equal(PostgresProductionReadinessValidator.RequiredTableMissing, exception.Code);
    }

    [Fact]
    public async Task Production_rejects_zero_active_api_keys()
    {
        await using var db = postgres.CreateDbContext();

        var exception = await Assert.ThrowsAsync<ApiKeyStoreProductionReadinessException>(() =>
            new ApiKeyStoreProductionReadinessValidator(db, new ApiKeyStorePepper(Pepper)).ValidateAsync(CancellationToken.None));

        Assert.Equal(ApiKeyStoreProductionReadinessValidator.NoActiveKeys, exception.Code);
    }

    [Fact]
    public void Production_rejects_localdev_api_key_store()
    {
        using var factory = ProductionFactory(builder =>
        {
            ConfigureProductionDb(builder, postgres.ConnectionString);
            ConfigureProductionTrialP12(builder, createValidP12: true);
            builder.UseSetting("TagEkyc:ApiKeyStore:Backend", ApiKeyStoreBackends.LocalDev);
        });

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains("PROD_APIKEY_LOCALDEV_STORE_FORBIDDEN", exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Postgres_api_key_store_requires_postgres_persistence()
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("environment", "Development");
                builder.UseSetting("TagEkyc:Persistence:Provider", "InMemory");
                builder.UseSetting("TagEkyc:ApiKeyStore:Backend", ApiKeyStoreBackends.Postgres);
                builder.UseSetting("TagEkyc:ApiKeyStore:PepperSecretRef", PepperSecretRef());
            });

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains("PROD_APIKEY_REQUIRES_POSTGRES_PERSISTENCE", exception.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null, "PROD_APIKEY_PEPPER_SECRET_UNAVAILABLE")]
    [InlineData("vault:missing", "PROD_APIKEY_PEPPER_SECRET_REF_INVALID")]
    [InlineData("short", "PROD_APIKEY_PEPPER_SECRET_REF_INVALID")]
    public void Pepper_secret_ref_taxonomy_is_sanitized(string? secretRefKind, string expectedCode)
    {
        var secretRef = secretRefKind switch
        {
            null => null,
            "short" => PepperSecretRef("too-short"),
            _ => secretRefKind,
        };
        using var factory = ProductionFactory(builder =>
        {
            ConfigureProductionDb(builder, postgres.ConnectionString);
            ConfigureProductionTrialP12(builder, createValidP12: true);
            builder.UseSetting("TagEkyc:ApiKeyStore:Backend", ApiKeyStoreBackends.Postgres);
            if (secretRef is not null)
            {
                builder.UseSetting("TagEkyc:ApiKeyStore:PepperSecretRef", secretRef);
            }
        });

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains(expectedCode, exception.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("too-short", exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Production_rejects_plaintext_pepper_config()
    {
        using var factory = ProductionFactory(builder =>
        {
            ConfigureProductionDb(builder, postgres.ConnectionString);
            ConfigureProductionTrialP12(builder, createValidP12: true);
            builder.UseSetting("TagEkyc:ApiKeyStore:Backend", ApiKeyStoreBackends.Postgres);
            builder.UseSetting("TagEkyc:ApiKeyStore:Pepper", "plaintext-pepper-secret-that-must-not-be-used");
        });

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains("PROD_APIKEY_PEPPER_SECRET_REF_INVALID", exception.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("plaintext-pepper-secret", exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Signing_configuration_error_is_not_masked_by_api_key_store()
    {
        using var factory = ProductionFactory(builder =>
        {
            ConfigureProductionDb(builder, postgres.ConnectionString);
            builder.UseSetting("TagEkyc:EvidenceSigning:Backend", EvidenceSigningBackends.ProductionTrialP12);
            builder.UseSetting("TagEkyc:EvidenceSigning:P12Path", Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.p12"));
            builder.UseSetting("TagEkyc:EvidenceSigning:P12PasswordSecretRef", "env:TAGEKYC_TIP84B_MISSING_P12");
            builder.UseSetting("TagEkyc:EvidenceSigning:KeyId", "tagekyc-es256-2026-v1");
            builder.UseSetting("TagEkyc:ApiKeyStore:Backend", ApiKeyStoreBackends.Postgres);
            builder.UseSetting("TagEkyc:ApiKeyStore:PepperSecretRef", "vault:invalid");
        });

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        var rendered = exception.ToString();
        Assert.Contains("PROD_SIGNING_P12_FILE_NOT_FOUND", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("PROD_APIKEY_", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public void Source_uses_fixed_time_hash_compare_and_no_plaintext_columns()
    {
        var root = Tip84BTestSupport.FindRepositoryRoot();
        var storeSource = File.ReadAllText(Path.Combine(root, "src", "TagEkyc.Infrastructure", "Auth", "PostgresHashedApiKeyStore.cs"));
        var migrationSource = File.ReadAllText(Directory.GetFiles(Path.Combine(root, "src", "TagEkyc.Infrastructure", "Persistence", "Migrations"), "*Tip84BApiKeyStore.cs").Single());

        Assert.Contains("CryptographicOperations.FixedTimeEquals", File.ReadAllText(Path.Combine(root, "src", "TagEkyc.Infrastructure", "Auth", "ApiKeyHasher.cs")), StringComparison.Ordinal);
        Assert.DoesNotContain("== row.KeyHash", storeSource, StringComparison.Ordinal);
        Assert.DoesNotContain("ApiKeyValue", migrationSource, StringComparison.Ordinal);
        Assert.DoesNotContain("Plaintext", migrationSource, StringComparison.OrdinalIgnoreCase);
    }

    private async Task SeedApiKeyAsync(string presentedKey, string credentialStatus)
    {
        await using var db = postgres.CreateDbContext();
        db.ApiKeys.Add(Tip84BTestSupport.ApiKeyRow(
            presentedKey,
            ApiKeyHasher.Hash(Pepper, presentedKey),
            credentialStatus));
        await db.SaveChangesAsync();
    }

    private static WebApplicationFactory<Program> ProductionFactory(Action<IWebHostBuilder> configure) =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("environment", "Production");
                builder.UseSetting("TagEkyc:Persistence:Provider", "Postgres");
                configure(builder);
                ConfigureRetention(builder);
            });

    private static void ConfigureProductionDb(IWebHostBuilder builder, string connectionString) =>
        builder.UseSetting("TagEkyc:Persistence:ConnectionStringSecretRef", DbSecretRef(connectionString));

    private static void ConfigureRetention(IWebHostBuilder builder) =>
        builder.UseSetting("TagEkyc:Retention:RegulatedEvidenceRetentionDays", "30");

    private static void ConfigureProductionTrialP12(IWebHostBuilder builder, bool createValidP12)
    {
        var password = $"tip84b-password-{Guid.NewGuid():N}";
        var secretName = $"TAGEKYC_TIP84B_P12_{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable(secretName, password);
        var p12Path = Path.Combine(Path.GetTempPath(), $"tagekyc-tip84b-{Guid.NewGuid():N}.p12");
        File.WriteAllBytes(p12Path, CreateP12(password));
        builder.UseSetting("TagEkyc:EvidenceSigning:Backend", EvidenceSigningBackends.ProductionTrialP12);
        builder.UseSetting("TagEkyc:EvidenceSigning:P12Path", createValidP12 ? p12Path : $"{p12Path}.missing");
        builder.UseSetting("TagEkyc:EvidenceSigning:P12PasswordSecretRef", $"env:{secretName}");
        builder.UseSetting("TagEkyc:EvidenceSigning:KeyId", "tagekyc-es256-2026-v1");
    }

    private static string PepperSecretRef(string value = "01234567890123456789012345678901") =>
        Tip84BTestSupport.PepperSecretRef(value);

    private static string DbSecretRef(string connectionString)
    {
        var secretName = $"TAGEKYC_TIP84B_DB_{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable(secretName, connectionString);
        return $"env:{secretName}";
    }

    private static byte[] CreateP12(string password)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest("CN=TagEkyc TIP-84B Test", key, HashAlgorithmName.SHA256);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(1));
        return certificate.Export(X509ContentType.Pkcs12, password);
    }

    private sealed class FixedGenerator(string presentedKey) : IManagedApiKeyGenerator
    {
        public ManagedApiKeyMaterial Generate()
        {
            var parsed = ManagedApiKeyParser.Parse(presentedKey) ?? throw new InvalidOperationException("bad key");
            return new ManagedApiKeyMaterial(presentedKey, parsed.Prefix);
        }
    }

    private sealed class SequenceGenerator(params string[] keys) : IManagedApiKeyGenerator
    {
        private int index;

        public ManagedApiKeyMaterial Generate()
        {
            var key = keys[Math.Min(index++, keys.Length - 1)];
            var parsed = ManagedApiKeyParser.Parse(key) ?? throw new InvalidOperationException("bad key");
            return new ManagedApiKeyMaterial(key, parsed.Prefix);
        }
    }
}

internal static class Tip84BTestSupport
{
    public const string PresentedKey = "tek_abcdefghijklmnop_AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    public static readonly byte[] Pepper = Encoding.UTF8.GetBytes("01234567890123456789012345678901");

    public static ApiKeyRow ApiKeyRow(string presentedKey, byte[] keyHash, string credentialStatus) =>
        new()
        {
            ApiKeyId = Guid.NewGuid(),
            ClientApplicationId = LocalDevRuntimePolicySource.BusinessClientId,
            PrincipalId = LocalDevRuntimePolicySource.BusinessClientId,
            CredentialRef = $"managed-api-key:{Guid.NewGuid():N}",
            CredentialType = ManagedApiKeyConstants.CredentialType,
            CredentialStatus = credentialStatus,
            KeyPrefix = ManagedApiKeyParser.Parse(presentedKey)?.Prefix ?? "abcdefghijklmnop",
            KeyHash = keyHash,
            ScopesJson = JsonSerializer.Serialize(new[] { "business.session.read" }),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
            CallerCategory = AuthenticatedCallerCategory.BusinessConsumer.ToString(),
            CreatedAt = DateTimeOffset.UtcNow,
        };

    public static async Task SeedActiveApiKeyAsync(PostgresPersistenceFixture postgres, string presentedKey, byte[] pepper)
    {
        await using var db = postgres.CreateDbContext();
        db.ApiKeys.Add(ApiKeyRow(presentedKey, ApiKeyHasher.Hash(pepper, presentedKey), "Active"));
        await db.SaveChangesAsync();
    }

    public static string PepperSecretRef(string value = "01234567890123456789012345678901")
    {
        var secretName = $"TAGEKYC_TIP84B_PEPPER_{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable(secretName, value);
        return $"env:{secretName}";
    }

    public static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "TagEkyc.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}
