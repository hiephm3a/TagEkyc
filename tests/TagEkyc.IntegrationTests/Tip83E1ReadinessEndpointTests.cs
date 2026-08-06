using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
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

public sealed class Tip83E1ReadinessEndpointTests
{
    private const int PostureOrder = 0;
    private const int DatabaseOrder = 1;
    private const int ApiKeyOrder = 2;
    private const int SignerOrder = 3;
    private const int GenericOrder = 4;

    [Fact]
    public async Task Readiness_returns_ready_with_no_store_headers_when_all_checks_pass()
    {
        using var factory = ProductionReadinessFactory(new FakeReadinessCheck(_ => Task.FromResult<IReadOnlyList<ReadinessIssue>>([])));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/readiness");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"status\":\"ready\"", body, StringComparison.Ordinal);
        Assert.Contains("no-store", response.Headers.CacheControl?.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains(response.Headers.Pragma, value => value.Name.Equals("no-cache", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Readiness_reports_all_failure_codes_distinct_and_ordered()
    {
        using var factory = ProductionReadinessFactory(
            new FakeReadinessCheck(_ => Task.FromResult<IReadOnlyList<ReadinessIssue>>([
                new(GenericOrder, ReadinessEndpoint.GenericFailureCode),
                new(DatabaseOrder, PostgresProductionReadinessValidator.Unreachable),
            ])),
            new FakeReadinessCheck(_ => Task.FromResult<IReadOnlyList<ReadinessIssue>>([
                new(PostureOrder, "PROD_PERSISTENCE_INMEMORY_FORBIDDEN"),
                new(ApiKeyOrder, ApiKeyStoreProductionReadinessValidator.NoActiveKeys),
                new(SignerOrder, "JWKS_PUBLIC_KEY_INVALID"),
                new(GenericOrder, ReadinessEndpoint.GenericFailureCode),
            ])));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/readiness");
        var codes = await ReadCodesAsync(response);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal([
            "PROD_PERSISTENCE_INMEMORY_FORBIDDEN",
            PostgresProductionReadinessValidator.Unreachable,
            ApiKeyStoreProductionReadinessValidator.NoActiveKeys,
            "JWKS_PUBLIC_KEY_INVALID",
            ReadinessEndpoint.GenericFailureCode,
        ], codes);
    }

    [Fact]
    public async Task Readiness_maps_untyped_exceptions_to_generic_without_leaking_message()
    {
        const string secretMessage = "Host=private-db;Password=secret;Path=C:\\private\\key.p12";
        using var factory = ProductionReadinessFactory(new FakeReadinessCheck(_ => throw new InvalidOperationException(secretMessage)));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/readiness");
        var body = await response.Content.ReadAsStringAsync();
        var codes = CodesFromBody(body);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal([ReadinessEndpoint.GenericFailureCode], codes);
        Assert.DoesNotContain(secretMessage, body, StringComparison.Ordinal);
        Assert.DoesNotContain("private-db", body, StringComparison.Ordinal);
        Assert.DoesNotContain("secret", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("private\\key.p12", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Readiness_times_out_hung_async_checks_without_hanging_or_leaking()
    {
        using var factory = ProductionReadinessFactory(new FakeReadinessCheck(_ =>
            Task.Delay(TimeSpan.FromSeconds(30)).ContinueWith<IReadOnlyList<ReadinessIssue>>(_ => [], TaskScheduler.Default)));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/readiness");
        var codes = await ReadCodesAsync(response);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal([ReadinessEndpoint.GenericFailureCode], codes);
    }

    [Fact]
    public async Task Health_stays_liveness_only_when_readiness_fails()
    {
        using var factory = ProductionReadinessFactory(new FakeReadinessCheck(_ =>
            Task.FromResult<IReadOnlyList<ReadinessIssue>>([new(DatabaseOrder, PostgresProductionReadinessValidator.Unreachable)])));
        using var client = factory.CreateClient();

        var health = await client.GetAsync("/health");
        var readiness = await client.GetAsync("/readiness");

        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, readiness.StatusCode);
    }

    [Fact]
    public async Task Non_production_readiness_returns_ready_without_running_checks()
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("environment", "Development");
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped<IReadinessCheck>(_ => new FakeReadinessCheck(_ =>
                        Task.FromResult<IReadOnlyList<ReadinessIssue>>([new(DatabaseOrder, PostgresProductionReadinessValidator.Unreachable)])));
                });
            });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/readiness");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"status\":\"ready\"", body, StringComparison.Ordinal);
        Assert.DoesNotContain("codes", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Signer_jwks_check_surfaces_only_allowlisted_codes()
    {
        var known = new SignerJwksReadinessCheck(
            new ThrowingStartupSigner("JWKS_PUBLIC_KEY_INVALID"),
            new NoopJwksProvider());
        var unknown = new SignerJwksReadinessCheck(
            new ThrowingStartupSigner("Path=C:\\secret\\signer.p12"),
            new NoopJwksProvider());

        var knownIssues = await known.CheckAsync(CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(() => unknown.CheckAsync(CancellationToken.None));

        Assert.Equal([new ReadinessIssue(SignerOrder, "JWKS_PUBLIC_KEY_INVALID")], knownIssues);
    }

    [Fact]
    public async Task Production_object_custody_host_matrix_is_fail_closed_without_removing_real_checks()
    {
        using (var disabled = ProductionObjectCustodyFactory("Disabled"))
        using (var client = disabled.CreateClient())
        {
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
            Assert.False(disabled.Services.GetRequiredService<ObjectCustodyRegistrationSnapshot>()
                .HasReadinessWrapper);
            Assert.Null(disabled.Services.GetService<ProvisionalObjectCustodyReadinessValidator>());
        }

        foreach (var topology in new string?[] { null, "UnknownTopology" })
        {
            using var invalid = ProductionObjectCustodyFactory(topology);
            using var client = invalid.CreateClient();
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
            using var scope = invalid.Services.CreateScope();
            Assert.True(invalid.Services.GetRequiredService<ObjectCustodyRegistrationSnapshot>()
                .HasReadinessWrapper);
            var validator = scope.ServiceProvider
                .GetRequiredService<ProvisionalObjectCustodyReadinessValidator>();
            var wrapper = new ProvisionalObjectCustodyReadinessCheck(validator);
            var issues = await wrapper.CheckAsync(CancellationToken.None);
            Assert.Equal([new ReadinessIssue(DatabaseOrder,
                "PROD_RAW_EXPORT_OBJECT_TOPOLOGY_INVALID")], issues);
            Assert.Null(scope.ServiceProvider.GetService<ProvisionalObjectCustodyRepository>());
        }

        using (var durable = ProductionObjectCustodyFactory("S3CompatibleDurable", durable: true))
        using (var client = durable.CreateClient())
        {
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
            using var scope = durable.Services.CreateScope();
            Assert.True(durable.Services.GetRequiredService<ObjectCustodyRegistrationSnapshot>()
                .HasReadinessWrapper);
            var validator = scope.ServiceProvider
                .GetRequiredService<ProvisionalObjectCustodyReadinessValidator>();
            var wrapper = new ProvisionalObjectCustodyReadinessCheck(validator);
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<ProvisionalObjectCustodyRepository>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<IProvisionalObjectPostureProbe>());
            Assert.Null(scope.ServiceProvider.GetService<IProvisionalObjectWriter>());
            Assert.Null(scope.ServiceProvider.GetService<IProvisionalObjectReconciler>());
            Assert.Null(scope.ServiceProvider.GetService<IProvisionalObjectLifecycle>());
            await Assert.ThrowsAnyAsync<Exception>(() => wrapper.CheckAsync(CancellationToken.None));
        }
    }

    private static WebApplicationFactory<Program> ProductionReadinessFactory(params IReadinessCheck[] checks) =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("environment", "Production");
                builder.UseSetting("TagEkyc:Persistence:Provider", "Postgres");
                builder.UseSetting("TagEkyc:Persistence:ConnectionStringSecretRef", SecretRef("Host=127.0.0.1;Database=unused;Username=unused;Password=db-secret"));
                builder.UseSetting("TagEkyc:ApiKeyStore:Backend", ApiKeyStoreBackends.Postgres);
                builder.UseSetting("TagEkyc:ApiKeyStore:PepperSecretRef", Tip84BTestSupport.PepperSecretRef());
                builder.UseSetting("TagEkyc:EvidenceSigning:Backend", EvidenceSigningBackends.ProductionTrialP12);
                builder.UseSetting("TagEkyc:Retention:RegulatedEvidenceRetentionDays", "30");
                builder.UseSetting("TagEkyc:DecisionThresholds:FaceMatch", "0.80");
                builder.UseSetting("TagEkyc:DecisionThresholds:Liveness", "0.80");
                builder.ConfigureTestServices(services =>
                {
                    RemoveReadinessHostedServices(services);
                    services.RemoveAll<IReadinessCheck>();
                    foreach (var check in checks)
                    {
                        services.AddScoped<IReadinessCheck>(_ => check);
                    }
                });
            });

    private static WebApplicationFactory<Program> ProductionObjectCustodyFactory(
        string? topology,
        bool durable = false) =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("environment", "Production");
                builder.UseSetting("TagEkyc:Persistence:Provider", "Postgres");
                builder.UseSetting("TagEkyc:Persistence:ConnectionStringSecretRef", SecretRef(
                    "Host=127.0.0.1;Port=1;Database=unused;Username=unused;Password=db-secret;Timeout=1"));
                builder.UseSetting("TagEkyc:ApiKeyStore:Backend", ApiKeyStoreBackends.Postgres);
                builder.UseSetting("TagEkyc:ApiKeyStore:PepperSecretRef", Tip84BTestSupport.PepperSecretRef());
                builder.UseSetting("TagEkyc:EvidenceSigning:Backend", EvidenceSigningBackends.ProductionTrialP12);
                builder.UseSetting("TagEkyc:Retention:RegulatedEvidenceRetentionDays", "30");
                builder.UseSetting("TagEkyc:DecisionThresholds:FaceMatch", "0.80");
                builder.UseSetting("TagEkyc:DecisionThresholds:Liveness", "0.80");
                if (topology is not null)
                    builder.UseSetting("TagEkyc:RawExport:ObjectCustody:Topology", topology);
                if (durable)
                {
                    builder.UseSetting("TagEkyc:RawExport:ObjectCustody:Capability", "PostureProbe");
                    builder.UseSetting("TagEkyc:RawExport:ObjectCustody:ServiceUrl", "https://object-store.invalid");
                    builder.UseSetting("TagEkyc:RawExport:ObjectCustody:BucketName", "tagekyc-raw-export");
                    builder.UseSetting("TagEkyc:RawExport:ObjectCustody:AccessKeyId", "fixture-access");
                    builder.UseSetting("TagEkyc:RawExport:ObjectCustody:SecretAccessKey", "fixture-secret");
                    builder.UseSetting("TagEkyc:RawExport:ObjectCustody:AllowLoopbackHttp", "false");
                    builder.UseSetting("TagEkyc:RawExport:ObjectCustody:MaximumSinglePartCiphertextBytes", "134217728");
                    builder.UseSetting("TagEkyc:RawExport:ObjectCustody:OperationTimeoutSeconds", "300");
                }
                builder.ConfigureTestServices(services =>
                {
                    var hasReadinessWrapper = services.Any(descriptor =>
                        descriptor.ServiceType == typeof(IReadinessCheck)
                        && descriptor.ImplementationType
                            == typeof(ProvisionalObjectCustodyReadinessCheck));
                    RemoveReadinessHostedServices(services);
                    services.AddSingleton(new ObjectCustodyRegistrationSnapshot(hasReadinessWrapper));
                });
            });

    private sealed record ObjectCustodyRegistrationSnapshot(bool HasReadinessWrapper);

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

    private static async Task<string[]> ReadCodesAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        return CodesFromBody(body);
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
        var name = $"TAGEKYC_TIP83E1_SECRET_{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable(name, value);
        return $"env:{name}";
    }

    private sealed class FakeReadinessCheck(Func<CancellationToken, Task<IReadOnlyList<ReadinessIssue>>> check) : IReadinessCheck
    {
        public Task<IReadOnlyList<ReadinessIssue>> CheckAsync(CancellationToken cancellationToken) => check(cancellationToken);
    }

    private sealed class ThrowingStartupSigner(string message) : IEvidenceSigner, IEvidenceSignerStartupValidator
    {
        public void ValidateStartup(CancellationToken cancellationToken = default) => throw new InvalidOperationException(message);

        public Task<EvidenceSignatureEnvelope> SignAsync(EvidenceSignatureRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<EvidenceSignatureEnvelope> SignProofAsync(EvidenceProofSignatureRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class NoopJwksProvider : IEs256JwksProvider
    {
        public Es256JwksDocument GetJwks() => new([]);

        public void ValidateStartup(CancellationToken cancellationToken = default)
        {
        }
    }
}
