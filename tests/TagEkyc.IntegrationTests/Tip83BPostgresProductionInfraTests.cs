using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using TagEkyc.Infrastructure.Auth;
using TagEkyc.Infrastructure.Persistence;
using TagEkyc.Infrastructure.Signing;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip83BPostgresProductionInfraTests(PostgresPersistenceFixture postgres) : IAsyncLifetime
{
    public Task InitializeAsync() => postgres.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [InlineData("InMemory")]
    [InlineData(null)]
    public void Production_refuses_inmemory_or_missing_persistence_provider(string? provider)
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("environment", "Production");
                if (provider is not null)
                {
                    builder.UseSetting("TagEkyc:Persistence:Provider", provider);
                }
            });

        var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());
        Assert.Contains("PROD_PERSISTENCE_INMEMORY_FORBIDDEN", exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Development_inmemory_persistence_still_starts()
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
    public void Production_rejects_plaintext_connection_string()
    {
        const string connectionString = "Host=localhost;Database=unused;Username=unused;Password=plain-secret";
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("environment", "Production");
                builder.UseSetting("TagEkyc:Persistence:Provider", "Postgres");
                builder.UseSetting("TagEkyc:Persistence:ConnectionString", connectionString);
            });

        var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());
        var rendered = exception.ToString();
        Assert.Contains("PROD_DB_CONNECTION_PLAINTEXT_FORBIDDEN", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("plain-secret", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain(connectionString, rendered, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(SecretRefCases))]
    public void Production_connection_secret_ref_taxonomy_is_fail_closed_and_sanitized(string secretRef, string expectedCode, string? forbiddenText)
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("environment", "Production");
                builder.UseSetting("TagEkyc:Persistence:Provider", "Postgres");
                builder.UseSetting("TagEkyc:Persistence:ConnectionStringSecretRef", secretRef);
            });

        var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());
        var rendered = exception.ToString();
        Assert.Contains(expectedCode, rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("PROD_SIGNING", rendered, StringComparison.Ordinal);
        if (!string.IsNullOrWhiteSpace(forbiddenText))
        {
            Assert.DoesNotContain(forbiddenText, rendered, StringComparison.Ordinal);
        }
    }

    public static IEnumerable<object?[]> SecretRefCases()
    {
        var unsetEnv = $"TAGEKYC_TIP83B_UNSET_{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable(unsetEnv, null);
        var emptyEnv = $"TAGEKYC_TIP83B_EMPTY_{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable(emptyEnv, "   ");
        var directoryPath = Path.Combine(Path.GetTempPath(), $"tagekyc-tip83b-secret-dir-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directoryPath);
        var emptyFile = Path.Combine(Path.GetTempPath(), $"tagekyc-tip83b-empty-{Guid.NewGuid():N}.txt");
        File.WriteAllText(emptyFile, "  ");
        var missingFile = Path.Combine(Path.GetTempPath(), $"tagekyc-tip83b-missing-{Guid.NewGuid():N}.txt");

        yield return ["vault:db", "PROD_DB_CONNECTION_SECRET_INVALID", null];
        yield return ["env:", "PROD_DB_CONNECTION_SECRET_INVALID", null];
        yield return ["file:", "PROD_DB_CONNECTION_SECRET_INVALID", null];
        yield return ["file:relative-secret.txt", "PROD_DB_CONNECTION_SECRET_INVALID", "relative-secret.txt"];
        yield return [$"file:{directoryPath}", "PROD_DB_CONNECTION_SECRET_INVALID", directoryPath];
        yield return [$"env:{unsetEnv}", "PROD_DB_CONNECTION_SECRET_MISSING", unsetEnv];
        yield return [$"env:{emptyEnv}", "PROD_DB_CONNECTION_SECRET_MISSING", emptyEnv];
        yield return [$"file:{missingFile}", "PROD_DB_CONNECTION_SECRET_MISSING", missingFile];
        yield return [$"file:{emptyFile}", "PROD_DB_CONNECTION_SECRET_MISSING", emptyFile];
    }

    [Fact]
    public async Task Production_startup_accepts_secret_ref_postgres_after_signing_self_test_and_readiness()
    {
        var password = $"tip83b-password-{Guid.NewGuid():N}";
        var passwordSecret = $"TAGEKYC_TIP83B_P12_{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable(passwordSecret, password);
        var p12Path = Path.Combine(Path.GetTempPath(), $"tagekyc-tip83b-{Guid.NewGuid():N}.p12");
        await File.WriteAllBytesAsync(p12Path, CreateP12(password));

        try
        {
            await Tip84BTestSupport.SeedActiveApiKeyAsync(
                postgres,
                Tip84BTestSupport.PresentedKey,
                Tip84BTestSupport.Pepper);
            using var factory = ProductionFactory(builder =>
            {
                ConfigureProductionDb(builder, postgres.ConnectionString);
                ConfigureProductionTrialP12(builder, p12Path, $"env:{passwordSecret}");
            });
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/health");

            Assert.True(response.IsSuccessStatusCode);
        }
        finally
        {
            Environment.SetEnvironmentVariable(passwordSecret, null);
            TryDelete(p12Path);
        }
    }

    [Fact]
    public void Signing_configuration_errors_are_not_masked_by_db_readiness()
    {
        var dbSecret = $"TAGEKYC_TIP83B_DB_UNREACHABLE_{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable(
            dbSecret,
            "Host=127.0.0.1;Port=1;Database=unreachable;Username=unused;Password=db-secret;Timeout=1");
        using var factory = ProductionFactory(builder =>
        {
            builder.UseSetting("TagEkyc:Persistence:ConnectionStringSecretRef", $"env:{dbSecret}");
            builder.UseSetting("TagEkyc:EvidenceSigning:Backend", EvidenceSigningBackends.ProductionTrialP12);
            builder.UseSetting("TagEkyc:EvidenceSigning:P12Path", Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.p12"));
            builder.UseSetting("TagEkyc:EvidenceSigning:P12PasswordSecretRef", "env:TAGEKYC_TIP83B_MISSING_P12_SECRET");
            builder.UseSetting("TagEkyc:EvidenceSigning:KeyId", "tagekyc-es256-2026-v1");
        });

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        var rendered = exception.ToString();
        Assert.Contains("PROD_SIGNING_P12_FILE_NOT_FOUND", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("PROD_DB_", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("db-secret", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Readiness_rejects_unreachable_database_with_sanitized_code()
    {
        const string connectionString = "Host=127.0.0.1;Port=1;Database=unreachable;Username=unused;Password=secret-db-password;Timeout=1";
        await using var db = CreateDbContext(connectionString);

        var exception = await Assert.ThrowsAsync<PostgresProductionReadinessException>(() =>
            new PostgresProductionReadinessValidator(db).ValidateAsync(CancellationToken.None));

        Assert.Equal(PostgresProductionReadinessValidator.Unreachable, exception.Code);
        Assert.DoesNotContain("secret-db-password", exception.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(connectionString, exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Readiness_rejects_non_npgsql_provider()
    {
        await using var db = new TagEkycDbContext(new DbContextOptionsBuilder<TagEkycDbContext>().Options);

        var exception = await Assert.ThrowsAsync<PostgresProductionReadinessException>(() =>
            new PostgresProductionReadinessValidator(db).ValidateAsync(CancellationToken.None));

        Assert.Equal(PostgresProductionReadinessValidator.ProviderInvalid, exception.Code);
    }

    [Fact]
    public async Task Readiness_rejects_missing_migration_history()
    {
        await using var db = postgres.CreateDbContext();
        await db.Database.ExecuteSqlRawAsync("DROP TABLE public.\"__EFMigrationsHistory\"");

        var exception = await Assert.ThrowsAsync<PostgresProductionReadinessException>(() =>
            new PostgresProductionReadinessValidator(db).ValidateAsync(CancellationToken.None));

        Assert.Equal(PostgresProductionReadinessValidator.MigrationHistoryMissing, exception.Code);
    }

    [Fact]
    public async Task Readiness_rejects_pending_migrations()
    {
        await using var db = postgres.CreateDbContext();
        await db.Database.ExecuteSqlRawAsync("""
            DELETE FROM public."__EFMigrationsHistory"
            WHERE "MigrationId" = (
                SELECT "MigrationId"
                FROM public."__EFMigrationsHistory"
                ORDER BY "MigrationId" DESC
                LIMIT 1
            )
            """);

        var exception = await Assert.ThrowsAsync<PostgresProductionReadinessException>(() =>
            new PostgresProductionReadinessValidator(db).ValidateAsync(CancellationToken.None));

        Assert.Equal(PostgresProductionReadinessValidator.MigrationsPending, exception.Code);
    }

    [Fact]
    public async Task Readiness_rejects_missing_append_idempotency_table()
    {
        await using var db = postgres.CreateDbContext();
        await db.Database.ExecuteSqlRawAsync("DROP TABLE tagekyc.append_idempotency_records");

        var exception = await Assert.ThrowsAsync<PostgresProductionReadinessException>(() =>
            new PostgresProductionReadinessValidator(db).ValidateAsync(CancellationToken.None));

        Assert.Equal(PostgresProductionReadinessValidator.RequiredTableMissing, exception.Code);
    }

    [Fact]
    public async Task Readiness_accepts_current_postgres_migrations()
    {
        await using var db = postgres.CreateDbContext();

        await new PostgresProductionReadinessValidator(db).ValidateAsync(CancellationToken.None);
    }

    [Fact]
    public void Runtime_example_uses_secret_ref_not_plaintext_connection_string()
    {
        var root = FindRepositoryRoot();
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "config", "appsettings.example.json")));
        var persistence = document.RootElement.GetProperty("TagEkyc").GetProperty("Persistence");

        Assert.True(persistence.TryGetProperty("ConnectionStringSecretRef", out _));
        Assert.False(persistence.TryGetProperty("ConnectionString", out _));
    }

    private static WebApplicationFactory<Program> ProductionFactory(Action<IWebHostBuilder> configure) =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("environment", "Production");
                builder.UseSetting("TagEkyc:Persistence:Provider", "Postgres");
                configure(builder);
                ConfigureProductionApiKeyStore(builder);
                ConfigureRetention(builder);
            });

    private static void ConfigureProductionDb(IWebHostBuilder builder, string connectionString)
    {
        var secretName = $"TAGEKYC_TIP83B_DB_{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable(secretName, connectionString);
        builder.UseSetting("TagEkyc:Persistence:ConnectionStringSecretRef", $"env:{secretName}");
    }

    private static void ConfigureProductionTrialP12(IWebHostBuilder builder, string p12Path, string secretRef)
    {
        builder.UseSetting("TagEkyc:EvidenceSigning:Backend", EvidenceSigningBackends.ProductionTrialP12);
        builder.UseSetting("TagEkyc:EvidenceSigning:P12Path", p12Path);
        builder.UseSetting("TagEkyc:EvidenceSigning:P12PasswordSecretRef", secretRef);
        builder.UseSetting("TagEkyc:EvidenceSigning:KeyId", "tagekyc-es256-2026-v1");
    }

    private static void ConfigureProductionApiKeyStore(IWebHostBuilder builder)
    {
        builder.UseSetting("TagEkyc:ApiKeyStore:Backend", ApiKeyStoreBackends.Postgres);
        builder.UseSetting("TagEkyc:ApiKeyStore:PepperSecretRef", Tip84BTestSupport.PepperSecretRef());
    }

    private static void ConfigureRetention(IWebHostBuilder builder) =>
        builder.UseSetting("TagEkyc:Retention:RegulatedEvidenceRetentionDays", "30");

    private static TagEkycDbContext CreateDbContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<TagEkycDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new TagEkycDbContext(options);
    }

    private static byte[] CreateP12(string password)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest("CN=TagEkyc TIP-83B Test", key, HashAlgorithmName.SHA256);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(1));
        return certificate.Export(X509ContentType.Pkcs12, password);
    }

    private static string FindRepositoryRoot()
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

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
    }
}
