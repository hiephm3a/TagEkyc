using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Npgsql;
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
        using var factory = ProductionReadinessFactory(new FakeReadinessCheck(async cancellationToken =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return [];
        }));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/readiness");
        var codes = await ReadCodesAsync(response);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal([ReadinessEndpoint.GenericFailureCode], codes);
    }

    [Fact]
    public async Task RQ1_existing_two_second_timeout_semantics_remain_unchanged()
    {
        var timeoutField = typeof(ReadinessEndpoint).GetField(
            "PerCheckTimeout",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(timeoutField);
        Assert.Equal(TimeSpan.FromSeconds(2), timeoutField.GetValue(null));

        var lifecycle = new ReadinessLifecycleRegistry();
        using var factory = QuiescenceReadinessFactory(lifecycle);
        using var client = factory.CreateClient();
        var stopwatch = Stopwatch.StartNew();

        var response = await client.GetAsync("/readiness");
        stopwatch.Stop();
        var codes = await ReadCodesAsync(response);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal([ReadinessEndpoint.GenericFailureCode], codes);
        Assert.InRange(stopwatch.Elapsed, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(6));
    }

    [Fact]
    public async Task RQ2_timeout_cancellation_quiesces_before_scope_disposal()
    {
        var lifecycle = new ReadinessLifecycleRegistry();
        using var factory = QuiescenceReadinessFactory(lifecycle);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/readiness");
        var codes = await ReadCodesAsync(response);
        var operation = Assert.Single(lifecycle.Operations);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal([ReadinessEndpoint.GenericFailureCode], codes);
        Assert.True(operation.CancellationSequence > operation.StartedSequence);
        Assert.True(operation.TerminalSequence > operation.CancellationSequence);
        Assert.True(operation.DisposeSequence > operation.TerminalSequence);
        Assert.False(operation.DisposedBeforeTerminal);
        Assert.Equal(ConnectionState.Closed, operation.StateAtDispose);
    }

    [Fact]
    public async Task RQ3_repeated_database_timeouts_return_to_server_and_client_quiescence()
    {
        const int iterations = 20;
        await using var postgres = await DisposableReadinessPostgres.CreateAsync();
        var applicationName = $"tip83-rq3-{Guid.NewGuid():N}";
        var lifecycle = new ReadinessLifecycleRegistry();
        var connectionString = new NpgsqlConnectionStringBuilder(postgres.ConnectionString)
        {
            ApplicationName = applicationName,
            Pooling = false,
            Timeout = 10,
            CommandTimeout = 30,
        }.ConnectionString;
        var baseline = await CountSessionsAsync(postgres.ConnectionString, applicationName);
        var observedCounts = new List<int>(iterations);

        using var factory = PostgresTimeoutReadinessFactory(lifecycle, connectionString);
        using var client = factory.CreateClient();
        for (var index = 0; index < iterations; index++)
        {
            var response = await client.GetAsync("/readiness");
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.Equal([ReadinessEndpoint.GenericFailureCode], await ReadCodesAsync(response));
            observedCounts.Add(await WaitForSessionBaselineAsync(
                postgres.ConnectionString,
                applicationName,
                baseline));
        }

        Assert.Equal(iterations, lifecycle.Operations.Count);
        Assert.All(lifecycle.Operations, operation =>
        {
            Assert.True(operation.CancellationSequence > operation.StartedSequence);
            Assert.True(operation.TerminalSequence > operation.CancellationSequence);
            Assert.True(operation.DisposeSequence > operation.TerminalSequence);
            Assert.False(operation.DisposedBeforeTerminal);
            Assert.NotEqual(ConnectionState.Connecting, operation.StateAtDispose);
        });
        Assert.All(observedCounts, count => Assert.Equal(baseline, count));
        Assert.Equal(
            baseline,
            await CountSessionsAsync(postgres.ConnectionString, applicationName));
        Assert.Equal(
            0,
            await CountIdleInTransactionSessionsAsync(
                postgres.ConnectionString,
                applicationName));
    }

    [Fact]
    public async Task RQ4_normal_success_and_ordinary_database_failure_remain_unchanged()
    {
        using (var success = ProductionReadinessFactory(
                   new FakeReadinessCheck(_ =>
                       Task.FromResult<IReadOnlyList<ReadinessIssue>>([]))))
        using (var client = success.CreateClient())
        {
            var response = await client.GetAsync("/readiness");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        using (var failure = ProductionReadinessFactory(
                   new FakeReadinessCheck(_ =>
                       Task.FromResult<IReadOnlyList<ReadinessIssue>>([
                           new(DatabaseOrder, PostgresProductionReadinessValidator.Unreachable),
                       ]))))
        using (var client = failure.CreateClient())
        {
            var response = await client.GetAsync("/readiness");
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.Equal(
                [PostgresProductionReadinessValidator.Unreachable],
                await ReadCodesAsync(response));
        }
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
        ProductionReadinessFactory(services =>
        {
            foreach (var check in checks)
            {
                services.AddScoped<IReadinessCheck>(_ => check);
            }
        });

    private static WebApplicationFactory<Program> ProductionReadinessFactory(
        Action<IServiceCollection> configureChecks) =>
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
                    configureChecks(services);
                });
            });

    private static WebApplicationFactory<Program> QuiescenceReadinessFactory(
        ReadinessLifecycleRegistry lifecycle) =>
        ProductionReadinessFactory(services =>
        {
            services.AddSingleton(lifecycle);
            services.AddScoped<IReadinessCheck, DelayedCancellationReadinessCheck>();
        });

    private static WebApplicationFactory<Program> PostgresTimeoutReadinessFactory(
        ReadinessLifecycleRegistry lifecycle,
        string connectionString) =>
        ProductionReadinessFactory(services =>
        {
            services.AddSingleton(lifecycle);
            services.AddSingleton(new PostgresTimeoutConnectionString(connectionString));
            services.AddScoped<IReadinessCheck, PostgresTimeoutReadinessCheck>();
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

    private static async Task<int> CountSessionsAsync(
        string connectionString,
        string applicationName)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT count(*)::integer
            FROM pg_catalog.pg_stat_activity
            WHERE datname = current_database()
              AND application_name = @application_name
            """;
        command.Parameters.AddWithValue("application_name", applicationName);
        return (int)(await command.ExecuteScalarAsync())!;
    }

    private static async Task<int> CountIdleInTransactionSessionsAsync(
        string connectionString,
        string applicationName)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT count(*)::integer
            FROM pg_catalog.pg_stat_activity
            WHERE datname = current_database()
              AND application_name = @application_name
              AND state IN ('idle in transaction', 'idle in transaction (aborted)')
            """;
        command.Parameters.AddWithValue("application_name", applicationName);
        return (int)(await command.ExecuteScalarAsync())!;
    }

    private static async Task<int> WaitForSessionBaselineAsync(
        string connectionString,
        string applicationName,
        int baseline)
    {
        var deadline = Stopwatch.StartNew();
        int count;
        do
        {
            count = await CountSessionsAsync(connectionString, applicationName);
            if (count == baseline)
            {
                return count;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(25));
        } while (deadline.Elapsed < TimeSpan.FromSeconds(2));

        return count;
    }

    private sealed class FakeReadinessCheck(Func<CancellationToken, Task<IReadOnlyList<ReadinessIssue>>> check) : IReadinessCheck
    {
        public Task<IReadOnlyList<ReadinessIssue>> CheckAsync(CancellationToken cancellationToken) => check(cancellationToken);
    }

    private sealed class ReadinessLifecycleRegistry
    {
        private long sequence;

        public ConcurrentBag<ReadinessLifecycleOperation> Operations { get; } = [];

        public ReadinessLifecycleOperation Start()
        {
            var operation = new ReadinessLifecycleOperation
            {
                StartedSequence = NextSequence(),
            };
            Operations.Add(operation);
            return operation;
        }

        public long NextSequence() => Interlocked.Increment(ref sequence);
    }

    private sealed class ReadinessLifecycleOperation
    {
        public long StartedSequence { get; init; }
        public long CancellationSequence { get; set; }
        public long TerminalSequence { get; set; }
        public long DisposeSequence { get; set; }
        public bool DisposedBeforeTerminal { get; set; }
        public ConnectionState StateAtDispose { get; set; }
    }

    private sealed class DelayedCancellationReadinessCheck(
        ReadinessLifecycleRegistry registry) : IReadinessCheck, IAsyncDisposable
    {
        private readonly ReadinessLifecycleOperation operation = registry.Start();

        public async Task<IReadOnlyList<ReadinessIssue>> CheckAsync(
            CancellationToken cancellationToken)
        {
            using var registration = cancellationToken.Register(() =>
                operation.CancellationSequence = registry.NextSequence());
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return [];
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(150));
                throw;
            }
            finally
            {
                operation.TerminalSequence = registry.NextSequence();
            }
        }

        public ValueTask DisposeAsync()
        {
            operation.DisposedBeforeTerminal = operation.TerminalSequence == 0;
            operation.StateAtDispose = operation.DisposedBeforeTerminal
                ? ConnectionState.Connecting
                : ConnectionState.Closed;
            operation.DisposeSequence = registry.NextSequence();
            if (operation.DisposedBeforeTerminal)
            {
                throw new InvalidOperationException(
                    "Can't close, connection is in state Connecting");
            }

            return ValueTask.CompletedTask;
        }
    }

    private sealed record PostgresTimeoutConnectionString(string Value);

    private sealed class PostgresTimeoutReadinessCheck(
        ReadinessLifecycleRegistry registry,
        PostgresTimeoutConnectionString connectionString) : IReadinessCheck, IAsyncDisposable
    {
        private readonly ReadinessLifecycleOperation operation = registry.Start();
        private readonly NpgsqlConnection connection = new(connectionString.Value);

        public async Task<IReadOnlyList<ReadinessIssue>> CheckAsync(
            CancellationToken cancellationToken)
        {
            using var registration = cancellationToken.Register(() =>
                operation.CancellationSequence = registry.NextSequence());
            try
            {
                await connection.OpenAsync(cancellationToken);
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT pg_catalog.pg_sleep(30)";
                await command.ExecuteNonQueryAsync(cancellationToken);
                return [];
            }
            finally
            {
                operation.TerminalSequence = registry.NextSequence();
            }
        }

        public async ValueTask DisposeAsync()
        {
            operation.DisposedBeforeTerminal = operation.TerminalSequence == 0;
            operation.StateAtDispose = connection.State;
            operation.DisposeSequence = registry.NextSequence();
            if (operation.DisposedBeforeTerminal || connection.State == ConnectionState.Connecting)
            {
                throw new InvalidOperationException(
                    "Can't close, connection is in state Connecting");
            }

            await connection.DisposeAsync();
        }
    }

    private sealed class DisposableReadinessPostgres(
        string containerName,
        string connectionString) : IAsyncDisposable
    {
        private static readonly TimeSpan ProtocolUsabilityTimeout = TimeSpan.FromSeconds(15);
        private static readonly TimeSpan ProtocolUsabilityRetryDelay = TimeSpan.FromMilliseconds(100);

        public string ConnectionString { get; } = connectionString;

        public static async Task<DisposableReadinessPostgres> CreateAsync()
        {
            var containerName = $"tagekyc-readiness-{Guid.NewGuid():N}";
            await RunDockerAsync(
                "run",
                "-d",
                "--name",
                containerName,
                "-e",
                "POSTGRES_DB=tagekyc_readiness",
                "-e",
                "POSTGRES_USER=tagekyc",
                "-e",
                "POSTGRES_PASSWORD=tagekyc",
                "-p",
                "127.0.0.1::5432",
                "postgres:16");

            try
            {
                for (var attempt = 0; attempt < 60; attempt++)
                {
                    var ready = await RunDockerAsync(
                        allowFailure: true,
                        "exec",
                        containerName,
                        "pg_isready",
                        "-U",
                        "tagekyc",
                        "-d",
                        "tagekyc_readiness");
                    if (ready.ExitCode == 0)
                    {
                        var portResult = await RunDockerAsync(
                            "port",
                            containerName,
                            "5432/tcp");
                        var port = int.Parse(
                            portResult.StandardOutput.Trim().Split(':')[^1],
                            System.Globalization.CultureInfo.InvariantCulture);
                        var connectionString =
                            $"Host=127.0.0.1;Port={port};Database=tagekyc_readiness;" +
                            "Username=tagekyc;Password=tagekyc;Include Error Detail=true;Pooling=false";
                        await ProveStartupUsabilityPolicyAsync(connectionString);
                        return new DisposableReadinessPostgres(
                            containerName,
                            connectionString);
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(250));
                }

                throw new TimeoutException(
                    "Disposable readiness PostgreSQL did not become ready.");
            }
            catch
            {
                await RunDockerAsync(allowFailure: true, "rm", "-f", containerName);
                throw;
            }
        }

        private static async Task ProveStartupUsabilityPolicyAsync(string connectionString)
        {
            var injector = new SyntheticStartupTransientInjector();
            Assert.Equal(0, injector.ConsumedCount);
            var totalProbeAttempts = 0;
            var realStartupTransients = 0;
            var startupStopwatch = Stopwatch.StartNew();
            var successfulAttempt = 0;
            var protocolUsabilityBarrierSucceeded = false;
            await WaitForProtocolUsabilityAsync(
                async () =>
                {
                    var attempt = Interlocked.Increment(ref totalProbeAttempts);
                    injector.EmitNextIfAvailable();

                    try
                    {
                        await OpenConnectionAsync(connectionString);
                        successfulAttempt = attempt;
                        protocolUsabilityBarrierSucceeded = true;
                    }
                    catch (Exception exception)
                    {
                        if (IsStartupTransportTransient(exception))
                        {
                            Interlocked.Increment(ref realStartupTransients);
                        }

                        throw;
                    }
                },
                ProtocolUsabilityTimeout,
                ProtocolUsabilityRetryDelay);
            Assert.Equal(2, injector.ConsumedCount);
            Assert.True(protocolUsabilityBarrierSucceeded);
            Assert.True(startupStopwatch.Elapsed <= ProtocolUsabilityTimeout);
            Console.WriteLine(
                $"RQ3_STARTUP_TELEMETRY total_attempts={totalProbeAttempts} synthetic_consumed={injector.ConsumedCount} real_startup_transients={realStartupTransients} success_attempt={successfulAttempt} elapsed_ms={startupStopwatch.ElapsedMilliseconds}");

            var persistentAttempts = 0;
            await Assert.ThrowsAsync<TimeoutException>(() =>
                WaitForProtocolUsabilityAsync(
                    () =>
                    {
                        persistentAttempts++;
                        throw new NpgsqlException(
                            "Controlled persistent PostgreSQL startup transport transient.",
                            new EndOfStreamException("Controlled persistent startup EOF."));
                    },
                    TimeSpan.FromMilliseconds(10),
                    TimeSpan.Zero));
            Assert.True(persistentAttempts > 1);

            var invalidCredentials = new NpgsqlConnectionStringBuilder(connectionString)
            {
                Password = $"invalid-{Guid.NewGuid():N}",
            }.ConnectionString;
            var nonStartupAttempts = 0;
            var failure = await Assert.ThrowsAnyAsync<NpgsqlException>(() =>
                WaitForProtocolUsabilityAsync(
                    async () =>
                    {
                        nonStartupAttempts++;
                        await OpenConnectionAsync(invalidCredentials);
                    },
                    ProtocolUsabilityTimeout,
                    ProtocolUsabilityRetryDelay));
            Assert.False(IsStartupTransportTransient(failure));
            Assert.Equal(1, nonStartupAttempts);
        }

        private sealed class SyntheticStartupTransientInjector
        {
            private int consumedCount;

            public int ConsumedCount => Volatile.Read(ref consumedCount);

            public void EmitNextIfAvailable()
            {
                while (true)
                {
                    var current = Volatile.Read(ref consumedCount);
                    if (current >= 2)
                    {
                        return;
                    }

                    if (Interlocked.CompareExchange(ref consumedCount, current + 1, current) != current)
                    {
                        continue;
                    }

                    if (current == 0)
                    {
                        throw new NpgsqlException(
                            "Controlled PostgreSQL startup transport transient.",
                            new EndOfStreamException("Controlled startup EOF."));
                    }

                    throw new PostgresException(
                        "the database system is starting up",
                        "FATAL",
                        "FATAL",
                        "57P03");
                }
            }
        }

        private static async Task WaitForProtocolUsabilityAsync(
            Func<Task> probe,
            TimeSpan timeout,
            TimeSpan retryDelay)
        {
            var stopwatch = Stopwatch.StartNew();
            while (true)
            {
                try
                {
                    await probe();
                    return;
                }
                catch (Exception exception) when (IsStartupTransportTransient(exception))
                {
                    if (stopwatch.Elapsed >= timeout)
                    {
                        throw new TimeoutException(
                            "Disposable readiness PostgreSQL did not become protocol-usable.",
                            exception);
                    }
                }

                if (retryDelay > TimeSpan.Zero)
                {
                    await Task.Delay(retryDelay);
                }
                else
                {
                    await Task.Yield();
                }
            }
        }

        private static bool IsStartupTransportTransient(Exception exception)
        {
            if (exception is PostgresException
                {
                    SqlState: "57P03",
                    MessageText: "the database system is starting up",
                })
            {
                return true;
            }

            if (exception is not NpgsqlException)
            {
                return false;
            }

            for (var current = exception.InnerException; current is not null; current = current.InnerException)
            {
                if (current is EndOfStreamException)
                {
                    return true;
                }
            }

            return false;
        }

        private static async Task OpenConnectionAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await RunDockerAsync(allowFailure: true, "rm", "-f", containerName);
        }

        private static Task<DockerResult> RunDockerAsync(
            params string[] arguments) =>
            RunDockerAsync(allowFailure: false, arguments);

        private static async Task<DockerResult> RunDockerAsync(
            bool allowFailure,
            params string[] arguments)
        {
            var startInfo = new ProcessStartInfo("docker")
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };
            foreach (var argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Failed to start docker.");
            var standardOutput = await process.StandardOutput.ReadToEndAsync();
            var standardError = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            if (!allowFailure && process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"docker {string.Join(' ', arguments)} failed: {standardError}");
            }

            return new DockerResult(
                process.ExitCode,
                standardOutput,
                standardError);
        }

        private sealed record DockerResult(
            int ExitCode,
            string StandardOutput,
            string StandardError);
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
