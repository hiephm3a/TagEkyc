using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Domain;
using TagEkyc.Infrastructure.Auth;
using TagEkyc.Infrastructure.Signing;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip83ASigningKeyJwksTests(PostgresPersistenceFixture postgres) : IAsyncLifetime
{
    public Task InitializeAsync() => postgres.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ProductionTrialP12_starts_self_tests_and_serves_current_plus_previous_public_jwks()
    {
        var password = $"tip83a-password-{Guid.NewGuid():N}";
        var secretName = $"TAGEKYC_TIP83A_P12_PASSWORD_{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable(secretName, password);
        var p12Path = Path.Combine(Path.GetTempPath(), $"tagekyc-tip83a-{Guid.NewGuid():N}.p12");
        var previousKeysPath = Path.Combine(Path.GetTempPath(), $"tagekyc-tip83a-prev-{Guid.NewGuid():N}.json");
        await File.WriteAllBytesAsync(p12Path, CreateP12(password));

        using var previousSigner = new LocalDevEs256JwsEvidenceSigner(new LocalDevEs256JwsEvidenceSignerOptions
        {
            KeyId = "tagekyc-es256-2025-v1",
        });
        await File.WriteAllTextAsync(previousKeysPath, PreviousKeysJson(previousSigner));

        try
        {
            await Tip84BTestSupport.SeedActiveApiKeyAsync(
                postgres,
                Tip84BTestSupport.PresentedKey,
                Tip84BTestSupport.Pepper);
            using var factory = ProductionTrialFactory(p12Path, $"env:{secretName}", "tagekyc-es256-2026-v1", previousKeysPath);
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/.well-known/jwks.json");
            var body = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode, body);
            Assert.Contains("no-store", response.Headers.CacheControl?.ToString(), StringComparison.OrdinalIgnoreCase);
            using var document = JsonDocument.Parse(body);
            var keys = document.RootElement.GetProperty("keys").EnumerateArray().ToArray();
            Assert.Equal(2, keys.Length);
            Assert.All(keys, key => Assert.False(key.TryGetProperty("d", out _)));
            Assert.Contains(keys, key => key.GetProperty("kid").GetString() == "tagekyc-es256-2026-v1");
            var previous = keys.Single(key => key.GetProperty("kid").GetString() == "tagekyc-es256-2025-v1");

            var previousProof = await previousSigner.SignProofAsync(ProofRequest(), CancellationToken.None);
            Assert.True(VerifyJwsWithJwk(previousProof.SignatureValue, previous, "tagekyc-es256-2025-v1"));
        }
        finally
        {
            Environment.SetEnvironmentVariable(secretName, null);
            TryDelete(p12Path);
            TryDelete(previousKeysPath);
        }
    }

    [Fact]
    public void Production_rejects_localdev_signing_backend()
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("environment", "Production");
                builder.UseSetting("TagEkyc:Persistence:Provider", "Postgres");
                ConfigureProductionDb(builder, postgres.ConnectionString);
                builder.UseSetting("TagEkyc:EvidenceSigning:Backend", EvidenceSigningBackends.LocalDev);
            });

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains("PROD_SIGNING_LOCALDEV_BACKEND_FORBIDDEN", exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void RequireHardwareSigner_still_forces_pkcs11()
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("environment", "Production");
                builder.UseSetting("TagEkyc:Persistence:Provider", "Postgres");
                ConfigureProductionDb(builder, postgres.ConnectionString);
                ConfigureProductionApiKeyStore(builder);
                ConfigureRetention(builder);
                ConfigureDecisionThresholds(builder);
                builder.UseSetting("TagEkyc:EvidenceSigning:Backend", EvidenceSigningBackends.ProductionTrialP12);
                builder.UseSetting("TagEkyc:EvidenceSigning:RequireHardwareSigner", "true");
            });

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains("Invalid evidence signing backend configuration", exception.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null, "env:irrelevant", "tagekyc-es256-2026-v1", "PROD_SIGNING_P12_PATH_REQUIRED")]
    [InlineData("relative.p12", "env:irrelevant", "tagekyc-es256-2026-v1", "PROD_SIGNING_P12_PATH_NOT_ABSOLUTE")]
    [InlineData("missing", "env:irrelevant", "tagekyc-es256-2026-v1", "PROD_SIGNING_P12_FILE_NOT_FOUND")]
    [InlineData("valid", null, "tagekyc-es256-2026-v1", "PROD_SIGNING_P12_PASSWORD_SECRET_REQUIRED")]
    [InlineData("valid", "vault:irrelevant", "tagekyc-es256-2026-v1", "PROD_SIGNING_P12_PASSWORD_SECRET_REF_INVALID")]
    [InlineData("valid", "env:irrelevant", "localdev-es256-v1", "PROD_SIGNING_KEY_ID_DEV_FORBIDDEN")]
    [InlineData("valid", "env:irrelevant", "test-p12-es256-v1", "PROD_SIGNING_KEY_ID_INVALID")]
    public void ProductionTrialP12_fails_closed_for_invalid_required_config(
        string? pathKind,
        string? secretRef,
        string keyId,
        string expectedCode)
    {
        var p12Path = Path.Combine(Path.GetTempPath(), $"tagekyc-tip83a-{Guid.NewGuid():N}.p12");
        File.WriteAllBytes(p12Path, CreateP12("unused"));
        var configuredPath = pathKind switch
        {
            null => null,
            "relative.p12" => "relative.p12",
            "missing" => Path.Combine(Path.GetTempPath(), $"tagekyc-tip83a-missing-{Guid.NewGuid():N}.p12"),
            _ => p12Path,
        };

        try
        {
            using var factory = ProductionTrialFactory(configuredPath, secretRef, keyId, previousKeysPath: null);
            var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
            Assert.Contains(expectedCode, exception.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            TryDelete(p12Path);
        }
    }

    [Fact]
    public void ProductionTrialP12_rejects_plaintext_p12_password_config()
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("environment", "Production");
                builder.UseSetting("TagEkyc:Persistence:Provider", "Postgres");
                ConfigureProductionDb(builder, postgres.ConnectionString);
                ConfigureProductionApiKeyStore(builder);
                ConfigureRetention(builder);
                ConfigureDecisionThresholds(builder);
                builder.UseSetting("TagEkyc:EvidenceSigning:Backend", EvidenceSigningBackends.ProductionTrialP12);
                builder.UseSetting("TagEkyc:EvidenceSigning:P12Password", "plaintext-secret");
            });

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains("PROD_SIGNING_P12_PASSWORD_PLAINTEXT_FORBIDDEN", exception.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("plaintext-secret", exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProductionTrialP12_load_errors_are_sanitized()
    {
        var correctPassword = $"correct-{Guid.NewGuid():N}";
        var wrongPassword = $"wrong-{Guid.NewGuid():N}";
        var secretName = $"TAGEKYC_TIP83A_WRONG_PASSWORD_{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable(secretName, wrongPassword);
        var p12Path = Path.Combine(Path.GetTempPath(), $"tagekyc-tip83a-secret-path-{Guid.NewGuid():N}.p12");
        await File.WriteAllBytesAsync(p12Path, CreateP12(correctPassword));

        try
        {
            var exception = Assert.Throws<InvalidOperationException>(() =>
                new ProductionTrialP12Es256JwsEvidenceSigner(new ProductionTrialP12Es256JwsEvidenceSignerOptions
                {
                    P12Path = p12Path,
                    P12PasswordSecretRef = $"env:{secretName}",
                    KeyId = "tagekyc-es256-2026-v1",
                }));
            var rendered = exception.ToString();
            Assert.Contains("PROD_SIGNING_P12_LOAD_FAILED", rendered, StringComparison.Ordinal);
            Assert.DoesNotContain(p12Path, rendered, StringComparison.Ordinal);
            Assert.DoesNotContain(wrongPassword, rendered, StringComparison.Ordinal);
            Assert.DoesNotContain(correctPassword, rendered, StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable(secretName, null);
            TryDelete(p12Path);
        }
    }

    [Theory]
    [InlineData("private-d", "JWKS_PRIVATE_KEY_MATERIAL_FORBIDDEN")]
    [InlineData("duplicate-current", "JWKS_DUPLICATE_KID")]
    [InlineData("malformed-p256", "JWKS_PUBLIC_KEY_INVALID")]
    [InlineData("missing-validity", "JWKS_PREVIOUS_KEY_VALIDITY_REQUIRED")]
    public async Task Jwks_previous_key_registry_fails_closed_for_unsafe_entries(string caseName, string expectedCode)
    {
        var password = $"tip83a-password-{Guid.NewGuid():N}";
        var secretName = $"TAGEKYC_TIP83A_P12_PASSWORD_{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable(secretName, password);
        var p12Path = Path.Combine(Path.GetTempPath(), $"tagekyc-tip83a-{Guid.NewGuid():N}.p12");
        var previousKeysPath = Path.Combine(Path.GetTempPath(), $"tagekyc-tip83a-prev-{Guid.NewGuid():N}.json");
        await File.WriteAllBytesAsync(p12Path, CreateP12(password));

        using var previousSigner = new LocalDevEs256JwsEvidenceSigner(new LocalDevEs256JwsEvidenceSignerOptions
        {
            KeyId = caseName == "duplicate-current" ? "tagekyc-es256-2026-v1" : "tagekyc-es256-2025-v1",
        });
        await File.WriteAllTextAsync(previousKeysPath, PreviousKeysJson(previousSigner, caseName));

        try
        {
            using var factory = ProductionTrialFactory(p12Path, $"env:{secretName}", "tagekyc-es256-2026-v1", previousKeysPath);
            var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
            Assert.Contains(expectedCode, exception.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable(secretName, null);
            TryDelete(p12Path);
            TryDelete(previousKeysPath);
        }
    }

    [Fact]
    public void Appsettings_and_examples_do_not_contain_p12_password_or_private_jwk_material()
    {
        var root = FindRepositoryRoot();
        var files = new[]
        {
            Path.Combine(root, "src", "TagEkyc.Api", "appsettings.json"),
            Path.Combine(root, "src", "TagEkyc.Api", "appsettings.Development.json"),
            Path.Combine(root, "config", "appsettings.example.json"),
        };

        foreach (var file in files.Select(Path.GetFullPath))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("P12Password", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("\"d\"", text, StringComparison.Ordinal);
            Assert.DoesNotContain("PRIVATE KEY", text, StringComparison.OrdinalIgnoreCase);
        }
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

    private WebApplicationFactory<Program> ProductionTrialFactory(
        string? p12Path,
        string? secretRef,
        string keyId,
        string? previousKeysPath) =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("environment", "Production");
                builder.UseSetting("TagEkyc:Persistence:Provider", "Postgres");
                ConfigureProductionDb(builder, postgres.ConnectionString);
                ConfigureProductionApiKeyStore(builder);
                ConfigureRetention(builder);
                ConfigureDecisionThresholds(builder);
                builder.UseSetting("TagEkyc:EvidenceSigning:Backend", EvidenceSigningBackends.ProductionTrialP12);
                if (p12Path is not null)
                {
                    builder.UseSetting("TagEkyc:EvidenceSigning:P12Path", p12Path);
                }

                if (secretRef is not null)
                {
                    builder.UseSetting("TagEkyc:EvidenceSigning:P12PasswordSecretRef", secretRef);
                }

                builder.UseSetting("TagEkyc:EvidenceSigning:KeyId", keyId);
                if (previousKeysPath is not null)
                {
                    builder.UseSetting("TagEkyc:EvidenceSigning:Jwks:PreviousKeysFile", previousKeysPath);
                    builder.UseSetting("TagEkyc:EvidenceSigning:Jwks:PreviousKeyOverlapDays", "30");
                }
            });

    private static void ConfigureProductionDb(IWebHostBuilder builder, string connectionString)
    {
        var secretName = $"TAGEKYC_TIP83A_DB_{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable(secretName, connectionString);
        builder.UseSetting("TagEkyc:Persistence:ConnectionStringSecretRef", $"env:{secretName}");
    }

    private static void ConfigureProductionApiKeyStore(IWebHostBuilder builder)
    {
        builder.UseSetting("TagEkyc:ApiKeyStore:Backend", ApiKeyStoreBackends.Postgres);
        builder.UseSetting("TagEkyc:ApiKeyStore:PepperSecretRef", Tip84BTestSupport.PepperSecretRef());
    }

    private static void ConfigureRetention(IWebHostBuilder builder) =>
        builder.UseSetting("TagEkyc:Retention:RegulatedEvidenceRetentionDays", "30");

    private static void ConfigureDecisionThresholds(IWebHostBuilder builder)
    {
        builder.UseSetting("TagEkyc:DecisionThresholds:FaceMatch", "0.80");
        builder.UseSetting("TagEkyc:DecisionThresholds:Liveness", "0.80");
    }

    private static string PreviousKeysJson(IEs256PublicJwkSource signer, string caseName = "valid")
    {
        using var document = JsonDocument.Parse(signer.PublicKeyJwk);
        var root = document.RootElement;
        var x = root.GetProperty("x").GetString();
        var y = root.GetProperty("y").GetString();
        var fingerprint = signer.PublicKeyFingerprint;
        object key = caseName switch
        {
            "private-d" => new
            {
                kid = signer.KeyId,
                kty = "EC",
                crv = "P-256",
                x,
                y,
                use = "sig",
                alg = EvidenceSignatureDefaults.AlgorithmEs256,
                fingerprint,
                notAfter = DateTimeOffset.UtcNow.AddDays(30),
                d = "private-material",
            },
            "malformed-p256" => new
            {
                kid = signer.KeyId,
                kty = "EC",
                crv = "P-384",
                x,
                y,
                use = "sig",
                alg = EvidenceSignatureDefaults.AlgorithmEs256,
                fingerprint,
                notAfter = DateTimeOffset.UtcNow.AddDays(30),
            },
            "missing-validity" => new
            {
                kid = signer.KeyId,
                kty = "EC",
                crv = "P-256",
                x,
                y,
                use = "sig",
                alg = EvidenceSignatureDefaults.AlgorithmEs256,
                fingerprint,
            },
            _ => new
            {
                kid = signer.KeyId,
                kty = "EC",
                crv = "P-256",
                x,
                y,
                use = "sig",
                alg = EvidenceSignatureDefaults.AlgorithmEs256,
                fingerprint,
                notAfter = DateTimeOffset.UtcNow.AddDays(30),
            },
        };

        return JsonSerializer.Serialize(new { keys = new[] { key } });
    }

    private static bool VerifyJwsWithJwk(string jws, JsonElement jwk, string expectedKid)
    {
        var parts = jws.Split('.');
        if (parts.Length != 3)
        {
            return false;
        }

        using var headerDocument = JsonDocument.Parse(Encoding.UTF8.GetString(Base64UrlDecode(parts[0])));
        if (headerDocument.RootElement.GetProperty("kid").GetString() != expectedKid)
        {
            return false;
        }

        using var key = ECDsa.Create();
        key.ImportParameters(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint
            {
                X = Base64UrlDecode(jwk.GetProperty("x").GetString()!),
                Y = Base64UrlDecode(jwk.GetProperty("y").GetString()!),
            },
        });

        return key.VerifyData(
            Encoding.ASCII.GetBytes($"{parts[0]}.{parts[1]}"),
            Base64UrlDecode(parts[2]),
            HashAlgorithmName.SHA256,
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
    }

    private static EvidenceProofSignatureRequest ProofRequest() =>
        new(
            "11111111111151118111111111111111",
            EvidenceCanonicalization.PackageVersion,
            EvidenceCanonicalization.CanonicalizationScheme,
            EvidenceCanonicalization.HashAlgorithm,
            EvidenceSignatureDefaults.PurposeNeutralEkycProof,
            "22222222222252228222222222222222",
            "sha256:3333333333333333333333333333333333333333333333333333333333333333",
            "Passed",
            "High",
            ["DocumentNfc"],
            ["DocumentNfc"],
            [new EvidenceProofEngineRef("DocumentNfc", "bbbbbbbbbbbb5bbb8bbbbbbbbbbbbbbb", "nfc", "2", "DocumentNfc")],
            "opaque challenge: tip83a",
            "sha256:4444444444444444444444444444444444444444444444444444444444444444");

    private static byte[] CreateP12(string password)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest(
            "CN=tagekyc-tip83a-trial",
            key,
            HashAlgorithmName.SHA256);
        using var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow.AddDays(1));
        return certificate.Export(X509ContentType.Pkcs12, password);
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + ((4 - padded.Length % 4) % 4), '=');
        return Convert.FromBase64String(padded);
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch
        {
        }
    }
}
