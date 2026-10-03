using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Npgsql;
using TagEkyc.Api;
using TagEkyc.Application;
using TagEkyc.Application.CaptureRuntime;
using TagEkyc.Application.Ports;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Infrastructure.Auth;
using TagEkyc.Infrastructure.ProtectedValues;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.IntegrationTests;

public sealed class ProductionCompositionCorrectionTests
{
    [Fact]
    public void Production_api_composition_registers_existing_runtime_owners_without_fixture_fallback()
    {
        using var secrets = new SyntheticOwnerSecrets();
        var configuration = secrets.Configuration;

        using var provider = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddTagEkycCaptureRuntimeRawIngress(configuration, isProduction: true)
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });

        var owners = provider.GetRequiredService<CaptureRuntimeRawIngressComposition.RuntimeOwners>();
        Assert.Equal(1_048_576, owners.MaximumPlaintextWindowBytesPerStream);
        Assert.Equal(ProvisionalObjectCapability.Writer, owners.WriterObject.Capability);
        Assert.Equal(ProvisionalObjectCapability.Reconciler, owners.ReconcilerObject.Capability);
        Assert.Equal(ProvisionalObjectCapability.Lifecycle, owners.LifecycleObject.Capability);
        using var scope = provider.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICaptureRuntimeRawIngressAdmission>());
        Assert.NotNull(provider.GetRequiredService<ICaptureRuntimeA3Readiness>());
        Assert.NotNull(provider.GetRequiredService<ICaptureRuntimeA3Worker>());
    }

    [Fact]
    public void Missing_runtime_owner_configuration_keeps_prepared_graph_lazy()
    {
        var configuration = new ConfigurationManager();
        using var provider = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddTagEkycCaptureRuntimeRawIngress(configuration, isProduction: true)
            .BuildServiceProvider();

        Assert.Null(provider.GetService<CaptureRuntimeRawIngressComposition.RuntimeOwners>());
        using var scope = provider.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICaptureRuntimeRawIngressAdmission>());
        Assert.NotNull(provider.GetRequiredService<ICaptureRuntimeA3Readiness>());
        Assert.NotNull(provider.GetRequiredService<ICaptureRuntimeA3Worker>());
    }

    [Fact]
    public void Recipient_package_composition_registers_exact_three_role_factory()
    {
        var configuration = RecipientPackageConfiguration();
        using var provider = new ServiceCollection()
            .AddTagEkycRecipientPackage(configuration, isProduction: false)
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        var options = provider.GetRequiredService<RecipientPackageDatabaseOptions>();
        Assert.True(options.IsValid);
        using var scope = provider.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IRecipientPackageConnectionFactory>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<RecipientPackageRepository>());
    }

    [Fact]
    public void Development_broker_composition_registers_fixture_claim_services_and_exact_login()
    {
        var configuration = BrokerConfiguration();
        configuration[$"{RawIngressBrokerOptions.SectionName}:DatabaseConnectionString"] =
            Connection(QualifiedRawIngressBroker.Login);
        configuration["TagEkyc:RawExport:SubjectRefToken:FixtureKeys:fixture-subject-ref-token:1"] =
            "abcdef0123456789abcdef0123456789";
        configuration["TagEkyc:RawExport:ContentCommitment:FixtureKeys:fixture-content-commitment:1"] =
            "0123456789abcdef0123456789abcdef";

        using var provider = new ServiceCollection()
            .AddTagEkycRawIngressBroker(configuration, isProduction: false)
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });

        var database = provider.GetRequiredService<RawIngressBrokerDatabaseOptions>();
        Assert.True(database.IsValid);
        Assert.Equal(QualifiedRawIngressBroker.Login,
            new NpgsqlConnectionStringBuilder(database.ConnectionString).Username);
        Assert.NotNull(provider.GetRequiredService<NpgsqlDataSource>());
        using var scope = provider.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IRawIngressMetadataBroker>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<RawIngressBrokerTransport>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IContentCommitmentService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ISubjectRefTokenService>());
    }

    [Fact]
    public async Task Production_claim_registration_contains_only_OpenBao_providers_and_missing_catalog_is_readiness_RED()
    {
        var configuration = BrokerConfiguration();
        var services = new ServiceCollection();
        services.AddTagEkycProductionRawExportClaimProviders(configuration);
        using var provider = services.BuildServiceProvider();

        Assert.IsType<OpenBaoContentCommitmentService>(
            provider.GetRequiredService<IContentCommitmentService>());
        Assert.IsType<OpenBaoSubjectRefTokenService>(
            provider.GetRequiredService<ISubjectRefTokenService>());
        Assert.Null(provider.GetService<FixtureContentCommitmentCatalog>());
        Assert.Null(provider.GetService<FixtureSubjectTokenCatalog>());
        var failure = await Assert.ThrowsAsync<RawExportClaimProviderReadinessException>(() =>
            provider.GetRequiredService<RawExportClaimProviderReadinessValidator>()
                .ValidateAsync(CancellationToken.None));
        Assert.Equal(RawExportClaimProviderReadinessValidator.ConfigurationInvalid,
            failure.Code);
    }

    [Fact]
    public void Production_broker_with_fixture_content_commitment_fails_closed_at_commitment_arm()
    {
        var error = ProductionBrokerError((services, configuration) =>
            services.AddTagEkycContentCommitment(configuration));

        Assert.Equal("PROD_RAW_EXPORT_CONTENT_COMMITMENT_FIXTURE_ACTIVE",
            error.Message);
    }

    [Fact]
    public void Production_broker_with_fixture_subject_token_fails_closed_at_subject_arm()
    {
        var error = ProductionBrokerError((services, configuration) =>
            services.AddTagEkycSubjectRefToken(configuration));

        Assert.Equal("PROD_RAW_EXPORT_SUBJECT_REF_TOKEN_FIXTURE_ACTIVE",
            error.Message);
    }

    [Theory]
    [InlineData("content")]
    [InlineData("subject")]
    public void Production_broker_rejects_pre_registered_unknown_claim_provider(
        string arm)
    {
        var error = ProductionBrokerError((services, _) =>
        {
            if (arm == "content")
                services.AddSingleton<IContentCommitmentService,
                    UnknownContentCommitmentService>();
            else
                services.AddSingleton<ISubjectRefTokenService,
                    UnknownSubjectRefTokenService>();
        });

        Assert.Equal(RawExportClaimProviderProductionGuard.ProvidersMissing,
            error.Message);
    }

    [Fact]
    public void Production_broker_with_fixture_custody_profile_fails_closed_at_custody_arm()
    {
        var error = ProductionBrokerError((services, configuration) =>
            services.AddTagEkycCustodyProfiles(configuration));

        Assert.Equal("PROD_RAW_EXPORT_CUSTODY_PROFILE_FIXTURE_ACTIVE",
            error.Message);
    }

    [Fact]
    public async Task Production_custody_registration_excludes_fixtures_while_readiness_rejects_fixture_profile()
    {
        var configuration = BrokerConfiguration();
        configuration[RawExportCustodyProfileState.ConfigurationPath] = "Fixture";
        var services = new ServiceCollection();

        var returned = services.AddTagEkycCustodyProfiles(
            configuration, isProduction: true);

        Assert.Same(services, returned);
        using var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<CustodyTimeBoundsState>());
        Assert.Null(provider.GetService<FixtureSourceEncryptionProfileCatalog>());
        Assert.Null(provider.GetService<FixtureKekReferenceCatalog>());
        Assert.Null(provider.GetService<FixtureCustodyProfileProvider>());
        Assert.Null(provider.GetService<ICustodyProfileProvider>());

        var validator = new RawExportCustodyProfileReadinessValidator(
            configuration, isProduction: true);
        var error = await Assert.ThrowsAsync<RawExportCustodyProfileReadinessException>(
            () => validator.ValidateAsync(CancellationToken.None));
        Assert.Equal(RawExportCustodyProfileReadinessValidator.FixtureActive,
            error.Code);
    }

    [Fact]
    public void Development_custody_registration_keeps_fixture_catalogs_and_provider()
    {
        var services = new ServiceCollection();
        services.AddTagEkycCustodyProfiles(BrokerConfiguration(), isProduction: false);

        using var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<CustodyTimeBoundsState>());
        Assert.NotNull(provider.GetRequiredService<FixtureSourceEncryptionProfileCatalog>());
        Assert.NotNull(provider.GetRequiredService<FixtureKekReferenceCatalog>());
        Assert.IsType<FixtureCustodyProfileProvider>(
            provider.GetRequiredService<ICustodyProfileProvider>());
    }

    [Fact]
    public async Task Production_program_reaches_build_and_serves_health_with_fixture_profile_fail_closed_in_readiness()
    {
        var databaseSecret = $"TAGEKYC_PRODUCTION_COMPOSITION_DB_{Guid.NewGuid():N}";
        var pepperSecret = $"TAGEKYC_PRODUCTION_COMPOSITION_PEPPER_{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable(databaseSecret,
            "Host=127.0.0.1;Port=1;Database=unused;Username=unused;Password=synthetic-only;Timeout=1");
        Environment.SetEnvironmentVariable(pepperSecret,
            Convert.ToBase64String(new byte[32]));

        using var factory = new HistoricalPreparedWebApplicationFactory()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("environment", "Production");
                builder.UseSetting("TagEkyc:Persistence:Provider", "Postgres");
                builder.UseSetting("TagEkyc:Persistence:ConnectionStringSecretRef",
                    $"env:{databaseSecret}");
                builder.UseSetting("TagEkyc:ApiKeyStore:Backend", "Postgres");
                builder.UseSetting("TagEkyc:ApiKeyStore:PepperSecretRef",
                    $"env:{pepperSecret}");
                builder.UseSetting("TagEkyc:EvidenceSigning:Backend", "Pkcs11");
                builder.UseSetting("TagEkyc:Retention:RegulatedEvidenceRetentionDays", "30");
                builder.UseSetting("TagEkyc:DecisionThresholds:FaceMatch", "0.80");
                builder.UseSetting("TagEkyc:DecisionThresholds:Liveness", "0.80");
                builder.UseSetting(RawExportCustodyProfileState.ConfigurationPath, "Fixture");
                foreach (var pair in BrokerConfiguration().AsEnumerable())
                    if (pair.Value is not null)
                        builder.UseSetting(pair.Key, pair.Value);
                builder.ConfigureTestServices(services =>
                    services.RemoveAll<IHostedService>());
            });

        using var client = factory.CreateClient();
        using var health = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        Assert.Null(factory.Services.GetService<FixtureSourceEncryptionProfileCatalog>());
        Assert.Null(factory.Services.GetService<FixtureKekReferenceCatalog>());
        Assert.Null(factory.Services.GetService<FixtureCustodyProfileProvider>());
        Assert.Null(factory.Services.GetService<ICustodyProfileProvider>());
        var validator = factory.Services
            .GetRequiredService<RawExportCustodyProfileReadinessValidator>();
        var error = await Assert.ThrowsAsync<RawExportCustodyProfileReadinessException>(
            () => validator.ValidateAsync(CancellationToken.None));
        Assert.Equal(RawExportCustodyProfileReadinessValidator.FixtureActive,
            error.Code);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("malformed")]
    [InlineData("unreadable-directory")]
    public async Task Production_readiness_maps_bad_claim_provider_CA_to_stable_503_code(
        string arm)
    {
        var databaseSecret = $"TAGEKYC_CLAIM_CA_DB_{Guid.NewGuid():N}";
        var pepperSecret = $"TAGEKYC_CLAIM_CA_PEPPER_{Guid.NewGuid():N}";
        var candidateCa = arm == "unreadable-directory"
            ? Path.GetTempPath()
            : Path.Combine(Path.GetTempPath(), $"claim-ca-{Guid.NewGuid():N}.pem");
        if (arm == "malformed")
            await File.WriteAllTextAsync(candidateCa, "not-a-certificate");
        Environment.SetEnvironmentVariable(databaseSecret,
            "Host=127.0.0.1;Port=1;Database=unused;Username=unused;Password=synthetic-only;Timeout=1");
        Environment.SetEnvironmentVariable(pepperSecret,
            Convert.ToBase64String(new byte[32]));
        var claimConfiguration = OpenBaoProductionClaimProviderTests.Configuration();
        claimConfiguration[$"{OpenBaoProductionClaimProviderTests.Root}:ContentCommitment:CaCertificatePath"] = candidateCa;
        claimConfiguration[$"{OpenBaoProductionClaimProviderTests.Root}:SubjectRefToken:CaCertificatePath"] = candidateCa;

        using var factory = new HistoricalPreparedWebApplicationFactory()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("environment", "Production");
                builder.UseSetting("TagEkyc:Persistence:Provider", "Postgres");
                builder.UseSetting("TagEkyc:Persistence:ConnectionStringSecretRef",
                    $"env:{databaseSecret}");
                builder.UseSetting("TagEkyc:ApiKeyStore:Backend", "Postgres");
                builder.UseSetting("TagEkyc:ApiKeyStore:PepperSecretRef",
                    $"env:{pepperSecret}");
                builder.UseSetting("TagEkyc:EvidenceSigning:Backend", "Pkcs11");
                builder.UseSetting("TagEkyc:Retention:RegulatedEvidenceRetentionDays", "30");
                builder.UseSetting("TagEkyc:DecisionThresholds:FaceMatch", "0.80");
                builder.UseSetting("TagEkyc:DecisionThresholds:Liveness", "0.80");
                builder.UseSetting(RawExportCustodyProfileState.ConfigurationPath, "Fixture");
                foreach (var pair in claimConfiguration.AsEnumerable())
                    if (pair.Value is not null)
                        builder.UseSetting(pair.Key, pair.Value);
                foreach (var pair in BrokerConfiguration().AsEnumerable())
                    if (pair.Value is not null
                        && !pair.Key.EndsWith("SelectorId", StringComparison.Ordinal)
                        && !pair.Key.EndsWith("SelectorVersion", StringComparison.Ordinal))
                        builder.UseSetting(pair.Key, pair.Value);
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IHostedService>();
                    services.RemoveAll<IReadinessCheck>();
                    var readinessType = typeof(Program).Assembly.GetType(
                        "TagEkyc.Api.RawExportClaimProviderReadinessCheck",
                        throwOnError: true)!;
                    services.AddScoped(typeof(IReadinessCheck), readinessType);
                });
            });

        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/readiness");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains(RawExportClaimProviderReadinessValidator.ProviderInvalid,
            body, StringComparison.Ordinal);
        Assert.DoesNotContain(nameof(FileNotFoundException), body, StringComparison.Ordinal);
        Assert.DoesNotContain(nameof(System.Security.Cryptography.CryptographicException),
            body, StringComparison.Ordinal);
        Assert.DoesNotContain(nameof(UnauthorizedAccessException), body,
            StringComparison.Ordinal);

        if (arm == "malformed")
            File.Delete(candidateCa);
    }

    [Theory]
    [InlineData(AuthenticatedCallerCategory.OperatorAdmin,
        "operator.site-qualification.enroll", true)]
    [InlineData(AuthenticatedCallerCategory.CaptureAgent,
        "capture.raw-export.site-qualification", true)]
    [InlineData(AuthenticatedCallerCategory.OperatorAdmin,
        "capture.raw-export.site-qualification", false)]
    [InlineData(AuthenticatedCallerCategory.CaptureAgent,
        "operator.site-qualification.enroll", false)]
    public void Provisioning_category_scope_boundary_covers_exact_site_credentials(
        AuthenticatedCallerCategory category,
        string scope,
        bool expected)
    {
        Assert.Equal(expected, ApiKeyProvisioningService.ScopesMatchCategory(
            category, new HashSet<string>(StringComparer.Ordinal) { scope }));
    }

    [Fact]
    public void Existing_managed_key_generator_always_round_trips_fixed_length_prefix()
    {
        var generator = new RandomManagedApiKeyGenerator();
        for (var index = 0; index < 512; index++)
        {
            var material = generator.Generate();
            var parsed = ManagedApiKeyParser.Parse(material.PresentedKey);
            Assert.NotNull(parsed);
            Assert.Equal(material.Prefix, parsed.Prefix);
        }
    }

    [Fact]
    public void Production_runtime_owner_composition_rejects_direct_connection_strings()
    {
        var configuration = BrokerConfiguration();
        configuration["TagEkyc:RawExport:RawExportCustodyMaximumPlaintextWindowBytesPerStream"] = "1048576";
        AddOwner(configuration, "Writer", CaptureRuntimeCustodyProviderScopes.WriterLogin,
            ProvisionalObjectCapability.Writer, "writer-access");
        AddOwner(configuration, "Reconciler", CaptureRuntimeCustodyProviderScopes.ReconcilerLogin,
            ProvisionalObjectCapability.Reconciler, "reconciler-access");
        AddOwner(configuration, "Lifecycle", CaptureRuntimeCustodyProviderScopes.LifecycleLogin,
            ProvisionalObjectCapability.Lifecycle, "lifecycle-access");

        var error = Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddTagEkycCaptureRuntimeRawIngress(
                configuration, isProduction: true));
        Assert.Equal(CaptureRuntimeRawIngressProductionOptions.InvalidCode, error.Message);
    }

    [Fact]
    public void Production_recipient_factory_rejects_direct_connection_strings()
    {
        var configuration = RecipientPackageConfiguration();
        using var provider = new ServiceCollection()
            .AddTagEkycRecipientPackage(configuration, isProduction: true)
            .BuildServiceProvider();

        Assert.False(provider.GetRequiredService<RecipientPackageDatabaseOptions>().IsValid);
    }

    [Fact]
    public void Production_broker_database_options_reject_direct_connection_string()
    {
        var configuration = BrokerConfiguration();
        configuration[$"{RawIngressBrokerOptions.SectionName}:DatabaseConnectionString"] =
            Connection(QualifiedRawIngressBroker.Login);

        var options = RawIngressBrokerDatabaseOptions.Resolve(
            configuration, isProduction: true);

        Assert.False(options.IsValid);
        Assert.Null(options.ConnectionString);
    }

    private static InvalidOperationException ProductionBrokerError(
        Action<IServiceCollection, IConfiguration> arrange)
    {
        var configuration = BrokerConfiguration();
        var services = new ServiceCollection();
        arrange(services, configuration);
        return Assert.Throws<InvalidOperationException>(() =>
            services.AddTagEkycRawIngressBroker(configuration, isProduction: true));
    }

    private sealed class UnknownContentCommitmentService : IContentCommitmentService
    {
        public ValueTask<ContentCommitmentResult> ComputeAsync(
            CommitmentKeySelector selector,
            ReadOnlyMemory<byte> lpPayload,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult(ContentCommitmentResult.Failed(
                ContentCommitmentFailure.ProviderFailure));
    }

    private sealed class UnknownSubjectRefTokenService : ISubjectRefTokenService
    {
        public ValueTask<SubjectRefTokenResult> ComputeAsync(
            SubjectTokenKeySelector selector,
            ReadOnlyMemory<byte> lpPayload,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult(SubjectRefTokenResult.Failed(
                SubjectRefTokenFailure.ProviderFailure));
    }

    private static ConfigurationManager BrokerConfiguration(int port = 45679) => new()
    {
        [$"{RawIngressBrokerOptions.SectionName}:BaseUri"] = $"http://127.0.0.1:{port}/",
        [$"{RawIngressBrokerOptions.SectionName}:ListenAddress"] = "127.0.0.1",
        [$"{RawIngressBrokerOptions.SectionName}:AllowedApiAddresses:0"] = "127.0.0.1",
        [$"{RawIngressBrokerOptions.SectionName}:RequestTimeoutMilliseconds"] = "1000",
        [$"{RawIngressBrokerOptions.SectionName}:EvaluationTokenTtlSeconds"] = "10",
        [$"{RawIngressBrokerOptions.SectionName}:IdempotencyLockTimeoutMilliseconds"] = "100",
        [$"{RawIngressBrokerOptions.SectionName}:EvaluationOwnerId"] =
            "8bf2e6a5425a42689647465139fc46b0",
        [$"{RawIngressBrokerOptions.SectionName}:CommitmentSelectorId"] =
            "fixture-content-commitment",
        [$"{RawIngressBrokerOptions.SectionName}:CommitmentSelectorVersion"] = "1",
        [$"{RawIngressBrokerOptions.SectionName}:SubjectTokenSelectorId"] =
            "fixture-subject-ref-token",
        [$"{RawIngressBrokerOptions.SectionName}:SubjectTokenSelectorVersion"] = "1",
        [$"{RawIngressBrokerOptions.SectionName}:ApiReplicaCount"] = "1",
        [$"{RawIngressBrokerOptions.SectionName}:ContinuationPollIntervalMilliseconds"] = "100",
        [CustodyTimeBoundsState.SafetyMarginKey] = "2000",
        [CustodyTimeBoundsState.MaxRemainingContinuationWindowKey] = "1800",
        [CustodyTimeBoundsState.EncryptionAttemptDeadlineKey] = "900",
        [CustodyTimeBoundsState.OwnershipLeaseDurationKey] = "300",
    };

    private static void AddOwner(
        ConfigurationManager configuration,
        string name,
        string login,
        ProvisionalObjectCapability capability,
        string accessKey)
    {
        var prefix = $"{CaptureRuntimeRawIngressProductionOptions.SectionPath}:{name}:";
        configuration[prefix + "DatabaseConnectionString"] = Connection(login);
        prefix += "ObjectCustody:";
        configuration[prefix + "Topology"] = "S3CompatibleDurable";
        configuration[prefix + "Capability"] = capability.ToString();
        configuration[prefix + "ServiceUrl"] = "http://127.0.0.1:9000/";
        configuration[prefix + "BucketName"] = "tagekyc-raw-source";
        configuration[prefix + "AccessKeyId"] = accessKey;
        configuration[prefix + "SecretAccessKey"] = "fixture-secret";
        configuration[prefix + "AllowLoopbackHttp"] = "true";
        configuration[prefix + "MaximumSinglePartCiphertextBytes"] = "134217728";
        configuration[prefix + "OperationTimeoutSeconds"] = "300";
    }

    private static ConfigurationManager RecipientPackageConfiguration()
    {
        const string root = RecipientPackageOptions.SectionPath + ":";
        var configuration = new ConfigurationManager
        {
            [root + "Topology"] = "S3CompatibleDurable",
            [root + "ProviderConfigurationId"] = "lab-minio",
            [root + "Providers:lab-minio:ServiceUrl"] = "http://127.0.0.1:9000/",
            [root + "Providers:lab-minio:BucketName"] = "tagekyc-recipient-package",
            [root + "Providers:lab-minio:ForcePathStyle"] = "true",
            [root + "Providers:lab-minio:RegionIdentifier"] = "lab-1",
            [root + "Providers:lab-minio:AllowLoopbackHttp"] = "true",
        };
        var credentials = new[] { "Writer", "Reconciler", "Lifecycle", "PostureProbe" };
        for (var index = 0; index < credentials.Length; index++)
        {
            configuration[$"{root}Providers:lab-minio:{credentials[index]}:AccessKeyId"] =
                $"package-{credentials[index].ToLowerInvariant()}";
            configuration[$"{root}Providers:lab-minio:{credentials[index]}:SecretAccessKey"] =
                $"fixture-secret-{index}";
        }
        var database = RecipientPackageDatabaseOptions.SectionPath + ":";
        configuration[database + "PreparerConnectionString"] =
            Connection(RecipientPackageDatabaseOptions.PreparerLogin);
        configuration[database + "ReconcilerConnectionString"] =
            Connection(RecipientPackageDatabaseOptions.ReconcilerLogin);
        configuration[database + "LifecycleConnectionString"] =
            Connection(RecipientPackageDatabaseOptions.LifecycleLogin);
        return configuration;
    }

    private static string Connection(string username) =>
        new NpgsqlConnectionStringBuilder
        {
            Host = "127.0.0.1",
            Port = 5432,
            Database = "tagekyc",
            Username = username,
            Password = "fixture-password",
            Pooling = false,
        }.ConnectionString;
}

public sealed class ProductionCompositionLabProofTests
{
    [Fact]
    public async Task Lab_runtime_owners_open_exact_existing_role_connections()
    {
        var configuration = BrokerConfiguration();
        configuration["TagEkyc:RawExport:RawExportCustodyMaximumPlaintextWindowBytesPerStream"] = "1048576";
        AddOwner(configuration, "Writer", "Writer", Required("TAGEKYC_LAB_CUSTODY_WRITER_SECRET_REF"));
        AddOwner(configuration, "Reconciler", "Reconciler", Required("TAGEKYC_LAB_CUSTODY_RECONCILER_SECRET_REF"));
        AddOwner(configuration, "Lifecycle", "Lifecycle", Required("TAGEKYC_LAB_CUSTODY_LIFECYCLE_SECRET_REF"));

        using var provider = new ServiceCollection()
            .AddTagEkycCaptureRuntimeRawIngress(configuration, isProduction: true)
            .BuildServiceProvider();
        var owners = provider.GetRequiredService<CaptureRuntimeRawIngressComposition.RuntimeOwners>();
        await AssertRoleAsync(owners.WriterConnectionString,
            CaptureRuntimeCustodyProviderScopes.WriterLogin);
        await AssertRoleAsync(owners.ReconcilerConnectionString,
            CaptureRuntimeCustodyProviderScopes.ReconcilerLogin);
        await AssertRoleAsync(owners.LifecycleConnectionString,
            CaptureRuntimeCustodyProviderScopes.LifecycleLogin);
    }

    [Fact]
    public async Task Lab_recipient_factory_opens_exact_three_production_roles()
    {
        var configuration = RecipientConfiguration();
        var database = RecipientPackageDatabaseOptions.SectionPath + ":";
        configuration[database + "PreparerConnectionStringSecretRef"] =
            Required("TAGEKYC_LAB_PACKAGE_PREPARER_SECRET_REF");
        configuration[database + "ReconcilerConnectionStringSecretRef"] =
            Required("TAGEKYC_LAB_PACKAGE_RECONCILER_SECRET_REF");
        configuration[database + "LifecycleConnectionStringSecretRef"] =
            Required("TAGEKYC_LAB_PACKAGE_LIFECYCLE_SECRET_REF");

        using var provider = new ServiceCollection()
            .AddTagEkycRecipientPackage(configuration, isProduction: true)
            .BuildServiceProvider();
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IRecipientPackageConnectionFactory>();
        foreach (var capability in Enum.GetValues<RecipientPackageDatabaseCapability>())
            await using (await factory.OpenAsync(capability, CancellationToken.None)) { }
    }

    [Fact]
    public async Task Lab_broker_database_uses_exact_login_while_missing_custody_profile_fails_closed()
    {
        var configuration = BrokerConfiguration();
        configuration[$"{RawIngressBrokerOptions.SectionName}:DatabaseConnectionStringSecretRef"] =
            Required("TAGEKYC_LAB_BROKER_SECRET_REF");

        var database = RawIngressBrokerDatabaseOptions.Resolve(
            configuration, isProduction: true);
        Assert.True(database.IsValid);
        await AssertRoleAsync(database.ConnectionString!, QualifiedRawIngressBroker.Login);

        var error = Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddTagEkycRawIngressBroker(
                configuration, isProduction: true));
        Assert.Equal(RawExportCustodyProfileReadinessValidator.ProfileMissing,
            error.Message);
    }

    private static async Task AssertRoleAsync(string connectionString, string expected)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await AssertRoleAsync(connection, expected);
    }

    private static async Task AssertRoleAsync(NpgsqlConnection connection, string expected)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT session_user, current_user";
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(expected, reader.GetString(0));
        Assert.Equal(expected, reader.GetString(1));
        Assert.False(await reader.ReadAsync());
    }

    private static ConfigurationManager BrokerConfiguration() => new()
    {
        [$"{RawIngressBrokerOptions.SectionName}:BaseUri"] = "http://127.0.0.1:18081/",
        [$"{RawIngressBrokerOptions.SectionName}:ListenAddress"] = "127.0.0.1",
        [$"{RawIngressBrokerOptions.SectionName}:AllowedApiAddresses:0"] = "127.0.0.1",
        [$"{RawIngressBrokerOptions.SectionName}:RequestTimeoutMilliseconds"] = "1000",
        [$"{RawIngressBrokerOptions.SectionName}:EvaluationTokenTtlSeconds"] = "10",
        [$"{RawIngressBrokerOptions.SectionName}:IdempotencyLockTimeoutMilliseconds"] = "100",
        [$"{RawIngressBrokerOptions.SectionName}:EvaluationOwnerId"] = "8bf2e6a5425a42689647465139fc46b0",
        [$"{RawIngressBrokerOptions.SectionName}:CommitmentSelectorId"] = "fixture-content-commitment",
        [$"{RawIngressBrokerOptions.SectionName}:CommitmentSelectorVersion"] = "1",
        [$"{RawIngressBrokerOptions.SectionName}:SubjectTokenSelectorId"] = "fixture-subject-ref-token",
        [$"{RawIngressBrokerOptions.SectionName}:SubjectTokenSelectorVersion"] = "1",
        [$"{RawIngressBrokerOptions.SectionName}:ApiReplicaCount"] = "1",
        [$"{RawIngressBrokerOptions.SectionName}:ContinuationPollIntervalMilliseconds"] = "100",
        [CustodyTimeBoundsState.SafetyMarginKey] = "2000",
        [CustodyTimeBoundsState.MaxRemainingContinuationWindowKey] = "1800",
        [CustodyTimeBoundsState.EncryptionAttemptDeadlineKey] = "900",
        [CustodyTimeBoundsState.OwnershipLeaseDurationKey] = "300",
    };

    private static void AddOwner(ConfigurationManager configuration, string name,
        string capability, string connectionSecretRef)
    {
        var prefix = $"{CaptureRuntimeRawIngressProductionOptions.SectionPath}:{name}:";
        configuration[prefix + "DatabaseConnectionStringSecretRef"] = connectionSecretRef;
        prefix += "ObjectCustody:";
        configuration[prefix + "Topology"] = "S3CompatibleDurable";
        configuration[prefix + "Capability"] = capability;
        configuration[prefix + "ServiceUrl"] = "http://127.0.0.1:9000/";
        configuration[prefix + "BucketName"] = "tagekyc-raw-source";
        configuration[prefix + "AccessKeyId"] = $"lab-{name.ToLowerInvariant()}";
        configuration[prefix + "SecretAccessKey"] = "lab-proof-not-used";
        configuration[prefix + "AllowLoopbackHttp"] = "true";
        configuration[prefix + "MaximumSinglePartCiphertextBytes"] = "134217728";
        configuration[prefix + "OperationTimeoutSeconds"] = "300";
    }

    private static ConfigurationManager RecipientConfiguration()
    {
        const string root = RecipientPackageOptions.SectionPath + ":";
        var configuration = new ConfigurationManager
        {
            [root + "Topology"] = "S3CompatibleDurable",
            [root + "ProviderConfigurationId"] = "lab-minio",
            [root + "Providers:lab-minio:ServiceUrl"] = "http://127.0.0.1:9000/",
            [root + "Providers:lab-minio:BucketName"] = "tagekyc-recipient-package",
            [root + "Providers:lab-minio:ForcePathStyle"] = "true",
            [root + "Providers:lab-minio:RegionIdentifier"] = "lab-1",
            [root + "Providers:lab-minio:AllowLoopbackHttp"] = "true",
        };
        foreach (var credential in new[] { "Writer", "Reconciler", "Lifecycle", "PostureProbe" })
        {
            configuration[$"{root}Providers:lab-minio:{credential}:AccessKeyId"] =
                $"package-{credential.ToLowerInvariant()}";
            configuration[$"{root}Providers:lab-minio:{credential}:SecretAccessKey"] =
                "lab-proof-not-used";
        }
        return configuration;
    }

    private static string Required(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"LAB_PROOF_ENVIRONMENT_MISSING:{name}");
}
