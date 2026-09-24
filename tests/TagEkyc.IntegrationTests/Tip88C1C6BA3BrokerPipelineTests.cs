using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Reflection;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using TagEkyc.Application.Ports;
using TagEkyc.Application.RawExport;
using TagEkyc.Application.CaptureRuntime;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Api;
using TagEkyc.Infrastructure.RawExport;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.IntegrationTests;

public sealed class Tip88C1C6BA3ApiHostGraphTests
{
    [Fact]
    public void PreparedWithIncompleteActivationEvidenceStillStarts()
    {
        using var factory = StartupSelectionFactory(activated: false,
            ActivationEvidenceTestSeals.Valid(authorityOpenRows: 1));
        using var client = factory.CreateClient();
        Assert.NotNull(client);
        var registered = factory.Services.GetRequiredService<ICaptureRuntimeActivationEvidenceSealProvider>();
        Assert.Equal(1, registered.Current!.ApprovedAuthorityOpenRowCount);
        using var scope = factory.Services.CreateScope();
        var startup = scope.ServiceProvider.GetRequiredService<CaptureRuntimeStartup>();
        var injected = typeof(CaptureRuntimeStartup).GetFields(
                BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(field => field.FieldType == typeof(ICaptureRuntimeActivationEvidenceSealProvider))
            .GetValue(startup);
        Assert.Same(registered, injected);
    }

    [Fact]
    public void ActivatedWithIncompleteActivationEvidenceFailsBeforeRoutesStart()
    {
        using var factory = StartupSelectionFactory(activated: true,
            ActivationEvidenceTestSeals.Valid(authorityOpenRows: 1));
        var failure = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains("CAPTURE_RUNTIME_ACTIVATION_EVIDENCE_INCOMPLETE", failure.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("CAPTURE_RUNTIME_STARTUP_NOT_READY", failure.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void ActivatedWithCurrentGeneratedZeroOpenSealStartsWithMatchingSiteQualification()
    {
        using var factory = StartupSelectionFactory(activated: true, seal: null,
            useGeneratedSeal: true);
        using var client = factory.CreateClient();
        Assert.NotNull(client);
        var seal = factory.Services.GetRequiredService<ICaptureRuntimeActivationEvidenceSealProvider>().Current;
        Assert.Equal(0, seal!.ApprovedAuthorityOpenRowCount);
        Assert.True(seal.ApprovedSiteTransportQualificationRequired);
        Assert.Equal(1, seal.ApprovedSiteTransportQualificationPolicyVersion);
    }

    [Fact]
    public void ActivatedWithValidZeroOpenActivationEvidenceStarts()
    {
        using var factory = StartupSelectionFactory(activated: true,
            ActivationEvidenceTestSeals.Valid(authorityOpenRows: 0));
        using var client = factory.CreateClient();
        Assert.NotNull(client);
    }

    [Fact]
    public void ActivatedWithAssemblyExcludedSealRejectsInvalidEffectiveTopology()
    {
        using var factory = StartupSelectionFactory(activated: true,
            ActivationEvidenceTestSeals.Valid(authorityOpenRows: 0), "Unrecognized");
        var failure = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains("CAPTURE_RUNTIME_ACTIVATION_EVIDENCE_INVALID", failure.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("CAPTURE_RUNTIME_STARTUP_NOT_READY", failure.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ActivatedRawIngressRechecksRenewableSiteQualificationOnEveryRequest()
    {
        var qualification = new ActivationEvidenceTestSeals.MutableQualificationProvider();
        using var factory = StartupSelectionFactory(activated: true,
            ActivationEvidenceTestSeals.Valid(authorityOpenRows: 0), siteQualification: qualification);
        using var client = factory.CreateClient();
        qualification.Current = qualification.Current with { ValidUntilUtc = DateTimeOffset.UtcNow.AddSeconds(-1) };
        using var response = await client.PostAsync("/api/ekyc/raw-export/source-ingress",
            new ByteArrayContent([0x01]));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains(CaptureRuntimeSiteTransportQualificationPolicy.ExpiredCode,
            await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ActivatedHostStartsWithoutSiteRecordWhileRawIngressRemainsClosed()
    {
        using var factory = StartupSelectionFactory(activated: true,
            ActivationEvidenceTestSeals.Valid(authorityOpenRows: 0),
            siteQualification: new ActivationEvidenceTestSeals.MissingQualificationProvider());
        using var client = factory.CreateClient();
        using var health = await client.GetAsync("/health/site-transport-qualification");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, health.StatusCode);
        Assert.Contains("Missing", await health.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        using var response = await client.PostAsync("/api/ekyc/raw-export/source-ingress",
            new ByteArrayContent([0x01]));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains(CaptureRuntimeSiteTransportQualificationPolicy.InvalidCode,
            await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("malformed")]
    [InlineData("legacy-zero-open")]
    [InlineData("partition-mismatch")]
    [InlineData("ledger-mismatch")]
    public void ActivatedWithInvalidActivationEvidenceFailsBeforeRoutesStart(string mutation)
    {
        var seal = mutation switch
        {
            "missing" => null,
            "malformed" => ActivationEvidenceTestSeals.Valid(0) with { FormatVersion = 0 },
            "legacy-zero-open" => ActivationEvidenceTestSeals.Valid(0) with { FormatVersion = 1 },
            "partition-mismatch" => ActivationEvidenceTestSeals.Valid(0) with
                { BuildPartitionSha256 = ActivationEvidenceTestSeals.OtherSha },
            _ => ActivationEvidenceTestSeals.Valid(0) with
                { BuildLedgerSha256 = ActivationEvidenceTestSeals.OtherSha }
        };
        using var factory = StartupSelectionFactory(activated: true, seal);
        var failure = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains("CAPTURE_RUNTIME_ACTIVATION_EVIDENCE_INVALID", failure.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("CAPTURE_RUNTIME_STARTUP_NOT_READY", failure.ToString(), StringComparison.Ordinal);
    }

    private static WebApplicationFactory<Program> StartupSelectionFactory(bool activated,
        CaptureRuntimeActivationEvidenceSeal? seal, string assemblyTopology = "Disabled",
        bool useGeneratedSeal = false,
        ICaptureRuntimeSiteTransportQualificationProvider? siteQualification = null) =>
        new HistoricalPreparedWebApplicationFactory()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("environment", "Development");
                builder.UseSetting("TagEkyc:Persistence:Provider", "InMemory");
                builder.UseSetting("TagEkyc:RawExport:Assembly:Topology", assemblyTopology);
                builder.UseSetting("TagEkyc:RawExport:RawExportCustodyMaximumConcurrentStreamsPerProducer", "1");
                builder.UseSetting("TagEkyc:RawExport:RawExportCustodyMaximumConcurrentStreamsPerDeployment", "2");
                builder.UseSetting("TagEkyc:RawExport:RawExportCustodyMaximumAggregatePlaintextWindowBytesPerDeployment", "2097152");
                builder.UseSetting("TagEkyc:RawExport:RawExportCustodyMaximumPlaintextWindowBytesPerStream", "1048576");
                builder.UseSetting(SiteRawIngressTransportQualificationFileProvider.SiteIdConfigurationKey,
                    "integration-site");
                builder.UseSetting(SiteRawIngressTransportQualificationFileProvider.EndpointOriginConfigurationKey,
                    "https://127.0.0.1:8443");
                builder.UseSetting(SiteRawIngressTransportQualificationFileProvider.DeploymentRevisionConfigurationKey,
                    "integration-deployment-1");
                builder.ConfigureTestServices(services =>
                {
                    if (activated)
                    {
                        services.RemoveAll<ICaptureRuntimeStartupDependencyReader>();
                        services.AddSingleton<ICaptureRuntimeStartupDependencyReader, ActivatedReader>();
                    }
                    if (!useGeneratedSeal)
                    {
                        services.RemoveAll<ICaptureRuntimeActivationEvidenceSealProvider>();
                        if (seal is not null)
                            services.AddSingleton<ICaptureRuntimeActivationEvidenceSealProvider>(
                                new ActivationEvidenceTestSeals.Provider(seal));
                    }
                    services.RemoveAll<ICaptureRuntimeSiteTransportQualificationProvider>();
                    services.AddSingleton<ICaptureRuntimeSiteTransportQualificationProvider>(
                        siteQualification ?? new ActivationEvidenceTestSeals.QualificationProvider());
                    services.RemoveAll<ICaptureRuntimeSiteTransportQualificationSettingsProvider>();
                    services.AddSingleton<ICaptureRuntimeSiteTransportQualificationSettingsProvider>(
                        new ActivationEvidenceTestSeals.QualificationSettingsProvider());
                    services.RemoveAll<ICaptureRuntimeA3Readiness>();
                    services.AddSingleton<ICaptureRuntimeA3Readiness, Ready>();
                    services.RemoveAll<ICaptureRuntimeRawIngressAdmission>();
                    services.AddSingleton<ICaptureRuntimeRawIngressAdmission, NeverAdmission>();
                    foreach (var descriptor in services.Where(item =>
                                 item.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService) &&
                                 item.ImplementationType?.Name == "CaptureRuntimeA3HostedService").ToArray())
                        services.Remove(descriptor);
                });
            });

    [Fact]
    public void OrdinaryProgramPreparedHostRegistersA3PortsWithoutConstructingOwners()
    {
        using var factory = new HistoricalPreparedWebApplicationFactory()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("environment", "Development");
                builder.UseSetting("TagEkyc:Persistence:Provider", "InMemory");
            });
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICaptureRuntimeRawIngressAdmission>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICaptureRuntimeA3Readiness>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICaptureRuntimeA3Worker>());
        Assert.Null(scope.ServiceProvider.GetService<CaptureRuntimeRawIngressComposition.RuntimeOwners>());
        Assert.Null(scope.ServiceProvider.GetService<RawIngressBrokerOptions>());
    }

    [Fact]
    public void OrdinaryProgramActivatedWithoutExplicitOwnersOrProvidersFailsBeforeRoutesStart()
    {
        using var factory = new HistoricalPreparedWebApplicationFactory()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("environment", "Development");
                builder.UseSetting("TagEkyc:Persistence:Provider", "InMemory");
                builder.UseSetting("TagEkyc:RawExport:RawExportCustodyMaximumConcurrentStreamsPerProducer", "1");
                builder.UseSetting("TagEkyc:RawExport:RawExportCustodyMaximumConcurrentStreamsPerDeployment", "1");
                builder.UseSetting("TagEkyc:RawExport:RawExportCustodyMaximumAggregatePlaintextWindowBytesPerDeployment", "1024");
                builder.UseSetting("TagEkyc:RawExport:RawExportCustodyMaximumPlaintextWindowBytesPerStream", "1024");
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<ICaptureRuntimeStartupDependencyReader>();
                    services.AddSingleton<ICaptureRuntimeStartupDependencyReader, ActivatedReader>();
                    services.RemoveAll<ICaptureRuntimeActivationEvidenceSealProvider>();
                    services.AddSingleton<ICaptureRuntimeActivationEvidenceSealProvider>(
                        new ActivationEvidenceTestSeals.Provider(ActivationEvidenceTestSeals.Valid(0)));
                    services.RemoveAll<ICaptureRuntimeSiteTransportQualificationProvider>();
                    services.AddSingleton<ICaptureRuntimeSiteTransportQualificationProvider>(
                        new ActivationEvidenceTestSeals.QualificationProvider());
                    services.RemoveAll<ICaptureRuntimeSiteTransportQualificationSettingsProvider>();
                    services.AddSingleton<ICaptureRuntimeSiteTransportQualificationSettingsProvider>(
                        new ActivationEvidenceTestSeals.QualificationSettingsProvider());
                });
            });

        var failure = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains("CAPTURE_RUNTIME_STARTUP_NOT_READY", failure.ToString(), StringComparison.Ordinal);
    }

    private sealed class ActivatedReader : ICaptureRuntimeStartupDependencyReader
    {
        public Task<CaptureRuntimeStartupDependencies> ReadAsync(DateTimeOffset now, CancellationToken ct) =>
            Task.FromResult(new CaptureRuntimeStartupDependencies("Managed", "Activated", 1,
                now.AddMinutes(-2), now.AddMinutes(-1),
                Guid.Parse("35ca377f-39ed-42bb-aa04-7dbc6e94d221"), []));
    }

    private sealed class Ready : ICaptureRuntimeA3Readiness
    {
        public Task<bool> IsReadyAsync(CancellationToken ct) => Task.FromResult(true);
    }

    private sealed class NeverAdmission : ICaptureRuntimeRawIngressAdmission
    {
        public ValueTask<CaptureRuntimeRawIngressAdmissionResult> AdmitAsync(
            CaptureRuntimeRawIngressAdmissionContext context, Stream body, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Startup selection must not admit a body.");
    }
}

internal static class ActivationEvidenceTestSeals
{
    private const string PartitionSha = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
    private const string LedgerSha = "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB";
    private const string ManifestSha = "CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC";
    private const string RecordSha = "DDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDD";
    private const string QualificationSha = "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF";
    public const string OtherSha = "EEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEE";

    public static CaptureRuntimeActivationEvidenceSeal Valid(int authorityOpenRows) => new(
        2, 1, PartitionSha, LedgerSha, authorityOpenRows,
        PartitionSha, LedgerSha, authorityOpenRows, ManifestSha, RecordSha,
        "Disabled", "Disabled", PartitionSha, LedgerSha,
        QualificationSha, QualificationSha, true, true, 1, 1);

    internal sealed class Provider : ICaptureRuntimeActivationEvidenceSealProvider
    {
        public Provider(CaptureRuntimeActivationEvidenceSeal seal) => Current = seal;
        public CaptureRuntimeActivationEvidenceSeal Current { get; }
    }
    internal sealed class QualificationProvider : ICaptureRuntimeSiteTransportQualificationProvider
    {
        public CaptureRuntimeSiteTransportQualification Current { get; } = new(1,
            "integration-qualification", "integration-site", "https://127.0.0.1:8443",
            "integration-deployment-1", "PASS", DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow.AddHours(1), 0, 0, 1, true, false, false, false,
            QualificationSha);
    }
    internal sealed class MutableQualificationProvider : ICaptureRuntimeSiteTransportQualificationProvider
    {
        public CaptureRuntimeSiteTransportQualification Current { get; set; } =
            new QualificationProvider().Current;
    }
    internal sealed class MissingQualificationProvider : ICaptureRuntimeSiteTransportQualificationProvider
    {
        public CaptureRuntimeSiteTransportQualification? Current => null;
    }
    internal sealed class QualificationSettingsProvider
        : ICaptureRuntimeSiteTransportQualificationSettingsProvider
    {
        public CaptureRuntimeSiteTransportQualificationSettings Current { get; } = new(
            "integration-site", "https://127.0.0.1:8443", "integration-deployment-1",
            TimeSpan.FromMinutes(5));
    }
}

public sealed class Tip88C1C6BA3PublicAdmissionFacadeTests
{
    [Fact]
    public void PreparedResolutionDoesNotConstructRoleOrProviderOwners()
    {
        var disabled = new ProvisionalObjectCustodyOptions(ProvisionalObjectTopology.Disabled, null,
            null, null, null, null, false, 134_217_728, TimeSpan.FromSeconds(300), true);
        var options = RawIngressBrokerOptions.Read(Tip88C1C6BA3SyntheticComposition.BrokerConfiguration());
        var services = new ServiceCollection().AddLogging()
            .AddTagEkycCaptureRuntimeRawIngress(options, new(
                "invalid-writer", disabled, "invalid-reconciler", disabled,
                "invalid-lifecycle", disabled, 1024));
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICaptureRuntimeRawIngressAdmission>());
        Assert.NotNull(provider.GetRequiredService<TagEkyc.Application.CaptureRuntime.ICaptureRuntimeA3Readiness>());
    }

    [Fact]
    public async Task BrokerFinalReturnsWithoutBodyReadAndReleasesBoundedWindow()
    {
        var capacity = new Capacity();
        var broker = new Broker(new RawIngressBrokerResult.Final(
            new(RawExportSourceIngressCodes.BindingInvalid)));
        var pipeline = new BodyPipeline();
        var service = new CaptureRuntimeRawIngressAdmissionService(capacity, broker, pipeline, 8);
        using var body = new MemoryStream(new byte[24], writable: false);

        var result = await service.AdmitAsync(Tip88C1C6BA3BrokerHttpTests.Metadata(), body, CancellationToken.None);

        Assert.Equal(CaptureRuntimeRawIngressOutcome.BindingInvalid, result.Outcome);
        Assert.Equal(0, body.Position);
        Assert.Equal(1, broker.Calls);
        Assert.Equal(0, pipeline.Calls);
        Assert.Equal(8, capacity.Bytes);
        Assert.Equal(Tip88C1C6BA3BrokerHttpTests.Metadata().CaptureAgentId.ToString("N"), capacity.Producer);
        Assert.Equal(1, capacity.Disposals);
    }

    [Fact]
    public async Task HandoffPassesOriginalUnreadStreamExactlyOnce()
    {
        var handoff = new RawIngressBrokerHandoff(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, 1,
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1,
            DateTimeOffset.UtcNow.AddMinutes(1));
        var capacity = new Capacity();
        var broker = new Broker(new RawIngressBrokerResult.Handoff(handoff));
        using var body = new MemoryStream(new byte[24], writable: false);
        var pipeline = new BodyPipeline(body, handoff);
        var service = new CaptureRuntimeRawIngressAdmissionService(capacity, broker, pipeline, 8);

        var result = await service.AdmitAsync(Tip88C1C6BA3BrokerHttpTests.Metadata(), body, CancellationToken.None);

        Assert.Equal(CaptureRuntimeRawIngressOutcome.ResumePending, result.Outcome);
        Assert.Equal(1, pipeline.Calls);
        Assert.Equal(1, capacity.Disposals);
    }

    [Fact]
    public async Task CapacityDenialDoesNotCallBrokerOrPipeline()
    {
        var capacity = new Capacity { Deny = true };
        var broker = new Broker(new RawIngressBrokerResult.Final(new(RawExportSourceIngressCodes.BindingInvalid)));
        var pipeline = new BodyPipeline();
        var service = new CaptureRuntimeRawIngressAdmissionService(capacity, broker, pipeline, 8);

        var result = await service.AdmitAsync(Tip88C1C6BA3BrokerHttpTests.Metadata(), new MemoryStream(), CancellationToken.None);

        Assert.Equal(CaptureRuntimeRawIngressOutcome.CapacityUnavailable, result.Outcome);
        Assert.Equal(0, broker.Calls);
        Assert.Equal(0, pipeline.Calls);
    }

    [Fact]
    public async Task A3_S02_O08_CapacityGateDeniesBeforeBrokerAndReleasesSlot()
    {
        // One live producer occupies the only real capacity slot while B is
        // blocked. The denied request must not enter B or touch its body.
        var capacity = new RawExportIngressCapacity(1, 1, 8, 8);
        var broker = new BlockingFirstBroker();
        var pipeline = new BodyPipeline();
        var service = new CaptureRuntimeRawIngressAdmissionService(capacity, broker, pipeline, 8);
        var metadata = Tip88C1C6BA3BrokerHttpTests.Metadata();
        using var firstBody = new CountingReadBody();
        var first = service.AdmitAsync(metadata, firstBody, CancellationToken.None).AsTask();
        await broker.FirstEntered.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.False(first.IsCompleted);

        using var deniedBody = new CountingReadBody();
        CaptureRuntimeRawIngressAdmissionResult denied;
        try
        {
            denied = await service.AdmitAsync(metadata with { IngressIdempotencyKey = Guid.NewGuid() },
                deniedBody, CancellationToken.None);
            Assert.Equal(CaptureRuntimeRawIngressOutcome.CapacityUnavailable, denied.Outcome);
            Assert.Equal(1, broker.Calls);
            Assert.Equal(0, pipeline.Calls);
            Assert.Equal(0, deniedBody.ReadCalls);
            Assert.Equal(0, firstBody.ReadCalls);
        }
        finally
        {
            broker.ReleaseFirst();
            await first.WaitAsync(TimeSpan.FromSeconds(5));
        }

        var mapper = typeof(RawExportSourceIngressEndpoints).GetMethod("MapRuntimeResult",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(mapper);
        using var services = new ServiceCollection().AddOptions().AddLogging().BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;
        var response = Assert.IsAssignableFrom<IResult>(mapper.Invoke(null, [context, denied]));
        await response.ExecuteAsync(context);
        Assert.Equal(503, context.Response.StatusCode);
        responseBody.Position = 0;
        using (var json = await JsonDocument.ParseAsync(responseBody))
        {
            Assert.Equal(new[] { "outcomeCode" },
                json.RootElement.EnumerateObject().Select(property => property.Name).ToArray());
            Assert.Equal(RawExportSourceIngressCodes.CapacityUnavailable,
                json.RootElement.GetProperty("outcomeCode").GetString());
        }

        // Releasing the first request returns the same slot; there is no
        // leaked partial reservation after the denied call.
        using var nextBody = new CountingReadBody();
        var next = await service.AdmitAsync(metadata with { IngressIdempotencyKey = Guid.NewGuid() },
            nextBody, CancellationToken.None);
        Assert.Equal(CaptureRuntimeRawIngressOutcome.BindingInvalid, next.Outcome);
        Assert.Equal(2, broker.Calls);
        Assert.Equal(0, pipeline.Calls);
        Assert.Equal(0, nextBody.ReadCalls);
    }

    [Fact]
    public async Task InvalidBrokerResultFailsClosedAndReleasesCapacity()
    {
        var capacity = new Capacity();
        var broker = new Broker(new RawIngressBrokerResult.Final(new("UNKNOWN")));
        var service = new CaptureRuntimeRawIngressAdmissionService(capacity, broker, new BodyPipeline(), 8);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await service.AdmitAsync(Tip88C1C6BA3BrokerHttpTests.Metadata(), new MemoryStream(), CancellationToken.None));
        Assert.Equal(1, capacity.Disposals);
    }

    [Theory]
    [InlineData("final")]
    [InlineData("invalid-final")]
    [InlineData("broker-fault")]
    [InlineData("handoff")]
    [InlineData("pipeline-fault")]
    [InlineData("cancelled")]
    public async Task BP10_BoundedWindowAndBothCountersReturnOnEveryAdmissionExit(string exit)
    {
        // The declared body is enormous, but this admission may retain only a
        // six-byte window. Probe the real capacity while B is suspended: two
        // more bytes fit, three do not, and a third stream never fits.
        var capacity = new RawExportIngressCapacity(2, 2, 8, 8);
        var broker = new GatedExitBroker(exit);
        var pipeline = new ExitBodyPipeline(exit == "pipeline-fault");
        var service = new CaptureRuntimeRawIngressAdmissionService(capacity, broker, pipeline, 6);
        var metadata = Tip88C1C6BA3BrokerHttpTests.Metadata() with
        {
            ClaimedPlaintextLength = long.MaxValue
        };
        using var body = new CountingReadBody();
        using var cancellation = new CancellationTokenSource();
        var admission = service.AdmitAsync(metadata, body, cancellation.Token).AsTask();
        await broker.Entered.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Null(capacity.TryAcquire("probe-too-large", 3));
        using (var remainingTwoBytes = capacity.TryAcquire("probe", 2))
        {
            Assert.NotNull(remainingTwoBytes);
            Assert.Null(capacity.TryAcquire("third-stream", 1));
        }
        Assert.Null(capacity.TryAcquire("probe-still-too-large", 3));

        if (exit == "cancelled") cancellation.Cancel();
        else broker.Release();

        if (exit is "final" or "handoff")
        {
            var result = await admission.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(exit == "final" ? CaptureRuntimeRawIngressOutcome.BindingInvalid
                : CaptureRuntimeRawIngressOutcome.ResumePending, result.Outcome);
        }
        else
        {
            var error = await Record.ExceptionAsync(() => admission.WaitAsync(TimeSpan.FromSeconds(5)));
            switch (exit)
            {
                case "invalid-final":
                    Assert.Equal("RAW_INGRESS_ADMISSION_RESULT_INVALID",
                        Assert.IsType<InvalidOperationException>(error).Message);
                    break;
                case "broker-fault":
                    Assert.Equal("BP10_BROKER_FAULT", Assert.IsType<IOException>(error).Message);
                    break;
                case "pipeline-fault":
                    Assert.Equal("BP10_PIPELINE_FAULT", Assert.IsType<IOException>(error).Message);
                    break;
                case "cancelled":
                    Assert.IsAssignableFrom<OperationCanceledException>(error);
                    break;
            }
        }

        Assert.Equal(0, body.ReadCalls);
        using var fullWindow = capacity.TryAcquire("next-producer", 8);
        Assert.NotNull(fullWindow); // detects leaked stream count OR leaked bytes
        Assert.Null(capacity.TryAcquire("next-producer", 1));
    }

    private sealed class GatedExitBroker(string exit) : IRawIngressMetadataBroker
    {
        private readonly TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal Task Entered => entered.Task;

        public async Task<RawIngressBrokerResult> AdmitAsync(
            CaptureRuntimeRawIngressAdmissionContext context, CancellationToken cancellationToken)
        {
            entered.TrySetResult();
            await release.Task.WaitAsync(cancellationToken);
            if (exit == "broker-fault") throw new IOException("BP10_BROKER_FAULT");
            if (exit == "invalid-final") return new RawIngressBrokerResult.Final(new("UNKNOWN"));
            if (exit is "handoff" or "pipeline-fault")
                return new RawIngressBrokerResult.Handoff(new RawIngressBrokerHandoff(
                    Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, 1,
                    Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1,
                    DateTimeOffset.UtcNow.AddMinutes(1)));
            return new RawIngressBrokerResult.Final(new(RawExportSourceIngressCodes.BindingInvalid));
        }

        internal void Release() => release.TrySetResult();
    }

    private sealed class ExitBodyPipeline(bool fail) : ICaptureRuntimeRawIngressBodyPipeline
    {
        public Task<CaptureRuntimeRawIngressAdmissionResult> ProcessAsync(
            CaptureRuntimeRawIngressAdmissionContext context, RawIngressBrokerHandoff handoff,
            Stream body, CancellationToken cancellationToken) =>
            fail ? Task.FromException<CaptureRuntimeRawIngressAdmissionResult>(
                new IOException("BP10_PIPELINE_FAULT")) :
                Task.FromResult(new CaptureRuntimeRawIngressAdmissionResult(
                    CaptureRuntimeRawIngressOutcome.ResumePending, null, null, null, null));
    }

    private sealed class Broker(RawIngressBrokerResult result) : IRawIngressMetadataBroker
    {
        internal int Calls;
        public Task<RawIngressBrokerResult> AdmitAsync(CaptureRuntimeRawIngressAdmissionContext context, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(result);
        }
    }

    private sealed class BlockingFirstBroker : IRawIngressMetadataBroker
    {
        private readonly TaskCompletionSource firstEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource releaseFirst = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal Task FirstEntered => firstEntered.Task;
        internal int Calls;

        public async Task<RawIngressBrokerResult> AdmitAsync(
            CaptureRuntimeRawIngressAdmissionContext context, CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref Calls) == 1)
            {
                firstEntered.TrySetResult();
                await releaseFirst.Task.WaitAsync(cancellationToken);
            }
            return new RawIngressBrokerResult.Final(new(RawExportSourceIngressCodes.BindingInvalid));
        }

        internal void ReleaseFirst() => releaseFirst.TrySetResult();
    }

    private sealed class CountingReadBody : MemoryStream
    {
        internal int ReadCalls;
        public override int Read(byte[] buffer, int offset, int count)
        {
            Interlocked.Increment(ref ReadCalls);
            return base.Read(buffer, offset, count);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref ReadCalls);
            return base.ReadAsync(buffer, cancellationToken);
        }
    }

    private sealed class BodyPipeline(Stream? expectedBody = null, RawIngressBrokerHandoff? expectedHandoff = null)
        : ICaptureRuntimeRawIngressBodyPipeline
    {
        internal int Calls;
        public Task<CaptureRuntimeRawIngressAdmissionResult> ProcessAsync(
            CaptureRuntimeRawIngressAdmissionContext context, RawIngressBrokerHandoff handoff,
            Stream body, CancellationToken cancellationToken)
        {
            Calls++;
            if (expectedBody is not null) Assert.Same(expectedBody, body);
            if (expectedHandoff is not null) Assert.Equal(expectedHandoff, handoff);
            Assert.Equal(0, body.Position);
            return Task.FromResult(new CaptureRuntimeRawIngressAdmissionResult(
                CaptureRuntimeRawIngressOutcome.ResumePending, null, null, null, null));
        }
    }

    private sealed class Capacity : IRawExportIngressCapacity
    {
        internal bool Deny;
        internal string? Producer;
        internal long Bytes;
        internal int Disposals;
        public IRawExportIngressCapacityLease? TryAcquire(string producerId, long plaintextBytes)
        {
            Producer = producerId;
            Bytes = plaintextBytes;
            return Deny ? null : new CapacityLease(this);
        }
        private sealed class CapacityLease(Capacity owner) : IRawExportIngressCapacityLease
        {
            public void Dispose() => owner.Disposals++;
        }
    }
}

public sealed class Tip88C1C6BA3BrokerHttpTests
{
    [Fact]
    public void PrivateMetadataShapeIsClosedAndBounded()
    {
        var expected = Metadata();
        var bytes = RawIngressBrokerProtocol.WriteRequest(expected);
        var actual = RawIngressBrokerProtocol.ReadRequest(bytes);
        Assert.Equal(expected with { Nonce = actual.Nonce, SignedEnvelopeFingerprint = actual.SignedEnvelopeFingerprint }, actual);
        Assert.Equal(expected.Nonce, actual.Nonce); Assert.Equal(expected.SignedEnvelopeFingerprint, actual.SignedEnvelopeFingerprint);
        Assert.NotSame(expected.Nonce, actual.Nonce);
        using var doc = JsonDocument.Parse(bytes);
        Assert.Equal(23, doc.RootElement.EnumerateObject().Count());
        var text = Encoding.UTF8.GetString(bytes);
        var invalid = new List<byte[]>
        {
            Encoding.UTF8.GetBytes(text + "{}"), Encoding.UTF8.GetBytes(text[..^1] + ",\"protocolVersion\":1}"),
            Encoding.UTF8.GetBytes(text[..^1] + ",\"principalId\":\"11111111111111111111111111111111\"}"),
            Encoding.UTF8.GetBytes(text[..^1] + ",\"raw\":\"biometric\"}"),
            Encoding.UTF8.GetBytes(text.Replace("\"protocolVersion\":1", "\"protocolVersion\":\"1\"")),
            Encoding.UTF8.GetBytes(text.Replace("\"protocolVersion\":1", "\"protocolVersion\":1.0")),
            Encoding.UTF8.GetBytes(text.Replace("\"protocolVersion\":1", "\"protocolVersion\":NaN")),
            Encoding.UTF8.GetBytes(text.Replace("\"protocolVersion\":1", "\"protocolVersion\":2")),
            Encoding.UTF8.GetBytes(text.Replace("\"rawClass\":\"LiveSelfieImage\"", "\"rawClass\":null")),
            Encoding.UTF8.GetBytes(text.Replace("\"rawClass\":\"LiveSelfieImage\"", "\"rawClass\":{\"x\":{\"y\":1}}")),
            Encoding.UTF8.GetBytes(text.Replace("\"protocolVersion\"", "\"ProtocolVersion\"")),
            Encoding.UTF8.GetBytes(text.Replace("\"protocolVersion\":1,", "")),
            Encoding.UTF8.GetBytes(text.Replace("{", "{/*comment*/", StringComparison.Ordinal)),
            Encoding.UTF8.GetBytes(text[..^1] + ",}"), new byte[16385], new byte[] { 0xff, 0xfe },
            Encoding.UTF8.GetBytes(text.Replace(expected.CredentialId.ToString("N"), expected.CredentialId.ToString("D"))),
            Encoding.UTF8.GetBytes(text.Replace(expected.CredentialId.ToString("N"), expected.CredentialId.ToString("N").ToUpperInvariant())),
            Encoding.UTF8.GetBytes(text.Replace("0000001Z", "0000001+00:00")),
            Encoding.UTF8.GetBytes(text.Replace("\"credentialGeneration\":1", "\"credentialGeneration\":0")),
            Encoding.UTF8.GetBytes(text.Replace("\"captureRevision\":1", "\"captureRevision\":2147483648")),
        };
        foreach (var value in invalid)
        {
            Assert.NotEqual(bytes, value);
            Assert.NotNull(Record.Exception(() => RawIngressBrokerProtocol.ReadRequest(value)));
        }
        // Fixed transport bounds admit an exactly-limit legal JSON document.
        var padded = bytes.Concat(Enumerable.Repeat((byte)' ', 16384 - bytes.Length)).ToArray();
        Assert.Equal(16384, padded.Length); Assert.Equal(expected.CredentialId, RawIngressBrokerProtocol.ReadRequest(padded).CredentialId);
        Assert.ThrowsAny<Exception>(() => RawIngressBrokerProtocol.ReadRequest(padded.Append((byte)' ').ToArray()));
    }

    [Fact]
    public void PrivateUnionRejectsMalformedHandoffWithoutBodyRead()
    {
        var valid = Handoff();
        var bytes = RawIngressBrokerProtocol.WriteResponse(valid);
        Assert.Equal(valid, RawIngressBrokerProtocol.ReadResponse(bytes));
        using var doc = JsonDocument.Parse(bytes);
        var fields = doc.RootElement.GetProperty("handoff").EnumerateObject().ToArray();
        Assert.Equal(11, fields.Length);
        foreach (var field in fields)
        {
            var missing = "{\"protocolVersion\":1,\"kind\":\"Handoff\",\"handoff\":{" +
                string.Join(',', fields.Where(x => x.Name != field.Name).Select(x => JsonSerializer.Serialize(x.Name) + ":" + x.Value.GetRawText())) + "}}";
            Assert.ThrowsAny<Exception>(() => RawIngressBrokerProtocol.ReadResponse(Encoding.UTF8.GetBytes(missing)));
        }
        var text = Encoding.UTF8.GetString(bytes);
        foreach (var changed in new[]
        {
            text.Replace("\"expectedFence\":1", "\"expectedFence\":0"),
            text.Replace(valid.Value.AttemptId.ToString("N"), new string('0',32)),
            text[..^1] + ",\"final\":{\"outcomeCode\":\"RAW_EXPORT_SOURCE_BINDING_INVALID\"}}",
            text.Replace("\"handoff\":{", "\"handoff\":{\"subject\":\"forbidden\","),
            text.Replace("\"kind\":\"Handoff\"", "\"kind\":\"Unknown\""),
            text.Replace("\"expectedFence\":1", "\"expectedFence\":1,\"expectedFence\":1"),
        }) Assert.ThrowsAny<Exception>(() => RawIngressBrokerProtocol.ReadResponse(Encoding.UTF8.GetBytes(changed)));
    }

    [Fact]
    public void PrivateFinalCannotInventPostBodyResult()
    {
        foreach (var code in new[] { "CONTENT_COMMITMENT_MISMATCH", "RECAPTURE_REQUIRED", "RAW_EXPORT_SOURCE_ARTIFACT_SIZE_LIMIT_EXCEEDED" })
        {
            Assert.NotEmpty(RawIngressBrokerProtocol.WriteResponse(new RawIngressBrokerResult.Final(new(code), RawIngressBrokerFinalOrigin.PersistedTerminal)));
            if (code != "RAW_EXPORT_SOURCE_ARTIFACT_SIZE_LIMIT_EXCEEDED")
                Assert.ThrowsAny<Exception>(() => RawIngressBrokerProtocol.WriteResponse(new RawIngressBrokerResult.Final(new(code))));
        }
        foreach (var code in new[] { "RAW_EXPORT_SOURCE_AVAILABLE", "SOURCE_ENCRYPTION_FAILED", "RAW_EXPORT_AUTHORITY_INVALID", "ACCESS_DENIED", "RAW_EXPORT_SOURCE_TEMPORARILY_UNAVAILABLE" })
            foreach (var origin in Enum.GetValues<RawIngressBrokerFinalOrigin>())
                Assert.ThrowsAny<Exception>(() => RawIngressBrokerProtocol.WriteResponse(new RawIngressBrokerResult.Final(new(code), origin)));
        var evaluation = new RawIngressBrokerResult.Final(new("RAW_EXPORT_SOURCE_CLAIM_EVALUATION_IN_PROGRESS", RetryNotBeforeUtc: Metadata().SignedAtUtc));
        Assert.Equal(evaluation, RawIngressBrokerProtocol.ReadResponse(RawIngressBrokerProtocol.WriteResponse(evaluation)));
        Assert.ThrowsAny<Exception>(() => RawIngressBrokerProtocol.WriteResponse(evaluation with { Value = evaluation.Value with { RetryNotBeforeUtc = null } }));
        Assert.ThrowsAny<Exception>(() => RawIngressBrokerProtocol.WriteResponse(evaluation with { Value = evaluation.Value with { SourceArtifactId = Guid.NewGuid() } }));
        var published = new RawIngressBrokerResult.Final(new("RAW_EXPORT_SOURCE_ALREADY_AVAILABLE", Guid.NewGuid(), "Available", "Available"), RawIngressBrokerFinalOrigin.PublishedReplay);
        Assert.Equal(published, RawIngressBrokerProtocol.ReadResponse(RawIngressBrokerProtocol.WriteResponse(published)));
        Assert.ThrowsAny<Exception>(() => RawIngressBrokerProtocol.WriteResponse(published with { Origin = RawIngressBrokerFinalOrigin.PreAdmission }));
        Assert.ThrowsAny<Exception>(() => RawIngressBrokerProtocol.WriteResponse(published with { Value = published.Value with { CurrentDisposition = "Staged" } }));
        var pending = new RawIngressBrokerResult.Final(new("RAW_EXPORT_SOURCE_RESUME_PENDING"), RawIngressBrokerFinalOrigin.PreservedCiphertext);
        Assert.Equal(pending, RawIngressBrokerProtocol.ReadResponse(RawIngressBrokerProtocol.WriteResponse(pending)));
        Assert.ThrowsAny<Exception>(() => RawIngressBrokerProtocol.WriteResponse(pending with { Origin = RawIngressBrokerFinalOrigin.PreAdmission }));
    }

    [Fact]
    public void BrokerNetworkConfigurationHasNoMissingValueFallback()
    {
        var config = Tip88C1C6BA3SyntheticComposition.BrokerConfiguration();
        Assert.Equal(1000, RawIngressBrokerOptions.Read(config).RequestTimeoutMilliseconds);
        var keys = config.AsEnumerable().Where(x => x.Value is not null).Select(x => x.Key).ToArray();
        foreach (var key in keys)
        {
            var saved = config[key]; config[key] = null;
            Assert.ThrowsAny<Exception>(() => RawIngressBrokerOptions.Read(config));
            config[key] = saved;
        }
        foreach (var (key, value) in new[] { ("ListenAddress", "0.0.0.0"), ("ListenAddress", "8.8.8.8"),
            ("BaseUri", "http://127.0.0.1/"), ("BaseUri", "http://127.0.0.1:45678/?x=1"),
            ("BaseUri", "http://user@127.0.0.1:45678/"), ("ApiReplicaCount", "2"),
            ("IdempotencyLockTimeoutMilliseconds", "1000"), ("EvaluationTokenTtlSeconds", "1") })
        {
            var path = RawIngressBrokerOptions.SectionName + ":" + key; var saved = config[path]; config[path] = value;
            Assert.ThrowsAny<Exception>(() => RawIngressBrokerOptions.Read(config)); config[path] = saved;
        }
    }

    [Fact]
    public async Task PrivatePeerBoundaryCannotBeSpoofedByForwardedHeader()
    {
        var target = new CountingBroker();
        await using var host = await NetworkHost.Start(target);
        using (var client = new RawIngressBrokerHttpClient(host.Options))
            Assert.IsType<RawIngressBrokerResult.Final>(await client.AdmitAsync(Metadata(), CancellationToken.None));
        Assert.Equal(1, target.Calls);
        using var handler = new SocketsHttpHandler
        {
            UseProxy = false, AllowAutoRedirect = false,
            ConnectCallback = async (_, ct) =>
            {
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.2"), 0));
                try { await socket.ConnectAsync(IPAddress.Loopback, host.Options.BaseUri.Port, ct); return new NetworkStream(socket, true); }
                catch { socket.Dispose(); throw; }
            }
        };
        using var untrusted = new HttpClient(handler);
        using var spoofed = Request(host.Options, RawIngressBrokerProtocol.WriteRequest(Metadata()));
        spoofed.Headers.TryAddWithoutValidation("X-Forwarded-For", "127.0.0.1");
        spoofed.Headers.TryAddWithoutValidation("Forwarded", "for=127.0.0.1");
        using var denied = await untrusted.SendAsync(spoofed);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode); Assert.Empty(await denied.Content.ReadAsByteArrayAsync());
        Assert.Equal(1, target.Calls);
        using var trusted = new HttpClient(new SocketsHttpHandler { UseProxy = false });
        using var wrongHost = Request(host.Options, RawIngressBrokerProtocol.WriteRequest(Metadata()));
        wrongHost.Headers.Host = "localhost:" + host.Options.BaseUri.Port;
        using var wrongHostResponse = await trusted.SendAsync(wrongHost);
        Assert.Equal(HttpStatusCode.Forbidden, wrongHostResponse.StatusCode); Assert.Equal(1, target.Calls);
    }

    [Fact]
    public async Task PrivateHttpRejectsMalformedInputAndLeakingResults()
    {
        var target = new CountingBroker();
        await using var host = await NetworkHost.Start(target);
        using var client = new HttpClient(new SocketsHttpHandler { UseProxy = false });
        using (var valid = Request(host.Options, RawIngressBrokerProtocol.WriteRequest(Metadata())))
        using (var response = await client.SendAsync(valid)) Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        foreach (var payload in new[] { "{}", "{\"raw\":\"forbidden\"}", "null", "{\"protocolVersion\":1,\"protocolVersion\":1}" })
        {
            using var request = Request(host.Options, Encoding.UTF8.GetBytes(payload));
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); Assert.Empty(await response.Content.ReadAsByteArrayAsync());
        }
        Assert.Equal(1, target.Calls);
        target.Result = new RawIngressBrokerResult.Final(new("CONTENT_COMMITMENT_MISMATCH"));
        using (var request = Request(host.Options, RawIngressBrokerProtocol.WriteRequest(Metadata())))
        using (var response = await client.SendAsync(request))
        { Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode); Assert.Empty(await response.Content.ReadAsByteArrayAsync()); }
        target.Error = new InvalidOperationException("synthetic SQL token MUST NOT EGRESS");
        using (var request = Request(host.Options, RawIngressBrokerProtocol.WriteRequest(Metadata())))
        using (var response = await client.SendAsync(request))
        { Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode); Assert.Empty(await response.Content.ReadAsByteArrayAsync()); }
        Assert.Equal(3, target.Calls);
    }

    [Fact]
    public async Task PrivateClientMakesOneSendAndBoundsResponse()
    {
        var options = RawIngressBrokerOptions.Read(Tip88C1C6BA3SyntheticComposition.BrokerConfiguration());
        foreach (var scenario in new[] { "valid", "redirect", "unavailable", "unknown", "oversize", "encoded", "truncated", "exception" })
        {
            var handler = new ResponseHandler(scenario);
            using var client = new RawIngressBrokerHttpClient(options, handler);
            if (scenario == "valid") Assert.Equal(Handoff(), await client.AdmitAsync(Metadata(), CancellationToken.None));
            else Assert.NotNull(await Record.ExceptionAsync(() => client.AdmitAsync(Metadata(), CancellationToken.None)));
            Assert.Equal(1, handler.Calls);
            Assert.True(handler.DisposedContent || scenario == "exception");
        }
    }

    internal static CaptureRuntimeRawIngressAdmissionContext Metadata() => new(
        Guid.Parse("aaaaaaaa111111111111111111111111"), Guid.Parse("bbbbbbbb111111111111111111111111"),
        Guid.Parse("cccccccc111111111111111111111111"), 1, Guid.Parse("dddddddd111111111111111111111111"), 1,
        new DateTimeOffset(2026, 9, 14, 1, 2, 3, TimeSpan.Zero).AddTicks(1), new byte[32], Enumerable.Repeat((byte)255,32).ToArray(),
        1, Guid.Parse("eeeeeeee111111111111111111111111"), Guid.Parse("ffffffff111111111111111111111111"), 1,
        "LiveSelfieImage", Guid.Parse("12345678111111111111111111111111"), "image/jpeg", 24, new string('a',64),
        DateTimeOffset.Parse("2026-09-14T01:02:03Z"), DateTimeOffset.Parse("2026-09-14T01:02:03Z"),
        DateTimeOffset.Parse("2026-09-14T01:07:03Z"), 300);
    private static RawIngressBrokerResult.Handoff Handoff() => new(new(
        Guid.Parse("aaaaaaaa111111111111111111111111"), Guid.Parse("bbbbbbbb111111111111111111111111"),
        Guid.Parse("cccccccc111111111111111111111111"), 1, 1, Guid.Parse("dddddddd111111111111111111111111"),
        Guid.Parse("eeeeeeee111111111111111111111111"), Guid.Parse("ffffffff111111111111111111111111"),
        Guid.Parse("12345678111111111111111111111111"), 1, DateTimeOffset.Parse("2026-09-14T01:07:03Z")));
    private static HttpRequestMessage Request(RawIngressBrokerOptions options, byte[] bytes)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, new Uri(options.BaseUri, RawIngressBrokerOptions.AdmitPath)) { Content = new ByteArrayContent(bytes) };
        request.Content.Headers.ContentType = new("application/json"); return request;
    }
    private sealed class CountingBroker : IRawIngressMetadataBroker
    {
        internal int Calls;
        internal Exception? Error;
        internal RawIngressBrokerResult Result = new RawIngressBrokerResult.Final(new("RAW_EXPORT_SOURCE_BINDING_INVALID"));
        public Task<RawIngressBrokerResult> AdmitAsync(CaptureRuntimeRawIngressAdmissionContext c, CancellationToken ct)
        { Calls++; return Error is null ? Task.FromResult(Result) : Task.FromException<RawIngressBrokerResult>(Error); }
    }
    private sealed class ResponseHandler(string scenario) : HttpMessageHandler
    {
        internal int Calls; internal bool DisposedContent;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls++;
            if (scenario == "exception") throw new IOException("response lost");
            var bytes = scenario == "unknown" ? "{}"u8.ToArray() : RawIngressBrokerProtocol.WriteResponse(Handoff());
            var response = new HttpResponseMessage(scenario == "redirect" ? HttpStatusCode.TemporaryRedirect
                : scenario == "unavailable" ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK)
            { Content = new ObservedContent(bytes, () => DisposedContent = true) };
            response.Content.Headers.ContentType = new("application/json");
            if (scenario == "oversize") response.Content.Headers.ContentLength = 4097;
            if (scenario == "truncated") response.Content.Headers.ContentLength = bytes.Length + 1;
            if (scenario == "encoded") response.Content.Headers.ContentEncoding.Add("gzip");
            return Task.FromResult(response);
        }
    }
    private sealed class ObservedContent(byte[] bytes, Action disposed) : ByteArrayContent(bytes)
    {
        protected override void Dispose(bool disposing) { if (disposing) disposed(); base.Dispose(disposing); }
    }
    internal sealed class NetworkHost(WebApplication app, RawIngressBrokerOptions options) : IAsyncDisposable
    {
        internal RawIngressBrokerOptions Options => options;
        public ValueTask DisposeAsync() => app.DisposeAsync();
        internal static async Task<NetworkHost> Start(IRawIngressMetadataBroker broker, bool loseFirstResponse = false)
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
            builder.Logging.ClearProviders(); builder.WebHost.ConfigureKestrel(k => k.Listen(IPAddress.Loopback, 0));
            var app = builder.Build(); RawIngressBrokerTransport? transport = null; var responses = 0;
            app.Run(async context =>
            {
                var r = context.Request;
                var reply = await transport!.HandleAsync(new(context.Connection.RemoteIpAddress, context.Connection.LocalIpAddress,
                    context.Connection.LocalPort, r.Host.Value, r.Method, r.Path.Value ?? "", r.QueryString.HasValue,
                    r.ContentType ?? "", r.ContentLength, r.Headers.ContainsKey("Content-Encoding") || r.Headers.ContainsKey("Transfer-Encoding"),
                    r.Headers.Host.Count != 1 || r.Headers["Content-Length"].Count != 1 || r.Headers.ContentType.Count != 1), r.Body, context.RequestAborted);
                if (loseFirstResponse && Interlocked.Increment(ref responses) == 1 && reply.StatusCode == 200)
                { context.Abort(); return; }
                context.Response.StatusCode = reply.StatusCode; context.Response.ContentLength = reply.Body.Length;
                if (reply.StatusCode == 200) context.Response.ContentType = "application/json";
                if (reply.Body.Length != 0) await context.Response.Body.WriteAsync(reply.Body, context.RequestAborted);
            });
            await app.StartAsync();
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            var options = RawIngressBrokerOptions.Read(Tip88C1C6BA3SyntheticComposition.BrokerConfiguration(new Uri(address).Port));
            transport = new(options, () => broker);
            return new(app, options);
        }
    }
}

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88C1C6BA3BrokerQualificationTests(PostgresPersistenceFixture postgres)
{
    [Fact]
    public async Task P05_ActiveCommitmentKeyLossReturnsO05WithoutCustodyAndRestoredKeyCanRetry()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_p05_active_key");
        await Tip88C1C6BA3ConsentRetentionTests.Prepare(isolated);
        var scope = await Tip88C1C6BA3RetentionCheckpointTests.Seed(isolated, rawIngress: true);
        await using var observer = isolated.CreateDbContext();
        var before = await Rows();
        var login = await Tip88C1C6BA3SyntheticComposition.BrokerLogin(
            observer.Database.GetConnectionString()!);
        await using var source = NpgsqlDataSource.Create(login);
        var options = RawIngressBrokerOptions.Read(Tip88C1C6BA3SyntheticComposition.BrokerConfiguration());
        var now = await observer.Database.SqlQueryRaw<DateTimeOffset>(
            "SELECT clock_timestamp() AS \"Value\"").SingleAsync();
        var context = new CaptureRuntimeRawIngressAdmissionContext(
            Guid.Parse("40000000-0000-4000-8000-000000000001"),
            Guid.Parse("50000000-0000-4000-8000-000000000001"),
            Guid.Parse("60000000-0000-4000-8000-000000000001"), 1,
            Guid.Parse("10000000-0000-4000-8000-000000000001"), 1,
            now, new byte[32], new byte[32], 1, scope.Session, scope.Artifact, 1,
            "LiveSelfieImage", Guid.NewGuid(), "image/jpeg", 24,
            Convert.ToHexString(SHA256.HashData("synthetic-retainedsource"u8)).ToLowerInvariant(),
            now.AddSeconds(-5), now.AddSeconds(-4), now.AddMinutes(5), 300);

        await using (var unavailable = Tip88C1C6BA3SyntheticComposition.QualifiedBrokerServices(
                         source, options, missingKey: true))
        {
            var broker = unavailable.GetRequiredService<IRawIngressMetadataBroker>();
            var denied = Assert.IsType<RawIngressBrokerResult.Final>(
                await broker.AdmitAsync(context, CancellationToken.None));
            Assert.Equal(RawExportSourceIngressCodes.CapabilityUnavailable, denied.Value.OutcomeCode);
            Assert.Equal(RawIngressBrokerFinalOrigin.PreAdmission, denied.Origin);
        }
        Assert.Equal(before, await Rows()); // no R1, key, object or body work

        await using (var restored = Tip88C1C6BA3SyntheticComposition.QualifiedBrokerServices(
                         source, options))
        {
            var broker = restored.GetRequiredService<IRawIngressMetadataBroker>();
            Assert.IsType<RawIngressBrokerResult.Handoff>(
                await broker.AdmitAsync(context, CancellationToken.None));
        }
        Assert.NotEqual(before, await Rows()); // same metadata enters B after key restoration

        Task<string[]> Rows() => observer.Database.SqlQueryRaw<string>("""
            SELECT family||':'||value AS "Value" FROM (
              SELECT 'alias' family,to_jsonb(r)::text value FROM tagekyc.raw_export_source_ingress_claim_aliases r
              UNION ALL SELECT 'claim',to_jsonb(r)::text FROM tagekyc.raw_export_source_ingress_claims r
              UNION ALL SELECT 'reservation',to_jsonb(r)::text FROM tagekyc.raw_export_source_reservations r
              UNION ALL SELECT 'attempt',to_jsonb(r)::text FROM tagekyc.raw_export_source_encryption_attempts r
              UNION ALL SELECT 'provider-key',to_jsonb(r)::text FROM tagekyc.raw_export_attempt_key_reservations r
              UNION ALL SELECT 'provider-object',to_jsonb(r)::text FROM tagekyc.raw_export_provisional_objects r
            ) rows ORDER BY family,value
            """).ToArrayAsync();
    }

    [Fact]
    public async Task OrdinaryProgramActivatedUsesExplicitSyntheticOwnersOnItsOwnGraph()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_program_graph");
        await using var observer = isolated.CreateDbContext();
        await using var logins = await Tip88C1C6BA3SyntheticComposition.CustodyLogins(
            observer.Database.GetConnectionString()!);
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var objectOptions = new[] { ProvisionalObjectCapability.Writer, ProvisionalObjectCapability.Reconciler,
            ProvisionalObjectCapability.Lifecycle }.Select(capability => minio.Options(capability)).ToArray();
        var brokerOptions = RawIngressBrokerOptions.Read(Tip88C1C6BA3SyntheticComposition.BrokerConfiguration());
        var owners = new CaptureRuntimeRawIngressComposition.RuntimeOwners(
            logins.Connections[0], objectOptions[0], logins.Connections[1], objectOptions[1],
            logins.Connections[2], objectOptions[2], 1_048_576);
        using var factory = ActivatedProgramFactory(brokerOptions, owners);

        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICaptureRuntimeRawIngressAdmission>());
        Assert.Same(factory.Services.GetRequiredService<ICaptureRuntimeA3Readiness>(),
            factory.Services.GetRequiredService<ICaptureRuntimeA3Worker>());
        Assert.True(await factory.Services.GetRequiredService<ICaptureRuntimeA3Readiness>().IsReadyAsync(default));
    }

    [Fact]
    public async Task OrdinaryProgramActivatedInvalidCapacityStopsBeforeAnyIngress()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_program_capacity");
        await using var observer = isolated.CreateDbContext();
        await using var logins = await Tip88C1C6BA3SyntheticComposition.CustodyLogins(
            observer.Database.GetConnectionString()!);
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var objectOptions = new[] { ProvisionalObjectCapability.Writer, ProvisionalObjectCapability.Reconciler,
            ProvisionalObjectCapability.Lifecycle }.Select(capability => minio.Options(capability)).ToArray();
        var brokerOptions = RawIngressBrokerOptions.Read(Tip88C1C6BA3SyntheticComposition.BrokerConfiguration());
        var owners = new CaptureRuntimeRawIngressComposition.RuntimeOwners(
            logins.Connections[0], objectOptions[0], logins.Connections[1], objectOptions[1],
            logins.Connections[2], objectOptions[2], 1_048_576);
        using (var valid = ActivatedProgramFactory(brokerOptions, owners))
        using (var client = valid.CreateClient())
            Assert.True(await valid.Services.GetRequiredService<ICaptureRuntimeA3Readiness>().IsReadyAsync(default));

        var before = await observer.Database.SqlQueryRaw<int>(
            "SELECT count(*)::integer AS \"Value\" FROM tagekyc.raw_export_source_ingress_claims").SingleAsync();
        async Task AssertStopsBeforeIngress(WebApplicationFactory<Program> invalid)
        {
            using (invalid)
            {
                var failure = Assert.ThrowsAny<Exception>(() => invalid.CreateClient());
                Assert.Contains("CAPTURE_RUNTIME_STARTUP_NOT_READY", failure.ToString(), StringComparison.Ordinal);
                Assert.Equal(before, await observer.Database.SqlQueryRaw<int>(
                    "SELECT count(*)::integer AS \"Value\" FROM tagekyc.raw_export_source_ingress_claims").SingleAsync());
            }
        }

        foreach (var (key, value) in new[]
        {
            ("RawExportCustodyMaximumConcurrentStreamsPerProducer", ""),
            ("RawExportCustodyMaximumConcurrentStreamsPerProducer", "-1"),
            ("RawExportCustodyMaximumConcurrentStreamsPerProducer", "not-a-number"),
            ("RawExportCustodyMaximumConcurrentStreamsPerProducer", "2147483648"),
            ("RawExportCustodyMaximumAggregatePlaintextWindowBytesPerDeployment", "9223372036854775808"),
            ("RawExportCustodyMaximumAggregatePlaintextWindowBytesPerDeployment", "512"),
            ("RawExportCustodyMaximumPlaintextWindowBytesPerStream", "1.5")
        })
            await AssertStopsBeforeIngress(ActivatedProgramFactory(brokerOptions, owners, key, value));

        await AssertStopsBeforeIngress(ActivatedProgramFactory(brokerOptions, owners,
            "RawExportCustodyMaximumConcurrentStreamsPerProducer", "3",
            "RawExportCustodyMaximumConcurrentStreamsPerDeployment", "2"));

        // Keep the related operand valid so each upper-bound case reaches its
        // own range guard, rather than failing only the relationship check.
        foreach (var (key, value, companionKey, companionValue) in new[]
        {
            ("RawExportCustodyMaximumConcurrentStreamsPerProducer", "33",
                "RawExportCustodyMaximumConcurrentStreamsPerDeployment", "256"),
            ("RawExportCustodyMaximumConcurrentStreamsPerDeployment", "257", null, null),
            ("RawExportCustodyMaximumAggregatePlaintextWindowBytesPerDeployment", "2147483648", null, null),
            ("RawExportCustodyMaximumPlaintextWindowBytesPerStream", "16777217",
                "RawExportCustodyMaximumAggregatePlaintextWindowBytesPerDeployment", "16777217")
        })
            await AssertStopsBeforeIngress(ActivatedProgramFactory(brokerOptions, owners,
                key, value, companionKey, companionValue));

        foreach (var key in new[]
        {
            "RawExportCustodyMaximumConcurrentStreamsPerProducer",
            "RawExportCustodyMaximumConcurrentStreamsPerDeployment",
            "RawExportCustodyMaximumAggregatePlaintextWindowBytesPerDeployment",
            "RawExportCustodyMaximumPlaintextWindowBytesPerStream"
        })
            await AssertStopsBeforeIngress(ActivatedProgramFactory(brokerOptions, owners, key));

        await AssertStopsBeforeIngress(ActivatedProgramFactory(brokerOptions,
            owners with { MaximumPlaintextWindowBytesPerStream = 0 }));
    }

    [Fact]
    public async Task OrdinaryProgramActivatedInvalidTimeBoundsStopsBeforeAnyIngress()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_program_time_bounds");
        await using var observer = isolated.CreateDbContext();
        await using var logins = await Tip88C1C6BA3SyntheticComposition.CustodyLogins(
            observer.Database.GetConnectionString()!);
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var objectOptions = new[] { ProvisionalObjectCapability.Writer, ProvisionalObjectCapability.Reconciler,
            ProvisionalObjectCapability.Lifecycle }.Select(capability => minio.Options(capability)).ToArray();
        var brokerOptions = RawIngressBrokerOptions.Read(Tip88C1C6BA3SyntheticComposition.BrokerConfiguration());
        var owners = new CaptureRuntimeRawIngressComposition.RuntimeOwners(
            logins.Connections[0], objectOptions[0], logins.Connections[1], objectOptions[1],
            logins.Connections[2], objectOptions[2], 1_048_576);
        using (var valid = ActivatedProgramFactory(brokerOptions, owners))
        using (var client = valid.CreateClient())
            Assert.True(await valid.Services.GetRequiredService<ICaptureRuntimeA3Readiness>().IsReadyAsync(default));

        async Task<string[]> DurableRows() => await observer.Database.SqlQueryRaw<string>("""
            SELECT family||':'||value AS "Value" FROM (
              SELECT 'snapshot' family,to_jsonb(r)::text value FROM tagekyc.raw_export_authority_snapshots r
              UNION ALL SELECT 'alias',to_jsonb(r)::text FROM tagekyc.raw_export_source_ingress_claim_aliases r
              UNION ALL SELECT 'claim',to_jsonb(r)::text FROM tagekyc.raw_export_source_ingress_claims r
              UNION ALL SELECT 'reservation',to_jsonb(r)::text FROM tagekyc.raw_export_source_reservations r
              UNION ALL SELECT 'head',to_jsonb(r)::text FROM tagekyc.raw_export_source_head r
              UNION ALL SELECT 'attempt',to_jsonb(r)::text FROM tagekyc.raw_export_source_encryption_attempts r
              UNION ALL SELECT 'provider-key',to_jsonb(r)::text FROM tagekyc.raw_export_attempt_key_reservations r
              UNION ALL SELECT 'provider-object',to_jsonb(r)::text FROM tagekyc.raw_export_provisional_objects r
            ) rows ORDER BY family,value
            """).ToArrayAsync();
        var before = await DurableRows();
        foreach (var key in new[]
        {
            "RawExportSourceClaimSafetyMarginMilliseconds",
            "RawExportSourceMaximumRemainingContinuationWindowSeconds",
            "RawExportSourceEncryptionAttemptDeadlineSeconds",
            "RawExportSourceOwnershipLeaseDurationSeconds"
        })
        {
            using var invalid = ActivatedProgramFactory(brokerOptions, owners,
                invalidTimeBoundsKey: key, invalidTimeBoundsValue: "0");
            var failure = Assert.ThrowsAny<Exception>(() => invalid.CreateClient());
            Assert.Contains("CAPTURE_RUNTIME_STARTUP_NOT_READY", failure.ToString(), StringComparison.Ordinal);
            Assert.Equal(before, await DurableRows());
        }
    }

    private static WebApplicationFactory<Program> ActivatedProgramFactory(
        RawIngressBrokerOptions brokerOptions, CaptureRuntimeRawIngressComposition.RuntimeOwners owners,
        string? invalidCapacityKey = null, string invalidCapacityValue = "0",
        string? companionCapacityKey = null, string? companionCapacityValue = null,
        string? invalidTimeBoundsKey = null, string invalidTimeBoundsValue = "0") =>
        new HistoricalPreparedWebApplicationFactory()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("environment", "Development");
                builder.UseSetting("TagEkyc:Persistence:Provider", "InMemory");
                foreach (var pair in new[]
                {
                    ("RawExportCustodyMaximumConcurrentStreamsPerProducer", "1"),
                    ("RawExportCustodyMaximumConcurrentStreamsPerDeployment", "2"),
                    ("RawExportCustodyMaximumAggregatePlaintextWindowBytesPerDeployment", "2097152"),
                    ("RawExportCustodyMaximumPlaintextWindowBytesPerStream", "1048576")
                })
                    builder.UseSetting("TagEkyc:RawExport:" + pair.Item1,
                        pair.Item1 == invalidCapacityKey ? invalidCapacityValue
                            : pair.Item1 == companionCapacityKey ? companionCapacityValue : pair.Item2);
                foreach (var pair in new[]
                {
                    ("RawExportSourceClaimSafetyMarginMilliseconds", "1000"),
                    ("RawExportSourceMaximumRemainingContinuationWindowSeconds", "1800"),
                    ("RawExportSourceEncryptionAttemptDeadlineSeconds", "900"),
                    ("RawExportSourceOwnershipLeaseDurationSeconds", "300")
                })
                    builder.UseSetting(pair.Item1,
                        pair.Item1 == invalidTimeBoundsKey ? invalidTimeBoundsValue : pair.Item2);
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<ICaptureRuntimeStartupDependencyReader>();
                    services.AddSingleton<ICaptureRuntimeStartupDependencyReader, ProgramActivatedReader>();
                    services.RemoveAll<ICaptureRuntimeActivationEvidenceSealProvider>();
                    services.AddSingleton<ICaptureRuntimeActivationEvidenceSealProvider>(
                        new ActivationEvidenceTestSeals.Provider(ActivationEvidenceTestSeals.Valid(0)));
                    services.RemoveAll<ICaptureRuntimeSiteTransportQualificationProvider>();
                    services.AddSingleton<ICaptureRuntimeSiteTransportQualificationProvider>(
                        new ActivationEvidenceTestSeals.QualificationProvider());
                    services.RemoveAll<ICaptureRuntimeSiteTransportQualificationSettingsProvider>();
                    services.AddSingleton<ICaptureRuntimeSiteTransportQualificationSettingsProvider>(
                        new ActivationEvidenceTestSeals.QualificationSettingsProvider());
                    services.RemoveAll<IKekOperationProvider>();
                    services.RemoveAll<IKekProvisioningRecoveryOperation>();
                    services.RemoveAll<IContentCommitmentService>();
                    services.RemoveAll<DurableKeyCustodyOptions>();
                    services.AddSingleton<IKekOperationProvider, QualifiedKeys>();
                    services.AddSingleton<IKekProvisioningRecoveryOperation>(sp =>
                        (QualifiedKeys)sp.GetRequiredService<IKekOperationProvider>());
                    services.AddSingleton<IContentCommitmentService>(
                        Tip88C1C6BA3SyntheticComposition.PreflightServices()
                            .GetRequiredService<IContentCommitmentService>());
                    services.AddSingleton(DurableKeyCustodyOptions.Resolve(new ConfigurationManager()));
                    services.AddSingleton(brokerOptions);
                    services.AddSingleton(owners);
                });
            });

    private sealed class ProgramActivatedReader : ICaptureRuntimeStartupDependencyReader
    {
        public Task<CaptureRuntimeStartupDependencies> ReadAsync(DateTimeOffset now, CancellationToken ct) =>
            Task.FromResult(new CaptureRuntimeStartupDependencies("Managed", "Activated", 1,
                now.AddMinutes(-2), now.AddMinutes(-1),
                Guid.Parse("e696004a-fb63-493d-8aa6-036087011e18"), []));
    }

    [Fact]
    public async Task ActivatedReadinessRequiresAcceptancePolicyForEveryRetainedClient()
    {
        var client = Guid.Parse("11111111-1111-4111-8111-111111111111");
        var values = new Dictionary<string, string?>
        {
            ["RawSourceRetentionProfiles:Entries:0:ClientApplicationId"] = client.ToString("D"),
            ["RawSourceRetentionProfiles:Entries:0:PolicyId"] = Guid.Parse("22222222-2222-4222-8222-222222222222").ToString("D"),
            ["RawSourceRetentionProfiles:Entries:0:PolicyVersion"] = "1",
            ["RawSourceRetentionProfiles:Entries:0:RawClasses:0"] = "ChipDg2Portrait",
            ["RawSourceRetentionProfiles:Entries:0:RawClasses:1"] = "LiveSelfieImage",
            ["RawSourceRetentionProfiles:Entries:0:ControllerIdentity"] = "synthetic-controller",
            ["RawSourceRetentionProfiles:Entries:0:StableDataScopeId"] = "synthetic-scope",
            ["RawSourceRetentionProfiles:Entries:0:RetentionPolicyId"] = "synthetic-retention",
            ["RawSourceRetentionProfiles:Entries:0:RetentionPolicyVersion"] = "1",
            ["RawSourceRetentionProfiles:Entries:0:RetentionClass"] = "synthetic",
            ["RawSourceRetentionProfiles:Entries:0:RevocationPolicyId"] = "synthetic-revoke",
            ["RawSourceRetentionProfiles:Entries:0:PurgePolicyId"] = "synthetic-purge",
            ["RawSourceRetentionProfiles:Entries:0:LegalHoldPolicyId"] = "synthetic-hold",
            ["RawSourceRetentionProfiles:Entries:0:MaximumRetentionSeconds"] = "300"
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var validator = new RawSourceRetentionReadinessValidator("must-not-open",
            new RawSourceRetentionProfileValidator(configuration),
            new ConfiguredRawExportCaptureAcceptancePolicyProvider(configuration));

        Assert.False(await validator.IsReadyAsync(default));
    }

    [Fact]
    public async Task ActivatedReadinessRejectsMissingA3CatalogueMember()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_readiness_catalogue");
        await using var observer = isolated.CreateDbContext();
        await using var logins = await Tip88C1C6BA3SyntheticComposition.CustodyLogins(
            observer.Database.GetConnectionString()!);
        var validator = new RawSourceRetentionReadinessValidator(
            logins.Connections[1], new RawSourceRetentionProfileValidator(new ConfigurationManager()),
            new ConfiguredRawExportCaptureAcceptancePolicyProvider(new ConfigurationManager()));

        Assert.True(await validator.IsReadyAsync(default));
        await observer.Database.ExecuteSqlRawAsync("""
            ALTER FUNCTION tagekyc.raw_export_accept_runtime_evidence(uuid,uuid,uuid,text,integer)
            RENAME TO raw_export_accept_runtime_evidence_missing_probe
            """);
        Assert.False(await validator.IsReadyAsync(default));
    }

    [Fact]
    public async Task ActivatedReadinessFailsClosedWhenRequiredHostCapacityIsMissing()
    {
        var disabled = new ProvisionalObjectCustodyOptions(ProvisionalObjectTopology.Disabled, null,
            null, null, null, null, false, 134_217_728, TimeSpan.FromSeconds(300), true);
        var brokerOptions = RawIngressBrokerOptions.Read(Tip88C1C6BA3SyntheticComposition.BrokerConfiguration());
        var services = new ServiceCollection().AddLogging()
            .AddSingleton<ICaptureRuntimeHostLifetime>(new Lifetime())
            .AddSingleton<IKekOperationProvider, QualifiedKeys>()
            .AddSingleton<IKekProvisioningRecoveryOperation>(sp =>
                (QualifiedKeys)sp.GetRequiredService<IKekOperationProvider>())
            .AddSingleton<IContentCommitmentService>(
                Tip88C1C6BA3SyntheticComposition.PreflightServices().GetRequiredService<IContentCommitmentService>())
            .AddSingleton(DurableKeyCustodyOptions.Resolve(new ConfigurationManager()))
            .AddTagEkycCaptureRuntimeRawIngress(brokerOptions, new(
                "invalid-writer", disabled, "invalid-reconciler", disabled,
                "invalid-lifecycle", disabled, 1024));
        await using var provider = services.BuildServiceProvider();

        Assert.False(await provider.GetRequiredService<ICaptureRuntimeA3Readiness>().IsReadyAsync(default));
    }

    [Fact]
    public async Task ActivatedCompositionQualifiesThreeOwnersAndExposesClosedPorts()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_activated_composition");
        await using var observer = isolated.CreateDbContext();
        await using var logins = await Tip88C1C6BA3SyntheticComposition.CustodyLogins(
            observer.Database.GetConnectionString()!);
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var objectOptions = new[] { ProvisionalObjectCapability.Writer, ProvisionalObjectCapability.Reconciler,
            ProvisionalObjectCapability.Lifecycle }.Select(capability => minio.Options(capability)).ToArray();
        var brokerOptions = RawIngressBrokerOptions.Read(Tip88C1C6BA3SyntheticComposition.BrokerConfiguration());
        var timeBounds = new ConfigurationManager
        {
            ["RawExportSourceClaimSafetyMarginMilliseconds"] = "1000",
            ["RawExportSourceMaximumRemainingContinuationWindowSeconds"] = "1800",
            ["RawExportSourceEncryptionAttemptDeadlineSeconds"] = "900",
            ["RawExportSourceOwnershipLeaseDurationSeconds"] = "300"
        };
        var services = new ServiceCollection().AddLogging()
            .AddSingleton<IConfiguration>(timeBounds)
            .AddTagEkycCustodyProfiles(timeBounds)
            .AddSingleton<IRawExportIngressCapacity>(new RawExportIngressCapacity(1, 1, 1024, 1024))
            .AddSingleton<ICaptureRuntimeHostLifetime>(new Lifetime())
            .AddSingleton<IKekOperationProvider, QualifiedKeys>()
            .AddSingleton<IKekProvisioningRecoveryOperation>(sp =>
                (QualifiedKeys)sp.GetRequiredService<IKekOperationProvider>())
            .AddSingleton<IContentCommitmentService>(
                Tip88C1C6BA3SyntheticComposition.PreflightServices().GetRequiredService<IContentCommitmentService>())
            .AddSingleton(DurableKeyCustodyOptions.Resolve(new ConfigurationManager()))
            .AddTagEkycCaptureRuntimeRawIngress(brokerOptions, new(
                logins.Connections[0], objectOptions[0], logins.Connections[1], objectOptions[1],
                logins.Connections[2], objectOptions[2], 1024));
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions
            { ValidateOnBuild = true, ValidateScopes = true });

        Assert.True(await provider.GetRequiredService<ICaptureRuntimeA3Readiness>().IsReadyAsync(default));
        Assert.Same(provider.GetRequiredService<ICaptureRuntimeA3Worker>(),
            provider.GetRequiredService<ICaptureRuntimeA3Readiness>());
        await using var scope = provider.CreateAsyncScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICaptureRuntimeRawIngressAdmission>());
        Assert.IsType<RawIngressBrokerHttpClient>(provider.GetRequiredService<IRawIngressMetadataBroker>());
    }

    [Fact]
    public async Task RoleScopesDoNotCombineObjectCapabilities()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_role_scopes");
        await using var observer = isolated.CreateDbContext();
        await using var logins = await Tip88C1C6BA3SyntheticComposition.CustodyLogins(observer.Database.GetConnectionString()!);
        var connections = logins.Connections;
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var configurations = new[] { ProvisionalObjectCapability.Writer, ProvisionalObjectCapability.Reconciler,
            ProvisionalObjectCapability.Lifecycle }.Select(c => Tip88C1C6BA3SyntheticComposition.ObjectConfiguration(minio.Options(c))).ToArray();
        await using var owners = new CaptureRuntimeCustodyProviderScopes(connections[0], configurations[0],
            connections[1], configurations[1], connections[2], configurations[2]);
        await using var writer = await owners.OpenWriterAsync(default);
        await using var reconciler = await owners.OpenReconcilerAsync(default);
        await using var lifecycle = await owners.OpenLifecycleAsync(default);
        var scopes = new[] { writer, reconciler, lifecycle };
        var expectedLogins = new[] { CaptureRuntimeCustodyProviderScopes.WriterLogin,
            CaptureRuntimeCustodyProviderScopes.ReconcilerLogin, CaptureRuntimeCustodyProviderScopes.LifecycleLogin };
        var types = new[] { typeof(IProvisionalObjectWriter), typeof(IProvisionalObjectReconciler), typeof(IProvisionalObjectLifecycle) };
        var pids = new HashSet<int>();
        for (var i = 0; i < 3; i++)
        {
            var db = scopes[i].Services.GetRequiredService<TagEkycDbContext>();
            Assert.Equal(expectedLogins[i], await db.Database.SqlQueryRaw<string>("SELECT session_user::text AS \"Value\"").SingleAsync());
            Assert.Equal(expectedLogins[i], await db.Database.SqlQueryRaw<string>("SELECT current_user::text AS \"Value\"").SingleAsync());
            Assert.True(pids.Add(await db.Database.SqlQueryRaw<int>("SELECT pg_backend_pid() AS \"Value\"").SingleAsync()));
            for (var other = 0; other < 3; other++)
                Assert.Equal(other == i, scopes[i].Services.GetService(types[other]) is not null);
            Assert.Null(scopes[i].Services.GetService<IProvisionalObjectPostureProbe>());
            Assert.Null(scopes[i].Services.GetService<IRawIngressMetadataBroker>());
        }
        Assert.Equal(3, pids.Count);
        Assert.NotSame(writer.Services.GetRequiredService<NpgsqlDataSource>(), reconciler.Services.GetRequiredService<NpgsqlDataSource>());
        Assert.NotSame(reconciler.Services.GetRequiredService<NpgsqlDataSource>(), lifecycle.Services.GetRequiredService<NpgsqlDataSource>());

        // Actual production S3 implementations, separate MinIO credentials, not
        // only a service-registration census. Content is synthetic ciphertext.
        var id = Guid.NewGuid();
        var locator = new ExactObjectLocator(id, "raw-export/c1/v1/" + id.ToString("N"), SHA256.HashData("scope-proof"u8));
        var bytes = "non-patient synthetic ciphertext"u8.ToArray();
        using var stream = new MemoryStream(bytes, writable: false);
        var put = await writer.Services.GetRequiredService<IProvisionalObjectWriter>()
            .PutIfAbsentAsync(new(locator, Guid.NewGuid(), bytes.Length), stream, default);
        Assert.Equal(ConditionalPutOutcome.Created, put.Outcome);
        var reader = reconciler.Services.GetRequiredService<IProvisionalObjectReconciler>();
        var present = await reader.InspectExactAsync(locator, default);
        Assert.Equal(ExactObjectInspectionOutcome.Present, present.Outcome);
        Assert.Equal(locator.ObjectBindingDigest, present.ObjectBindingDigest);
        await using (var read = await reader.OpenExactReadAsync(locator, default))
        {
            using var copy = new MemoryStream(); await read.Ciphertext.CopyToAsync(copy);
            Assert.Equal(bytes, copy.ToArray());
        }
        // Verify the credentials behind those scopes cannot do another role's
        // operation, even if a consumer were to construct a client itself.
        using var writerClient = minio.CreateCapabilityClient(ProvisionalObjectCapability.Writer);
        using var reconcilerClient = minio.CreateCapabilityClient(ProvisionalObjectCapability.Reconciler);
        using var lifecycleClient = minio.CreateCapabilityClient(ProvisionalObjectCapability.Lifecycle);
        Assert.Equal(HttpStatusCode.Forbidden, (await Assert.ThrowsAsync<Amazon.S3.AmazonS3Exception>(
            () => writerClient.GetObjectMetadataAsync(minio.BucketName, locator.ObjectKey))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Assert.ThrowsAsync<Amazon.S3.AmazonS3Exception>(
            () => reconcilerClient.DeleteObjectAsync(minio.BucketName, locator.ObjectKey))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Assert.ThrowsAsync<Amazon.S3.AmazonS3Exception>(
            () => lifecycleClient.GetObjectMetadataAsync(minio.BucketName, locator.ObjectKey))).StatusCode);
        Assert.Equal(ExactDeleteOutcome.DeletedAcknowledged, (await lifecycle.Services.GetRequiredService<IProvisionalObjectLifecycle>()
            .DeleteExactAsync(locator, default)).Outcome);
        Assert.Equal(ExactObjectInspectionOutcome.PositivelyAbsent, (await reader.InspectExactAsync(locator, default)).Outcome);

        await writer.DisposeAsync();
        Assert.Throws<ObjectDisposedException>(() => writer.Services.GetRequiredService<TagEkycDbContext>());
        await using var next = await owners.OpenWriterAsync(default);
        Assert.NotSame(writer, next);
        Assert.Equal(CaptureRuntimeCustodyProviderScopes.WriterLogin, await next.Services.GetRequiredService<TagEkycDbContext>()
            .Database.SqlQueryRaw<string>("SELECT session_user::text AS \"Value\"").SingleAsync());
    }

    private sealed class Lifetime : ICaptureRuntimeHostLifetime
    {
        public CancellationToken Stopping => CancellationToken.None;
    }

    private sealed class QualifiedKeys : IKekOperationProvider, IKekProvisioningRecoveryOperation,
        IDurableKekProviderCapabilitySource
    {
        public DurableKekProviderCapabilities Capabilities => new(true, true, true, true);
        public Task<KekWrapResult> WrapDekAsync(KekReference reference, ProviderOperationToken providerOperationToken,
            ReadOnlyMemory<byte> attemptKeyContextFingerprint, IAttemptDekCandidate candidate,
            CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<KekOperationLookup> LookupByOperationTokenAsync(ProviderOperationToken providerOperationToken,
            ReadOnlyMemory<byte> attemptKeyContextFingerprint, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<AttemptDekLease> UnwrapDekAsync(KekReference reference, KekWrappedMaterial wrapped,
            ReadOnlyMemory<byte> attemptKeyContextFingerprint, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<KekProvisioningResolution> ResolveProvisioningOperationAsync(
            ProviderOperationToken providerOperationToken, ReadOnlyMemory<byte> attemptKeyContextFingerprint,
            CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<KekProvisioningCleanupResult> CleanupProvisioningOperationAsync(string providerCleanupReference,
            ReadOnlyMemory<byte> attemptKeyContextFingerprint, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    [Theory]
    [InlineData("combined-login")]
    [InlineData("transitive-role")]
    [InlineData("incoming-login")]
    [InlineData("set-role")]
    public async Task ApiCustodyRoleScopes_RejectActualDatabaseRoleDrift(string defect)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_scope_drift");
        await using var observer = isolated.CreateDbContext();
        await using var logins = await Tip88C1C6BA3SyntheticComposition.CustodyLogins(observer.Database.GetConnectionString()!);
        var connections = logins.Connections;
        var configs = ScopeConfigurations();
        await using var owners = new CaptureRuntimeCustodyProviderScopes(connections[0], configs[0], connections[1], configs[1], connections[2], configs[2]);
        await using (var green = await owners.OpenWriterAsync(default)) Assert.NotNull(green.Services.GetService<IProvisionalObjectWriter>());
        var mutation = defect switch
        {
            "combined-login" => "GRANT tagekyc_raw_export_reconciler TO tagekyc_raw_export_encryptor_login WITH ADMIN FALSE, INHERIT TRUE, SET FALSE",
            "transitive-role" => "GRANT tagekyc_runtime TO tagekyc_raw_export_custody_encryptor WITH ADMIN FALSE, INHERIT TRUE, SET FALSE",
            "incoming-login" => "GRANT tagekyc_raw_export_encryptor_login TO tagekyc_runtime WITH ADMIN FALSE, INHERIT TRUE, SET FALSE",
            _ => "GRANT tagekyc_raw_export_custody_encryptor TO tagekyc_raw_export_encryptor_login WITH ADMIN FALSE, INHERIT TRUE, SET TRUE"
        };
        var restore = defect switch
        {
            "combined-login" => "REVOKE tagekyc_raw_export_reconciler FROM tagekyc_raw_export_encryptor_login",
            "transitive-role" => "REVOKE tagekyc_runtime FROM tagekyc_raw_export_custody_encryptor",
            "incoming-login" => "REVOKE tagekyc_raw_export_encryptor_login FROM tagekyc_runtime",
            _ => "GRANT tagekyc_raw_export_custody_encryptor TO tagekyc_raw_export_encryptor_login WITH ADMIN FALSE, INHERIT TRUE, SET FALSE"
        };
        try
        {
            await observer.Database.ExecuteSqlRawAsync(mutation);
            if (defect is "transitive-role" or "incoming-login")
                Assert.Equal("RAW_INGRESS_CUSTODY_SCOPE_ACTUAL_DATABASE_ACTOR", (await Assert.ThrowsAsync<InvalidOperationException>(
                    () => owners.OpenWriterAsync(default))).Message);
            else
                Assert.Equal(defect == "combined-login" ? "PROD_RAW_EXPORT_CUSTODY_ROLE_GRANT_INVALID" : "PROD_RAW_EXPORT_CUSTODY_ROLE_SET_ROLE_ENABLED",
                    (await Assert.ThrowsAsync<DurableKeyReadinessException>(() => owners.OpenWriterAsync(default))).Message);
        }
        finally { await observer.Database.ExecuteSqlRawAsync(restore); }
        await using var restored = await owners.OpenWriterAsync(default);
        Assert.NotNull(restored.Services.GetService<IProvisionalObjectWriter>());
    }

    [Fact]
    public async Task ApiCustodyRoleScopes_DoNotEnlistInCallerAmbientTransaction()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_scope_ambient");
        await using var observer = isolated.CreateDbContext();
        await using var logins = await Tip88C1C6BA3SyntheticComposition.CustodyLogins(observer.Database.GetConnectionString()!);
        var c = logins.Connections; var configs = ScopeConfigurations();
        await using var owners = new CaptureRuntimeCustodyProviderScopes(c[0], configs[0], c[1], configs[1], c[2], configs[2]);
        using (var ambient = new System.Transactions.TransactionScope(System.Transactions.TransactionScopeOption.RequiresNew,
            System.Transactions.TransactionScopeAsyncFlowOption.Enabled))
        {
            Assert.NotNull(System.Transactions.Transaction.Current);
            await using var role = await owners.OpenReconcilerAsync(default);
            var db = role.Services.GetRequiredService<TagEkycDbContext>();
            // Run the real stage transaction owner, not only BeginTransaction in
            // a test helper. Missing target yields its typed NotFound normally.
            var result = await new RawExportR3StagingService(db).StageAsync(new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, 1, 1, 1));
            Assert.Equal(RawExportR3StageDisposition.NotFound, result.Disposition);
            // A later independently opened connection must also remain outside
            // ambient enlistment; qualification-only suppression would fail here.
            await using var later = await role.Services.GetRequiredService<NpgsqlDataSource>().OpenConnectionAsync();
            await using var tx = await later.BeginTransactionAsync();
            await using var sql = new NpgsqlCommand("SELECT 1", later, tx);
            Assert.Equal(1, await sql.ExecuteScalarAsync());
            await tx.CommitAsync();
            Assert.False(new NpgsqlConnectionStringBuilder(role.Services.GetRequiredService<NpgsqlDataSource>().ConnectionString).Enlist);
            // Deliberately do not complete the outer caller transaction.
        }
        await using var restored = await owners.OpenReconcilerAsync(default);
        Assert.Equal(CaptureRuntimeCustodyProviderScopes.ReconcilerLogin,
            await restored.Services.GetRequiredService<TagEkycDbContext>().Database.SqlQueryRaw<string>(
                "SELECT session_user::text AS \"Value\"").SingleAsync());
    }

    [Theory]
    [InlineData("wrong-login")]
    [InlineData("different-database")]
    [InlineData("connection-options")]
    [InlineData("missing-credential")]
    [InlineData("wrong-capability")]
    [InlineData("different-bucket")]
    [InlineData("combined-object-credential")]
    public void ApiCustodyRoleScopes_RejectMissingOrCombinedOwnersBeforeConnection(string defect)
    {
        var configs = ScopeConfigurations();
        var connections = new[] { CaptureRuntimeCustodyProviderScopes.WriterLogin,
            CaptureRuntimeCustodyProviderScopes.ReconcilerLogin, CaptureRuntimeCustodyProviderScopes.LifecycleLogin }
            .Select(login => $"Host=127.0.0.1;Port=1;Database=synthetic;Username={login}").ToArray();
        const string p = "TagEkyc:RawExport:ObjectCustody:";
        var expected = "RAW_INGRESS_CUSTODY_SCOPE_";
        switch (defect)
        {
            case "wrong-login": connections[0] = connections[1]; expected += "CONNECTION_OWNER"; break;
            case "different-database": connections[2] += ";Database=other"; expected += "CONNECTION_OWNER"; break;
            case "connection-options": connections[0] += ";Options=-c role=tagekyc"; expected += "CONNECTION_OWNER"; break;
            case "missing-credential": configs[0][p + "SecretAccessKey"] = null; expected += "OBJECT_OWNER"; break;
            case "wrong-capability": configs[1][p + "Capability"] = "Writer"; expected += "OBJECT_OWNER"; break;
            case "different-bucket": configs[2][p + "BucketName"] = "other-bucket"; expected += "OBJECT_OWNER"; break;
            default: configs[2][p + "AccessKeyId"] = configs[0][p + "AccessKeyId"]; expected += "COMBINED_OBJECT_CREDENTIAL"; break;
        }
        Assert.Equal(expected, Assert.Throws<InvalidOperationException>(() => new CaptureRuntimeCustodyProviderScopes(
            connections[0], configs[0], connections[1], configs[1], connections[2], configs[2])).Message);
    }

    [Theory]
    [InlineData(0,"direct")]
    [InlineData(1,"direct")]
    [InlineData(2,"direct")]
    [InlineData(0,"public")]
    [InlineData(1,"public")]
    [InlineData(2,"public")]
    [InlineData(0,"column")]
    [InlineData(1,"column")]
    [InlineData(2,"column")]
    [InlineData(0,"missing")]
    [InlineData(1,"missing")]
    [InlineData(2,"missing")]
    [InlineData(0,"broker")]
    [InlineData(1,"broker")]
    [InlineData(2,"broker")]
    [InlineData(0,"table-read")]
    [InlineData(1,"public-read")]
    [InlineData(2,"column-read")]
    [InlineData(0,"grant-option")]
    [InlineData(1,"grant-option")]
    [InlineData(2,"grant-option")]
    public async Task ApiCustodyRoleScopes_RejectCrossRoleSqlPrivileges(int role, string defect)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_scope_acl");
        await using var observer = isolated.CreateDbContext();
        await using var logins = await Tip88C1C6BA3SyntheticComposition.CustodyLogins(observer.Database.GetConnectionString()!);
        var c = logins.Connections; var configs = ScopeConfigurations();
        await using var owners = new CaptureRuntimeCustodyProviderScopes(c[0], configs[0], c[1], configs[1], c[2], configs[2]);
        Func<Task<CaptureRuntimeCustodyProviderScopes.RoleScope>> open = role switch
        {
            0 => () => owners.OpenWriterAsync(default), 1 => () => owners.OpenReconcilerAsync(default),
            _ => () => owners.OpenLifecycleAsync(default)
        };
        var login = new NpgsqlConnectionStringBuilder(c[role]).Username;
        var otherFunction = role switch
        {
            0 => "raw_export_read_provisional_object_reconcile_context(uuid)",
            1 => "raw_export_read_provisional_object_lifecycle_context(uuid)",
            _ => "raw_export_read_source_encryption_context(uuid,bigint,bigint)"
        };
        var grantee = defect is "public" or "public-read" ? "PUBLIC" : login;
        var permission = defect == "column" ? "UPDATE(\"State\") ON TABLE tagekyc.capture_capabilities"
            : "EXECUTE ON FUNCTION tagekyc." + otherFunction;
        if (defect == "missing")
        {
            grantee = new[] { "tagekyc_raw_export_custody_encryptor", "tagekyc_raw_export_reconciler", "tagekyc_raw_export_lifecycle" }[role];
            permission = "EXECUTE ON FUNCTION tagekyc." + CaptureRuntimeCustodyProviderScopes.StageRights.First(x => (x.Roles & (1 << role)) != 0).Signature;
        }
        if (defect == "broker") permission = "EXECUTE ON FUNCTION " + QualifiedRawIngressBroker.Functions[0];
        if (defect is "table-read" or "public-read") permission = "SELECT ON TABLE tagekyc.raw_export_attempt_key_reservations";
        if (defect == "column-read") permission = "SELECT(\"AttemptId\") ON TABLE tagekyc.raw_export_attempt_key_reservations";
        if (defect == "grant-option") permission = "EXECUTE ON FUNCTION tagekyc." + CaptureRuntimeCustodyProviderScopes.StageRights.First(x => (x.Roles & (1 << role)) != 0).Signature;
        var mutation = defect == "missing" ? $"REVOKE {permission} FROM {grantee}" : $"GRANT {permission} TO {grantee}";
        if (defect == "grant-option") mutation += " WITH GRANT OPTION";
        var restore = defect == "missing" ? $"GRANT {permission} TO {grantee}" : $"REVOKE {permission} FROM {grantee}";
        await using (var green = await open()) Assert.NotNull(green.Services.GetService<TagEkycDbContext>());
        try
        {
            await observer.Database.ExecuteSqlRawAsync(mutation);
            Assert.Equal("RAW_INGRESS_CUSTODY_SCOPE_STAGE_PRIVILEGES",
                (await Assert.ThrowsAsync<InvalidOperationException>(open)).Message);
        }
        finally { await observer.Database.ExecuteSqlRawAsync(restore); }
        await using var restored = await open();
        Assert.NotNull(restored.Services.GetService<TagEkycDbContext>());
    }

    [Fact]
    public async Task ApiCustodyRoleScopes_StageManifestMatchesExactMigratedGrants()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_scope_manifest");
        await using var observer = isolated.CreateDbContext();
        var rows = await observer.Database.SqlQueryRaw<ScopeGrantRow>("""
            SELECT p.oid::regprocedure::text AS "Signature", sum(CASE r.rolname
              WHEN 'tagekyc_raw_export_custody_encryptor' THEN 1
              WHEN 'tagekyc_raw_export_reconciler' THEN 2 ELSE 4 END)::integer AS "Roles"
            FROM pg_catalog.pg_proc p JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
            CROSS JOIN LATERAL pg_catalog.aclexplode(p.proacl) a JOIN pg_catalog.pg_roles r ON r.oid=a.grantee
            WHERE n.nspname='tagekyc' AND a.privilege_type='EXECUTE'
              AND r.rolname IN ('tagekyc_raw_export_custody_encryptor','tagekyc_raw_export_reconciler','tagekyc_raw_export_lifecycle')
            GROUP BY p.oid
            """).ToListAsync();
        Assert.Equal(41, rows.Count);
        Assert.Equal(41, CaptureRuntimeCustodyProviderScopes.StageRights.Count);
        // PostgreSQL regprocedure prints timestamptz as timestamp with time zone;
        // compare exact OIDs via regprocedure rather than a display formatter.
        foreach (var expected in CaptureRuntimeCustodyProviderScopes.StageRights)
        {
            var signature = await observer.Database.SqlQuery<string>(
                $"SELECT ({"tagekyc." + expected.Signature}::regprocedure)::text AS \"Value\"").SingleAsync();
            Assert.Equal(expected.Roles, Assert.Single(rows, r => r.Signature == signature).Roles);
        }
    }
    private sealed record ScopeGrantRow(string Signature, int Roles);

    private static ConfigurationManager[] ScopeConfigurations() =>
        new[] { ProvisionalObjectCapability.Writer, ProvisionalObjectCapability.Reconciler, ProvisionalObjectCapability.Lifecycle }
            .Select(role => Tip88C1C6BA3SyntheticComposition.ObjectConfiguration(new(ProvisionalObjectTopology.S3CompatibleDurable,
                role, new Uri("http://127.0.0.1:1/"), "synthetic-bucket", "synthetic-" + role, "synthetic-secret-" + role,
                true, 134217728, TimeSpan.FromSeconds(300), true))).ToArray();

    [Fact]
    public async Task BrokerQualification_FrozenStageCensusMatchesMigratedAuthority()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_broker_census");
        await using var observer = isolated.CreateDbContext();
        // This is a test of the unmodified migrated authority, not the product
        // classifier's input. Future stage grants must extend the frozen guard.
        var stageNames = await observer.Database.SqlQueryRaw<string>("""
            SELECT DISTINCT p.proname::text AS "Value" FROM pg_catalog.pg_proc p
            JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
            WHERE n.nspname='tagekyc' AND EXISTS(SELECT 1 FROM pg_catalog.aclexplode(p.proacl) acl
              JOIN pg_catalog.pg_roles r ON r.oid=acl.grantee
              WHERE r.rolname IN ('tagekyc_raw_export_custody_encryptor',
                'tagekyc_raw_export_reconciler','tagekyc_raw_export_lifecycle') AND acl.privilege_type='EXECUTE')
            ORDER BY "Value"
            """).ToArrayAsync();
        Assert.Equal(41, stageNames.Length);
        Assert.Equal(43, QualifiedRawIngressBroker.ForbiddenFunctions.Length);
        Assert.Equal(stageNames.Append("raw_export_begin_source_ingress_with_authority_core")
            .Append("raw_export_reenter_retained_source").Order(),
            QualifiedRawIngressBroker.ForbiddenFunctions.Order());
    }

    [Fact]
    public async Task BrokerQualification_RealPrivateHttpCommitsAsDedicatedLogin()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_qualified_http");
        await Tip88C1C6BA3ConsentRetentionTests.Prepare(isolated);
        var seeded = await Tip88C1C6BA3RetentionCheckpointTests.Seed(isolated, rawIngress: true);
        await using var observer = isolated.CreateDbContext();
        var connection = await Tip88C1C6BA3SyntheticComposition.BrokerLogin(observer.Database.GetConnectionString()!);
        await using var source = NpgsqlDataSource.Create(connection);
        // The observer records session_user at the actual durable insertion,
        // not a connection-string username or a test-local selected role.
        await observer.Database.ExecuteSqlRawAsync("""
            CREATE SCHEMA a3_login_proof;
            CREATE TABLE a3_login_proof.writes(principal text NOT NULL);
            CREATE FUNCTION a3_login_proof.observe() RETURNS trigger LANGUAGE plpgsql SECURITY DEFINER
              SET search_path=pg_catalog AS $proof$
            BEGIN INSERT INTO a3_login_proof.writes VALUES(session_user); RETURN NEW; END $proof$;
            CREATE TRIGGER a3_login_observe AFTER INSERT ON tagekyc.raw_export_source_encryption_attempts
              FOR EACH ROW EXECUTE FUNCTION a3_login_proof.observe();
            """);
        var options = RawIngressBrokerOptions.Read(Tip88C1C6BA3SyntheticComposition.BrokerConfiguration());
        await using var services = Tip88C1C6BA3SyntheticComposition.QualifiedBrokerServices(source, options);
        var broker = Assert.IsType<QualifiedRawIngressBroker>(services.GetRequiredService<IRawIngressMetadataBroker>());
        await using var host = await Tip88C1C6BA3BrokerHttpTests.NetworkHost.Start(broker);
        using var client = new RawIngressBrokerHttpClient(host.Options);
        var now = await observer.Database.SqlQueryRaw<DateTimeOffset>("SELECT clock_timestamp() AS \"Value\"").SingleAsync();
        var context = new CaptureRuntimeRawIngressAdmissionContext(
            Guid.Parse("40000000-0000-4000-8000-000000000001"), Guid.Parse("50000000-0000-4000-8000-000000000001"),
            Guid.Parse("60000000-0000-4000-8000-000000000001"), 1,
            Guid.Parse("10000000-0000-4000-8000-000000000001"), 1, now, new byte[32], new byte[32],
            1, seeded.Session, seeded.Artifact, 1, "LiveSelfieImage", Guid.NewGuid(), "image/jpeg", 24,
            Convert.ToHexString(SHA256.HashData("synthetic-retainedsource"u8)).ToLowerInvariant(),
            now.AddSeconds(-5), now.AddSeconds(-4), now.AddMinutes(5), 300);
        var handoff = Assert.IsType<RawIngressBrokerResult.Handoff>(await client.AdmitAsync(context, CancellationToken.None)).Value;
        Assert.Equal(QualifiedRawIngressBroker.Login, await observer.Database.SqlQueryRaw<string>(
            "SELECT principal AS \"Value\" FROM a3_login_proof.writes").SingleAsync());
        var stored = await observer.RawExportSourceEncryptionAttempts.AsNoTracking()
            .Where(x => x.AttemptId == handoff.AttemptId).SingleAsync();
        Assert.Equal(handoff.SourceArtifactId, stored.SourceArtifactId);
        Assert.Equal(handoff.ExpectedFence, stored.Fence);
        Assert.Equal(seeded.RuntimeBinding, handoff.BindingId);
        // Receipt loss/replay does not turn successful host qualification into
        // permission to produce another R1 or re-arm the existing attempt.
        var replay = Assert.IsType<RawIngressBrokerResult.Final>(await client.AdmitAsync(context, CancellationToken.None));
        Assert.Equal("RAW_EXPORT_SOURCE_RESERVATION_BUSY", replay.Value.OutcomeCode);
        Assert.Equal(1, await observer.Database.SqlQueryRaw<int>(
            "SELECT count(*)::integer AS \"Value\" FROM a3_login_proof.writes").SingleAsync());
    }

    [Theory]
    [InlineData("wrong-login")]
    [InlineData("role-option")]
    [InlineData("extra-membership")]
    [InlineData("missing-membership")]
    [InlineData("role-admin")]
    [InlineData("create-role")]
    [InlineData("bypass-rls")]
    [InlineData("direct-table")]
    [InlineData("direct-column")]
    [InlineData("inherited-column")]
    [InlineData("public-column")]
    [InlineData("inverse-membership")]
    [InlineData("inverse-owner-membership")]
    [InlineData("inverse-owner-set-only")]
    [InlineData("inverse-broker-set-only")]
    [InlineData("inverse-superuser")]
    [InlineData("stage-execute")]
    [InlineData("stage-moved")]
    [InlineData("core-execute")]
    [InlineData("public-execute")]
    [InlineData("missing-execute")]
    [InlineData("search-path")]
    [InlineData("missing-key")]
    [InlineData("missing-provider")]
    [InlineData("invalid-profile")]
    public async Task BrokerQualification_RealLoginAndProviderFailClosed(string defect)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_qualification");
        await using var observer = isolated.CreateDbContext();
        var admin = observer.Database.GetConnectionString()!;
        var brokerConnection = await Tip88C1C6BA3SyntheticComposition.BrokerLogin(admin);
        await using var source = NpgsqlDataSource.Create(brokerConnection);
        var options = RawIngressBrokerOptions.Read(Tip88C1C6BA3SyntheticComposition.BrokerConfiguration());
        await using var positive = Tip88C1C6BA3SyntheticComposition.QualifiedBrokerServices(source, options);
        var broker = Assert.IsType<QualifiedRawIngressBroker>(positive.GetRequiredService<IRawIngressMetadataBroker>());
        await broker.QualifyAsync(CancellationToken.None); // identical real-provider positive control
        var before = await Snapshot();
        var function = QualifiedRawIngressBroker.Functions[0];
        var stageFunction = await observer.Database.SqlQueryRaw<string>("""
            SELECT p.oid::regprocedure::text AS "Value" FROM pg_catalog.pg_proc p
            JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
            WHERE n.nspname='tagekyc' AND EXISTS(SELECT 1 FROM pg_catalog.aclexplode(p.proacl) acl
              WHERE acl.grantee='tagekyc_raw_export_reconciler'::regrole AND acl.privilege_type='EXECUTE')
              AND NOT pg_catalog.has_function_privilege('tagekyc_raw_export_claim_broker',p.oid,'EXECUTE')
            ORDER BY p.oid LIMIT 1
            """).SingleAsync();
        var coreFunction = await observer.Database.SqlQueryRaw<string>("""
            SELECT p.oid::regprocedure::text AS "Value" FROM pg_catalog.pg_proc p
            JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
            WHERE n.nspname='tagekyc' AND p.proname='raw_export_begin_source_ingress_with_authority_core'
            """).SingleAsync();
        var change = defect switch
        {
            "extra-membership" => "GRANT tagekyc_raw_export_reconciler TO tagekyc_raw_export_claim_broker_login",
            "missing-membership" => "REVOKE tagekyc_runtime FROM tagekyc_raw_export_claim_broker_login",
            "role-admin" => "GRANT tagekyc_runtime TO tagekyc_raw_export_claim_broker_login WITH ADMIN TRUE",
            "create-role" => "ALTER ROLE tagekyc_raw_export_claim_broker_login CREATEROLE",
            "bypass-rls" => "ALTER ROLE tagekyc_raw_export_claim_broker_login BYPASSRLS",
            "direct-table" => "GRANT SELECT ON tagekyc.raw_export_source_encryption_attempts TO tagekyc_raw_export_claim_broker_login",
            "direct-column" => "GRANT SELECT(\"AttemptId\") ON tagekyc.raw_export_source_encryption_attempts TO tagekyc_raw_export_claim_broker_login",
            "inherited-column" => "GRANT UPDATE(\"Fence\") ON tagekyc.raw_export_source_encryption_attempts TO tagekyc_raw_export_claim_broker",
            "public-column" => "GRANT UPDATE(\"Fence\") ON tagekyc.raw_export_source_encryption_attempts TO PUBLIC",
            "inverse-membership" => "GRANT tagekyc_raw_export_claim_broker TO tagekyc_capture_runtime_authenticator",
            "inverse-owner-membership" => "GRANT tagekyc_raw_export_deployer TO tagekyc_capture_runtime_authenticator",
            "inverse-owner-set-only" => "GRANT tagekyc_raw_export_deployer TO tagekyc_capture_runtime_authenticator WITH INHERIT FALSE, SET TRUE",
            "inverse-broker-set-only" => "GRANT tagekyc_raw_export_claim_broker TO tagekyc_capture_runtime_authenticator WITH INHERIT FALSE, SET TRUE",
            "inverse-superuser" => "ALTER ROLE tagekyc_capture_runtime_authenticator SUPERUSER",
            "stage-execute" => $"GRANT EXECUTE ON FUNCTION {stageFunction} TO tagekyc_raw_export_claim_broker",
            "stage-moved" => $"REVOKE EXECUTE ON FUNCTION {stageFunction} FROM tagekyc_raw_export_reconciler; GRANT EXECUTE ON FUNCTION {stageFunction} TO tagekyc_raw_export_claim_broker",
            "core-execute" => $"GRANT EXECUTE ON FUNCTION {coreFunction} TO tagekyc_raw_export_claim_broker",
            "public-execute" => $"GRANT EXECUTE ON FUNCTION {function} TO PUBLIC",
            "missing-execute" => $"REVOKE EXECUTE ON FUNCTION {function} FROM tagekyc_raw_export_claim_broker",
            "search-path" => $"ALTER FUNCTION {function} SET search_path=pg_catalog,tagekyc",
            _ => "SELECT 1"
        };
        var restore = defect switch
        {
            "extra-membership" => "REVOKE tagekyc_raw_export_reconciler FROM tagekyc_raw_export_claim_broker_login",
            "missing-membership" => "GRANT tagekyc_runtime TO tagekyc_raw_export_claim_broker_login WITH ADMIN FALSE, INHERIT TRUE, SET FALSE",
            "role-admin" => "REVOKE ADMIN OPTION FOR tagekyc_runtime FROM tagekyc_raw_export_claim_broker_login",
            "create-role" => "ALTER ROLE tagekyc_raw_export_claim_broker_login NOCREATEROLE",
            "bypass-rls" => "ALTER ROLE tagekyc_raw_export_claim_broker_login NOBYPASSRLS",
            "direct-table" => "REVOKE SELECT ON tagekyc.raw_export_source_encryption_attempts FROM tagekyc_raw_export_claim_broker_login",
            "direct-column" => "REVOKE SELECT(\"AttemptId\") ON tagekyc.raw_export_source_encryption_attempts FROM tagekyc_raw_export_claim_broker_login",
            "inherited-column" => "REVOKE UPDATE(\"Fence\") ON tagekyc.raw_export_source_encryption_attempts FROM tagekyc_raw_export_claim_broker",
            "public-column" => "REVOKE UPDATE(\"Fence\") ON tagekyc.raw_export_source_encryption_attempts FROM PUBLIC",
            "inverse-membership" => "REVOKE tagekyc_raw_export_claim_broker FROM tagekyc_capture_runtime_authenticator",
            "inverse-owner-membership" => "REVOKE tagekyc_raw_export_deployer FROM tagekyc_capture_runtime_authenticator",
            "inverse-owner-set-only" => "REVOKE tagekyc_raw_export_deployer FROM tagekyc_capture_runtime_authenticator",
            "inverse-broker-set-only" => "REVOKE tagekyc_raw_export_claim_broker FROM tagekyc_capture_runtime_authenticator",
            "inverse-superuser" => "ALTER ROLE tagekyc_capture_runtime_authenticator NOSUPERUSER",
            "stage-execute" => $"REVOKE EXECUTE ON FUNCTION {stageFunction} FROM tagekyc_raw_export_claim_broker",
            "stage-moved" => $"REVOKE EXECUTE ON FUNCTION {stageFunction} FROM tagekyc_raw_export_claim_broker; GRANT EXECUTE ON FUNCTION {stageFunction} TO tagekyc_raw_export_reconciler",
            "core-execute" => $"REVOKE EXECUTE ON FUNCTION {coreFunction} FROM tagekyc_raw_export_claim_broker",
            "public-execute" => $"REVOKE EXECUTE ON FUNCTION {function} FROM PUBLIC",
            "missing-execute" => $"GRANT EXECUTE ON FUNCTION {function} TO tagekyc_raw_export_claim_broker",
            "search-path" => $"ALTER FUNCTION {function} SET search_path=pg_catalog",
            _ => "SELECT 1"
        };
        try
        {
            await observer.Database.ExecuteSqlRawAsync(change);
            if (defect is "inverse-owner-set-only" or "inverse-broker-set-only")
            {
                // Prove that this is the SET-only counterexample, not another
                // immediately inherited EXECUTE grant in disguise.
                Assert.False(await observer.Database.SqlQuery<bool>($"""
                    SELECT pg_catalog.has_function_privilege('tagekyc_capture_runtime_authenticator',
                      {function},'EXECUTE') AS "Value"
                    """).SingleAsync());
                var role = defect == "inverse-owner-set-only" ? "tagekyc_raw_export_deployer" : "tagekyc_raw_export_claim_broker";
                Assert.True(await observer.Database.SqlQuery<bool>($"""
                    SELECT pg_catalog.pg_has_role('tagekyc_capture_runtime_authenticator',{role},'SET') AS "Value"
                    """).SingleAsync());
            }
            var candidateConnection = new NpgsqlConnectionStringBuilder(defect == "wrong-login" ? admin : brokerConnection);
            if (defect == "role-option") candidateConnection.Options = "-c role=tagekyc_raw_export_claim_broker";
            await using var candidateSource = NpgsqlDataSource.Create(candidateConnection.ConnectionString);
            await using var candidate = Tip88C1C6BA3SyntheticComposition.QualifiedBrokerServices(candidateSource, options,
                missingKey: defect == "missing-key", missingProvider: defect == "missing-provider",
                invalidProfile: defect == "invalid-profile");
            var transport = candidate.GetRequiredService<RawIngressBrokerTransport>();
            var bytes = RawIngressBrokerProtocol.WriteRequest(Tip88C1C6BA3BrokerHttpTests.Metadata());
            using var body = new MemoryStream(bytes);
            var reply = await transport.HandleAsync(new(IPAddress.Loopback, IPAddress.Loopback, options.BaseUri.Port,
                options.BaseUri.Authority, "POST", RawIngressBrokerOptions.AdmitPath, false, "application/json", bytes.Length,
                false, false), body, CancellationToken.None);
            Assert.Equal(503, reply.StatusCode); Assert.Empty(reply.Body);
            Assert.Equal(before, await Snapshot());
        }
        finally { await observer.Database.ExecuteSqlRawAsync(restore); }
        await broker.QualifyAsync(CancellationToken.None);
        Assert.Equal(before, await Snapshot());

        async Task<string[]> Snapshot() => await observer.Database.SqlQueryRaw<string>("""
            SELECT 'attempt:'||row_to_json(t)::text AS "Value" FROM tagekyc.raw_export_source_encryption_attempts t
            UNION ALL SELECT 'snapshot:'||row_to_json(t)::text FROM tagekyc.raw_export_authority_snapshots t
            UNION ALL SELECT 'alias:'||row_to_json(t)::text FROM tagekyc.raw_export_source_ingress_claim_aliases t
            UNION ALL SELECT 'claim:'||row_to_json(t)::text FROM tagekyc.raw_export_source_ingress_claims t
            UNION ALL SELECT 'reservation:'||row_to_json(t)::text FROM tagekyc.raw_export_source_reservations t
            UNION ALL SELECT 'head:'||row_to_json(t)::text FROM tagekyc.raw_export_source_head t
            ORDER BY "Value"
            """).ToArrayAsync();
    }
}

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88C1C6BA3BrokerTransactionTests(PostgresPersistenceFixture postgres)
{
    [Fact]
    public async Task P05_ProviderLossInsideBReturnsO05AfterRollbackAndExactRetryCanProceed()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_p05_mid_b");
        await Tip88C1C6BA3ConsentRetentionTests.Prepare(isolated);
        var scope = await Tip88C1C6BA3RetentionCheckpointTests.Seed(isolated, rawIngress: true);
        await using var observer = isolated.CreateDbContext();
        var before = await Rows();
        var login = await Tip88C1C6BA3SyntheticComposition.BrokerLogin(
            observer.Database.GetConnectionString()!);
        await using var source = NpgsqlDataSource.Create(login);
        await using var services = Tip88C1C6BA3SyntheticComposition.PreflightServices();
        var commitment = new FailPayloadCommitment(
            services.GetRequiredService<IContentCommitmentService>());
        var subject = services.GetRequiredService<ISubjectRefTokenService>();
        var options = RawIngressBrokerOptions.Read(Tip88C1C6BA3SyntheticComposition.BrokerConfiguration());
        var broker = new QualifiedRawIngressBroker(source, commitment, subject, options,
            Tip88C1C6BA3SyntheticComposition.Broker(source, commitment, subject));
        var now = await observer.Database.SqlQueryRaw<DateTimeOffset>(
            "SELECT clock_timestamp() AS \"Value\"").SingleAsync();
        var context = new CaptureRuntimeRawIngressAdmissionContext(
            Guid.Parse("40000000-0000-4000-8000-000000000001"),
            Guid.Parse("50000000-0000-4000-8000-000000000001"),
            Guid.Parse("60000000-0000-4000-8000-000000000001"), 1,
            Guid.Parse("10000000-0000-4000-8000-000000000001"), 1,
            now, new byte[32], new byte[32], 1, scope.Session, scope.Artifact, 1,
            "LiveSelfieImage", Guid.NewGuid(), "image/jpeg", 24,
            Convert.ToHexString(SHA256.HashData("synthetic-retainedsource"u8)).ToLowerInvariant(),
            now.AddSeconds(-5), now.AddSeconds(-4), now.AddMinutes(5), 300);

        var denied = Assert.IsType<RawIngressBrokerResult.Final>(
            await broker.AdmitAsync(context, CancellationToken.None));
        Assert.Equal(RawExportSourceIngressCodes.CapabilityUnavailable, denied.Value.OutcomeCode);
        Assert.True(commitment.PublicProbeCalls > 0);
        Assert.Equal(1, commitment.FailedPayloadCalls);
        Assert.Equal(before, await Rows()); // B's attempted shell/alias/R1 is rolled back

        commitment.Recover();
        Assert.IsType<RawIngressBrokerResult.Handoff>(
            await broker.AdmitAsync(context, CancellationToken.None));
        Assert.False(before.SequenceEqual(await Rows()));

        Task<string[]> Rows() => observer.Database.SqlQueryRaw<string>("""
            SELECT family||':'||value AS "Value" FROM (
              SELECT 'alias' family,to_jsonb(r)::text value FROM tagekyc.raw_export_source_ingress_claim_aliases r
              UNION ALL SELECT 'claim',to_jsonb(r)::text FROM tagekyc.raw_export_source_ingress_claims r
              UNION ALL SELECT 'reservation',to_jsonb(r)::text FROM tagekyc.raw_export_source_reservations r
              UNION ALL SELECT 'attempt',to_jsonb(r)::text FROM tagekyc.raw_export_source_encryption_attempts r
            ) rows ORDER BY family,value
            """).ToArrayAsync();
    }

    private sealed class FailPayloadCommitment(IContentCommitmentService inner) : IContentCommitmentService
    {
        private bool failPayload = true;
        internal int PublicProbeCalls;
        internal int FailedPayloadCalls;

        public async ValueTask<ContentCommitmentResult> ComputeAsync(CommitmentKeySelector selector,
            ReadOnlyMemory<byte> lpPayload, CancellationToken cancellationToken)
        {
            if (lpPayload.IsEmpty) PublicProbeCalls++;
            if (failPayload && !lpPayload.IsEmpty)
            {
                FailedPayloadCalls++;
                return ContentCommitmentResult.Failed(ContentCommitmentFailure.ProviderFailure);
            }
            return await inner.ComputeAsync(selector, lpPayload, cancellationToken);
        }

        internal void Recover() => failPayload = false;
    }

    [Theory]
    [InlineData("success")]
    [InlineData("preflight-failure")]
    [InlineData("commit-failure")]
    [InlineData("extended-bounds")]
    [InlineData("http-success")]
    [InlineData("http-commit-failure")]
    public async Task BrokerUsesOneTransactionAndDerivedActor(string scenario)
    {
        var useHttp = scenario.StartsWith("http-", StringComparison.Ordinal);
        if (useHttp) scenario = scenario[5..];
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_same_b");
        await Tip88C1C6BA3ConsentRetentionTests.Prepare(isolated);
        var scope = await Tip88C1C6BA3RetentionCheckpointTests.Seed(isolated, rawIngress: true);
        await using var observer = isolated.CreateDbContext();
        // The disposable database clones current fixture data; it is not an
        // empty database. Preserve every inherited row, including legacy snapshots.
        var inheritedRows = await DurableRows();
        // Test-only instrumentation records the top-level transaction, not xmin:
        // PL/pgSQL exception blocks may give rows different subtransaction xids.
        await observer.Database.ExecuteSqlRawAsync("""
            CREATE SCHEMA a3_same_b_proof;
            CREATE TABLE a3_same_b_proof.writes(family text NOT NULL, root_xid text NOT NULL, backend integer NOT NULL);
            CREATE FUNCTION a3_same_b_proof.observe_write() RETURNS trigger
            LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $proof$
            BEGIN
              INSERT INTO a3_same_b_proof.writes VALUES(TG_TABLE_NAME,pg_current_xact_id()::text,pg_backend_pid());
              RETURN NEW;
            END $proof$;
            DO $proof$ DECLARE target text; BEGIN
              FOREACH target IN ARRAY ARRAY['raw_export_authority_snapshots','raw_export_source_ingress_claim_aliases',
                'raw_export_source_ingress_claims','raw_export_source_reservations','raw_export_source_head',
                'raw_export_source_encryption_attempts'] LOOP
                EXECUTE format('CREATE TRIGGER a3_same_b_observe AFTER INSERT OR UPDATE ON tagekyc.%I FOR EACH ROW EXECUTE FUNCTION a3_same_b_proof.observe_write()',target);
              END LOOP;
            END $proof$;
            """);
        if (scenario == "commit-failure")
            await observer.Database.ExecuteSqlRawAsync("""
                CREATE FUNCTION a3_same_b_proof.reject_commit() RETURNS trigger LANGUAGE plpgsql AS $proof$
                BEGIN RAISE EXCEPTION 'a3_synthetic_commit_failure'; END $proof$;
                CREATE CONSTRAINT TRIGGER a3_reject_commit AFTER INSERT ON tagekyc.raw_export_source_encryption_attempts
                DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION a3_same_b_proof.reject_commit();
                """);
        string? observedRootXid = null;
        int observedBackend = 0;
        var application = "a3-broker-" + Guid.NewGuid().ToString("N");
        var connection = new NpgsqlConnectionStringBuilder(observer.Database.GetConnectionString())
        {
            ApplicationName = application, Options = "-c role=tagekyc_raw_export_claim_broker", Pooling = false,
        };
        await using var source = NpgsqlDataSource.Create(connection.ConnectionString);
        await using var services = Tip88C1C6BA3SyntheticComposition.PreflightServices();
        var probe = new Probe(services.GetRequiredService<IContentCommitmentService>(), async () =>
        {
            // The callback occurs between real B-B and B-C. Independent observer
            // sees one broker transaction holding B-R locks, but no uncommitted R1.
            Assert.Equal(1, await observer.Database.SqlQuery<int>($"""
                SELECT count(*)::integer AS "Value" FROM pg_catalog.pg_stat_activity
                WHERE application_name={application} AND backend_xid IS NOT NULL
                """).SingleAsync());
            Assert.True(await observer.Database.SqlQuery<bool>($"""
                SELECT EXISTS(SELECT 1 FROM pg_catalog.pg_locks l
                  JOIN pg_catalog.pg_stat_activity a ON a.pid=l.pid
                  WHERE a.application_name={application} AND l.locktype='advisory'
                  AND l.granted AND l.mode='ExclusiveLock') AS "Value"
                """).SingleAsync());
            // B-B does not acquire this A1 runtime-domain lock. A B-R commit
            // before B-B loses it, even if B-B takes its own exclusive locks.
            Assert.True(await observer.Database.SqlQuery<bool>($"""
                SELECT EXISTS(SELECT 1 FROM pg_catalog.pg_locks l
                  JOIN pg_catalog.pg_stat_activity a ON a.pid=l.pid
                  CROSS JOIN (SELECT hashtextextended('40000000-0000-4000-8000-000000000001',10) k) domain
                  WHERE a.application_name={application} AND l.locktype='advisory' AND l.granted
                    AND l.mode='ExclusiveLock' AND l.objsubid=1
                    AND l.classid=((domain.k>>32)&4294967295)::oid AND l.objid=(domain.k&4294967295)::oid) AS "Value"
                """).SingleAsync());
            observedRootXid = await observer.Database.SqlQuery<string>($"""
                SELECT backend_xid::text AS "Value" FROM pg_catalog.pg_stat_activity WHERE application_name={application}
                """).SingleAsync();
            observedBackend = await observer.Database.SqlQuery<int>($"""
                SELECT pid AS "Value" FROM pg_catalog.pg_stat_activity WHERE application_name={application}
                """).SingleAsync();
            Assert.Equal(inheritedRows, await DurableRows());
            if (scenario == "preflight-failure") throw new IOException("synthetic-preflight-failure");
        });
        var broker = Tip88C1C6BA3SyntheticComposition.Broker(source, probe,
            services.GetRequiredService<ISubjectRefTokenService>(), extendedBounds: scenario == "extended-bounds");
        await using var privateHost = useHttp ? await Tip88C1C6BA3BrokerHttpTests.NetworkHost.Start(broker) : null;
        using var privateClient = privateHost is null ? null : new RawIngressBrokerHttpClient(privateHost.Options);
        IRawIngressMetadataBroker admission = privateClient is null ? broker : privateClient;
        var now = await observer.Database.SqlQueryRaw<DateTimeOffset>("SELECT clock_timestamp() AS \"Value\"").SingleAsync();
        var context = new CaptureRuntimeRawIngressAdmissionContext(
            Guid.Parse("40000000-0000-4000-8000-000000000001"), Guid.Parse("50000000-0000-4000-8000-000000000001"),
            Guid.Parse("60000000-0000-4000-8000-000000000001"), 1,
            Guid.Parse("10000000-0000-4000-8000-000000000001"), 1, now, new byte[32], new byte[32],
            1, scope.Session, scope.Artifact, 1, "LiveSelfieImage", Guid.NewGuid(), "image/jpeg", 24,
            Convert.ToHexString(SHA256.HashData("synthetic-retainedsource"u8)).ToLowerInvariant(),
            now.AddSeconds(-5), now.AddSeconds(-4), now.AddMinutes(5), 300);
        if (scenario is "preflight-failure" or "commit-failure")
        {
            if (scenario == "preflight-failure")
                await Assert.ThrowsAsync<IOException>(() => admission.AdmitAsync(context, CancellationToken.None));
            else if (useHttp)
                Assert.Equal("RAW_INGRESS_PRIVATE_PROTOCOL_INVALID", (await Assert.ThrowsAsync<InvalidOperationException>(
                    () => admission.AdmitAsync(context, CancellationToken.None))).Message);
            else
                Assert.Equal("a3_synthetic_commit_failure", (await Assert.ThrowsAsync<PostgresException>(
                    () => admission.AdmitAsync(context, CancellationToken.None))).MessageText);
            Assert.Equal(inheritedRows, await DurableRows());
            Assert.Equal(0, await observer.Database.SqlQueryRaw<int>("SELECT count(*)::integer AS \"Value\" FROM a3_same_b_proof.writes").SingleAsync());
            Assert.Equal(1, probe.Calls);
            return;
        }
        var handoff = Assert.IsType<RawIngressBrokerResult.Handoff>(await admission.AdmitAsync(context, CancellationToken.None)).Value;
        Assert.Equal(Tip88C1C6BA3ConsentRetentionTests.Principal, handoff.CustodyActorPrincipalId);
        Assert.Equal(Tip88C1C6BA3ConsentRetentionTests.Client, handoff.ClientApplicationId);
        Assert.Equal(scope.RuntimeBinding, handoff.BindingId);
        Assert.Equal(scope.Permit, handoff.RetentionAuthorityId);
        var xids = await observer.Database.SqlQueryRaw<string>("""
            SELECT DISTINCT root_xid AS "Value" FROM a3_same_b_proof.writes
            """).ToArrayAsync();
        Assert.Equal(observedRootXid, Assert.Single(xids));
        Assert.Equal(observedBackend, await observer.Database.SqlQueryRaw<int>("SELECT DISTINCT backend AS \"Value\" FROM a3_same_b_proof.writes").SingleAsync());
        Assert.Equal(6, await observer.Database.SqlQueryRaw<int>("SELECT count(DISTINCT family)::integer AS \"Value\" FROM a3_same_b_proof.writes").SingleAsync());
        var committedRows = await DurableRows();
        Assert.Equal(inheritedRows.Length + 6, committedRows.Length);
        Assert.All(inheritedRows, row => Assert.Contains(row, committedRows));
        var persisted = await observer.RawExportSourceEncryptionAttempts.AsNoTracking()
            .Where(row => row.SourceArtifactId == handoff.SourceArtifactId).SingleAsync();
        Assert.Equal(persisted.AttemptId, handoff.AttemptId);
        Assert.Equal(persisted.AttemptKeyReservationId, handoff.AttemptKeyReservationId);
        Assert.Equal(persisted.SourceArtifactId, handoff.SourceArtifactId);
        Assert.Equal(persisted.EncryptionAttemptRevision, handoff.ExpectedEncryptionAttemptRevision);
        Assert.Equal(persisted.Fence, handoff.ExpectedFence);
        Assert.Equal(inheritedRows.Where(IsProviderRow), committedRows.Where(IsProviderRow));

        // Lost handoff response: discard it and retry identical admitted metadata.
        // ExistingMatch must not arm the old attempt or return another Handoff.
        var replayBroker = Tip88C1C6BA3SyntheticComposition.Broker(source,
            services.GetRequiredService<IContentCommitmentService>(), services.GetRequiredService<ISubjectRefTokenService>());
        var replay = await ProjectWithoutBody(replayBroker, context,
            CaptureRuntimeRawIngressOutcome.ReservationBusy, RawExportSourceIngressCodes.ReservationBusy, 409);
        Assert.Equal("RAW_EXPORT_SOURCE_RESERVATION_BUSY", replay.OutcomeCode);
        Assert.Equal(new CaptureAgentFinalResult("RAW_EXPORT_SOURCE_RESERVATION_BUSY"), replay);
        Assert.Equal(persisted.AttemptId, (await observer.RawExportSourceEncryptionAttempts.AsNoTracking()
            .Where(row => row.SourceArtifactId == handoff.SourceArtifactId).SingleAsync()).AttemptId);
        Assert.Equal(1, probe.Calls);

        var unavailable = new UnavailableCommitment();
        var unavailableBroker = Tip88C1C6BA3SyntheticComposition.Broker(source, unavailable,
            services.GetRequiredService<ISubjectRefTokenService>(), configuredCommitmentVersion: 2);
        var historic = await ProjectWithoutBody(unavailableBroker,
            context with { IngressIdempotencyKey = Guid.NewGuid() },
            CaptureRuntimeRawIngressOutcome.HistoricCommitmentKeyUnavailable,
            RawExportSourceIngressCodes.HistoricCommitmentKeyUnavailable, 503);
        Assert.Equal("RAW_EXPORT_SOURCE_HISTORIC_COMMITMENT_KEY_UNAVAILABLE", historic.OutcomeCode);
        Assert.Equal(new CaptureAgentFinalResult("RAW_EXPORT_SOURCE_HISTORIC_COMMITMENT_KEY_UNAVAILABLE"), historic);
        Assert.Equal(new CommitmentKeySelector("fixture-content-commitment", 1), unavailable.Selector);

        var conflictContext = context with { IngressIdempotencyKey = Guid.NewGuid(), ClaimedPlaintextDigest = new string('b', 64) };
        var boundAliasBeforeConflict = await AliasRow(context.IngressIdempotencyKey);
        var beforeConflict = await DurableRows();
        var aliasesBeforeConflict = await observer.RawExportSourceIngressClaimAliases.AsNoTracking().CountAsync();
        var conflict = await ProjectWithoutBody(replayBroker, conflictContext,
            CaptureRuntimeRawIngressOutcome.FingerprintConflict, RawExportSourceIngressCodes.FingerprintConflict, 409);
        Assert.Equal("RAW_EXPORT_SOURCE_FINGERPRINT_CONFLICT", conflict.OutcomeCode);
        Assert.Equal(new CaptureAgentFinalResult("RAW_EXPORT_SOURCE_FINGERPRINT_CONFLICT"), conflict);
        var tombstoneAlias = await observer.RawExportSourceIngressClaimAliases.AsNoTracking()
            .Where(row => row.IngressIdempotencyKey == conflictContext.IngressIdempotencyKey)
            .Select(row => new { row.AliasState, row.CurrentClaimEvaluationDisposition,
                row.IngressClaimId, row.ClientApplicationId })
            .SingleAsync();
        Assert.Equal("ConflictTombstone", tombstoneAlias.AliasState);
        Assert.Equal("Conflict", tombstoneAlias.CurrentClaimEvaluationDisposition);
        Assert.Equal(handoff.ClientApplicationId, tombstoneAlias.ClientApplicationId);
        Assert.NotNull(tombstoneAlias.IngressClaimId);
        Assert.Equal(aliasesBeforeConflict + 1,
            await observer.RawExportSourceIngressClaimAliases.AsNoTracking().CountAsync());
        Assert.Equal(boundAliasBeforeConflict, await AliasRow(context.IngressIdempotencyKey));
        Assert.Equal(beforeConflict.Where(row => !row.StartsWith("alias:", StringComparison.Ordinal)),
            (await DurableRows()).Where(row => !row.StartsWith("alias:", StringComparison.Ordinal)));
        Assert.Equal(persisted.AttemptId, (await observer.RawExportSourceEncryptionAttempts.AsNoTracking()
            .Where(row => row.SourceArtifactId == handoff.SourceArtifactId).SingleAsync()).AttemptId);
        Assert.Equal(inheritedRows.Where(IsProviderRow), (await DurableRows()).Where(IsProviderRow));

        var beforeDenials = await DurableRows();
        var sameKey = await ProjectWithoutBody(replayBroker, context with { MediaType = "image/png" },
            CaptureRuntimeRawIngressOutcome.ClaimTokenInvalid, RawExportSourceIngressCodes.ClaimTokenInvalid, 403);
        Assert.Equal(new CaptureAgentFinalResult("RAW_EXPORT_SOURCE_CLAIM_TOKEN_INVALID"), sameKey);
        Assert.Equal(beforeDenials, await DurableRows());
        var tombstone = await ProjectWithoutBody(replayBroker, conflictContext,
            CaptureRuntimeRawIngressOutcome.FingerprintConflict, RawExportSourceIngressCodes.FingerprintConflict, 409);
        Assert.Equal(new CaptureAgentFinalResult("RAW_EXPORT_SOURCE_FINGERPRINT_CONFLICT"), tombstone);
        Assert.Equal(beforeDenials, await DurableRows());
        await using (var lockOwner = new NpgsqlConnection(observer.Database.GetConnectionString()))
        {
            await lockOwner.OpenAsync();
            await using var lockTransaction = await lockOwner.BeginTransactionAsync();
            await using var acquire = new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtextextended($1,0))", lockOwner, lockTransaction);
            acquire.Parameters.AddWithValue("tip88c1b1:alias:" + handoff.ClientApplicationId.ToString("D") + ":" +
                context.CaptureAgentId.ToString("N") + ":" + context.DeviceInstallationId.ToString("N") + ":" + context.IngressIdempotencyKey.ToString("D"));
            await acquire.ExecuteNonQueryAsync();
            var busy = Assert.IsType<RawIngressBrokerResult.Final>(await replayBroker.AdmitAsync(context, CancellationToken.None)).Value;
            Assert.Equal(new CaptureAgentFinalResult("RAW_EXPORT_SOURCE_IDEMPOTENCY_BUSY"), busy);
            Assert.Equal(beforeDenials, await DurableRows());
            // Drive the same real locked broker through the public facade. O09
            // must be decided from metadata before either plaintext or R2 starts.
            var bodyPipeline = new RejectBodyPipeline();
            var facade = new CaptureRuntimeRawIngressAdmissionService(
                new RawExportIngressCapacity(1, 1, 24, 24), replayBroker, bodyPipeline, 24);
            using var body = new MemoryStream(new byte[24], writable: false);
            var projectedBusy = await facade.AdmitAsync(context, body, CancellationToken.None);
            Assert.Equal(CaptureRuntimeRawIngressOutcome.IdempotencyBusy, projectedBusy.Outcome);
            Assert.Null(projectedBusy.SourceArtifactId);
            Assert.Null(projectedBusy.CurrentSourceState);
            Assert.Null(projectedBusy.CurrentDisposition);
            Assert.Null(projectedBusy.RetryNotBeforeUtc);
            Assert.Equal(0, body.Position);
            Assert.Equal(0, bodyPipeline.Calls);
            Assert.Equal(beforeDenials, await DurableRows());

            var mapper = typeof(RawExportSourceIngressEndpoints).GetMethod("MapRuntimeResult",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(mapper);
            using var responseServices = new ServiceCollection().AddOptions().AddLogging().BuildServiceProvider();
            var responseContext = new DefaultHttpContext { RequestServices = responseServices };
            using var responseBody = new MemoryStream();
            responseContext.Response.Body = responseBody;
            var response = Assert.IsAssignableFrom<IResult>(mapper.Invoke(null, [responseContext, projectedBusy]));
            await response.ExecuteAsync(responseContext);
            Assert.Equal(409, responseContext.Response.StatusCode);
            responseBody.Position = 0;
            using (var json = await JsonDocument.ParseAsync(responseBody))
            {
                Assert.Equal(new[] { "outcomeCode" },
                    json.RootElement.EnumerateObject().Select(property => property.Name).ToArray());
                Assert.Equal(RawExportSourceIngressCodes.IdempotencyBusy,
                    json.RootElement.GetProperty("outcomeCode").GetString());
            }
            await lockTransaction.RollbackAsync();
        }

        async Task<string[]> DurableRows() => await observer.Database.SqlQueryRaw<string>("""
            SELECT family||':'||value AS "Value" FROM (
              SELECT 'snapshot' family,to_jsonb(r)::text value FROM tagekyc.raw_export_authority_snapshots r
              UNION ALL SELECT 'alias',to_jsonb(r)::text FROM tagekyc.raw_export_source_ingress_claim_aliases r
              UNION ALL SELECT 'claim',to_jsonb(r)::text FROM tagekyc.raw_export_source_ingress_claims r
              UNION ALL SELECT 'reservation',to_jsonb(r)::text FROM tagekyc.raw_export_source_reservations r
              UNION ALL SELECT 'head',to_jsonb(r)::text FROM tagekyc.raw_export_source_head r
              UNION ALL SELECT 'attempt',to_jsonb(r)::text FROM tagekyc.raw_export_source_encryption_attempts r
              UNION ALL SELECT 'provider-key',to_jsonb(r)::text FROM tagekyc.raw_export_attempt_key_reservations r
              UNION ALL SELECT 'provider-object',to_jsonb(r)::text FROM tagekyc.raw_export_provisional_objects r
            ) rows ORDER BY family,value
            """).ToArrayAsync();

        Task<string> AliasRow(Guid key) => observer.Database.SqlQuery<string>($"""
            SELECT to_jsonb(r)::text AS "Value" FROM tagekyc.raw_export_source_ingress_claim_aliases r
            WHERE r."IngressIdempotencyKey"={key}
            """).SingleAsync();

        async Task<CaptureAgentFinalResult> ProjectWithoutBody(IRawIngressMetadataBroker actualBroker,
            CaptureRuntimeRawIngressAdmissionContext request, CaptureRuntimeRawIngressOutcome expectedOutcome,
            string expectedCode, int expectedStatus)
        {
            var observed = new ObservedBroker(actualBroker);
            var bodyPipeline = new RejectBodyPipeline();
            var facade = new CaptureRuntimeRawIngressAdmissionService(
                new RawExportIngressCapacity(1, 1, 24, 24), observed, bodyPipeline, 24);
            using var body = new MemoryStream(new byte[24], writable: false);
            var projected = await facade.AdmitAsync(request, body, CancellationToken.None);
            Assert.Equal(1, observed.Calls);
            var final = Assert.IsType<RawIngressBrokerResult.Final>(observed.Last).Value;
            Assert.Equal(new CaptureAgentFinalResult(expectedCode), final);
            Assert.Equal(expectedOutcome, projected.Outcome);
            Assert.Null(projected.SourceArtifactId);
            Assert.Null(projected.CurrentSourceState);
            Assert.Null(projected.CurrentDisposition);
            Assert.Null(projected.RetryNotBeforeUtc);
            Assert.Equal(0, body.Position);
            Assert.Equal(0, bodyPipeline.Calls);

            var mapper = typeof(RawExportSourceIngressEndpoints).GetMethod("MapRuntimeResult",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(mapper);
            using var responseServices = new ServiceCollection().AddOptions().AddLogging().BuildServiceProvider();
            var responseContext = new DefaultHttpContext { RequestServices = responseServices };
            using var responseBody = new MemoryStream();
            responseContext.Response.Body = responseBody;
            var response = Assert.IsAssignableFrom<IResult>(mapper.Invoke(null, [responseContext, projected]));
            await response.ExecuteAsync(responseContext);
            Assert.Equal(expectedStatus, responseContext.Response.StatusCode);
            responseBody.Position = 0;
            using var json = await JsonDocument.ParseAsync(responseBody);
            Assert.Equal(new[] { "outcomeCode" },
                json.RootElement.EnumerateObject().Select(property => property.Name).ToArray());
            Assert.Equal(expectedCode, json.RootElement.GetProperty("outcomeCode").GetString());
            return final;
        }

        static bool IsProviderRow(string row) => row.StartsWith("provider-", StringComparison.Ordinal);
    }

    [Fact]
    public async Task BrokerCommitResponseLossDoesNotCreateAnotherHandoff()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_private_loss");
        await Tip88C1C6BA3ConsentRetentionTests.Prepare(isolated);
        var scope = await Tip88C1C6BA3RetentionCheckpointTests.Seed(isolated, rawIngress: true);
        await using var observer = isolated.CreateDbContext();
        var before = await observer.RawExportSourceEncryptionAttempts.AsNoTracking().CountAsync();
        var connection = new NpgsqlConnectionStringBuilder(observer.Database.GetConnectionString())
            { Options = "-c role=tagekyc_raw_export_claim_broker", Pooling = false };
        await using var source = NpgsqlDataSource.Create(connection.ConnectionString);
        await using var providers = Tip88C1C6BA3SyntheticComposition.PreflightServices();
        var observed = new ObservedBroker(Tip88C1C6BA3SyntheticComposition.Broker(source,
            providers.GetRequiredService<IContentCommitmentService>(), providers.GetRequiredService<ISubjectRefTokenService>()));
        await using var host = await Tip88C1C6BA3BrokerHttpTests.NetworkHost.Start(observed, loseFirstResponse: true);
        using var client = new RawIngressBrokerHttpClient(host.Options);
        var now = await observer.Database.SqlQueryRaw<DateTimeOffset>("SELECT clock_timestamp() AS \"Value\"").SingleAsync();
        var context = new CaptureRuntimeRawIngressAdmissionContext(
            Guid.Parse("40000000-0000-4000-8000-000000000001"), Guid.Parse("50000000-0000-4000-8000-000000000001"),
            Guid.Parse("60000000-0000-4000-8000-000000000001"), 1, Guid.Parse("10000000-0000-4000-8000-000000000001"), 1,
            now, new byte[32], new byte[32], 1, scope.Session, scope.Artifact, 1, "LiveSelfieImage", Guid.NewGuid(),
            "image/jpeg", 24, new string('a',64), now.AddSeconds(-5), now.AddSeconds(-4), now.AddMinutes(5), 300);
        await Assert.ThrowsAsync<HttpRequestException>(() => client.AdmitAsync(context, CancellationToken.None));
        Assert.Equal(1, observed.Calls); // No automatic retry after a lost committed reply.
        var handoff = Assert.IsType<RawIngressBrokerResult.Handoff>(observed.Last).Value;
        var persisted = await observer.RawExportSourceEncryptionAttempts.AsNoTracking()
            .Where(x => x.SourceArtifactId == handoff.SourceArtifactId).SingleAsync();
        var originalAttempt = await AttemptRow();
        Assert.Equal(handoff.AttemptId, persisted.AttemptId);
        Assert.Equal(before + 1, await observer.RawExportSourceEncryptionAttempts.AsNoTracking().CountAsync());
        var reply = Assert.IsType<RawIngressBrokerResult.Final>(await client.AdmitAsync(context, CancellationToken.None));
        Assert.Equal("RAW_EXPORT_SOURCE_RESERVATION_BUSY", reply.Value.OutcomeCode);
        Assert.Equal(2, observed.Calls);
        Assert.Equal(originalAttempt, await AttemptRow());
        Assert.Equal(before + 1, await observer.RawExportSourceEncryptionAttempts.AsNoTracking().CountAsync());

        Task<string> AttemptRow() => observer.Database.SqlQuery<string>($"""
            SELECT to_jsonb(a)::text AS "Value" FROM tagekyc.raw_export_source_encryption_attempts a
            WHERE a."AttemptId"={handoff.AttemptId}
            """).SingleAsync();
    }

    private sealed class ObservedBroker(IRawIngressMetadataBroker inner) : IRawIngressMetadataBroker
    {
        internal int Calls; internal RawIngressBrokerResult? Last;
        public async Task<RawIngressBrokerResult> AdmitAsync(CaptureRuntimeRawIngressAdmissionContext context, CancellationToken ct)
        { Calls++; return Last = await inner.AdmitAsync(context, ct); }
    }

    private sealed class RejectBodyPipeline : ICaptureRuntimeRawIngressBodyPipeline
    {
        internal int Calls;
        public Task<CaptureRuntimeRawIngressAdmissionResult> ProcessAsync(
            CaptureRuntimeRawIngressAdmissionContext context, RawIngressBrokerHandoff handoff,
            Stream body, CancellationToken cancellationToken)
        {
            Calls++;
            throw new InvalidOperationException("O09_MUST_NOT_START_BODY_PIPELINE");
        }
    }

    private sealed class Probe(IContentCommitmentService inner, Func<Task> inspect) : IContentCommitmentService
    {
        internal int Calls { get; private set; }
        public async ValueTask<ContentCommitmentResult> ComputeAsync(CommitmentKeySelector selector,
            ReadOnlyMemory<byte> lpPayload, CancellationToken cancellationToken)
        {
            Calls++;
            await inspect();
            return await inner.ComputeAsync(selector, lpPayload, cancellationToken);
        }
    }

    private sealed class UnavailableCommitment : IContentCommitmentService
    {
        internal CommitmentKeySelector? Selector { get; private set; }
        public ValueTask<ContentCommitmentResult> ComputeAsync(CommitmentKeySelector selector,
            ReadOnlyMemory<byte> lpPayload, CancellationToken cancellationToken)
        {
            Selector = selector;
            return ValueTask.FromResult(ContentCommitmentResult.Failed(ContentCommitmentFailure.ProviderFailure));
        }
    }
}

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88C1C6BA3ContinuationWorkerTests(PostgresPersistenceFixture postgres, Xunit.Abstractions.ITestOutputHelper output)
{
    [Theory]
    [InlineData(4, "SourceRetentionNotAuthorized")]
    [InlineData(4, "StateConflict")]
    [InlineData(5, "SourceRetentionNotAuthorized")]
    [InlineData(5, "StateConflict")]
    public async Task PublicationFailurePayloadMustBeEmpty(int stage, string outcome)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_worker_failure_shape");
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var source = await Tip88C1C6BA3RetentionCheckpointTests.CreateVerifiedContinuationSource(isolated, minio);
        await using var observer = isolated.CreateDbContext();
        await using var logins = await Tip88C1C6BA3SyntheticComposition.CustodyLogins(observer.Database.GetConnectionString()!);
        await using var scopes = Owners(logins.Connections, minio);
        var pipeline = new CaptureRuntimeSourcePipeline(scopes);
        Assert.Equal(RetainedPublicationStep.Stage, (await pipeline.AdvancePublicationAsync(source, default)).Step);
        if (stage == 5) Assert.Equal(RetainedPublicationStep.Commit, (await pipeline.AdvancePublicationAsync(source, default)).Step);
        await using var admin = new NpgsqlConnection(observer.Database.GetConnectionString());
        await admin.OpenAsync();
        await using var read = new NpgsqlCommand("SELECT pg_get_functiondef(to_regprocedure($1))", admin);
        read.Parameters.AddWithValue(stage == 4
            ? "tagekyc.raw_export_commit_staged_source(uuid,bigint,bigint,bigint,bigint)"
            : "tagekyc.raw_export_publish_available_source(uuid,bigint,bigint)");
        var original = (string)(await read.ExecuteScalarAsync())!;
        var body = original.IndexOf("AS $function$", StringComparison.Ordinal);
        Assert.True(body > 0);
        string[] types = stage == 4
            ? ["uuid", "uuid", "uuid", "uuid", "bytea", "timestamptz", "bigint", "bigint"]
            : ["uuid", "uuid", "uuid", "bytea", "bigint", "bigint", "timestamptz", "text"];
        try
        {
            // Same real function/role/caller, all-NULL failure positive control.
            // Then vary only one forbidden field at a time; no expected tuple is
            // constructed by the product or asserted only in a test-local mapper.
            await Install(-1);
            var control = await pipeline.AdvancePublicationAsync(source, default);
            Assert.Equal(outcome == "SourceRetentionNotAuthorized", control.RetentionDenied);
            Assert.Equal("Staged", control.Snapshot!.CustodyState);
            for (var field = 0; field < types.Length; field++)
            {
                await Install(field);
                var error = await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.AdvancePublicationAsync(source, default));
                Assert.Equal("RAW_INGRESS_CONTINUATION_NOT_READY", error.Message);
            }
        }
        finally
        {
            await using var restore = new NpgsqlCommand(original, admin);
            await restore.ExecuteNonQueryAsync();
        }
        // Real SQL restored; same source can proceed without reseeding/relaxing.
        Assert.Equal(stage == 4 ? RetainedPublicationStep.Commit : RetainedPublicationStep.Publish,
            (await pipeline.AdvancePublicationAsync(source, default)).Step);

        async Task Install(int field)
        {
            var values = types.Select((type, i) => i != field ? "NULL::" + type : type switch
            {
                "uuid" => "'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'::uuid",
                "bytea" => "decode(repeat('aa',32),'hex')", "timestamptz" => "clock_timestamp()",
                "bigint" => "1::bigint", _ => "'Pending'::text"
            });
            var replacement = original[..body] + "AS $function$ BEGIN RETURN QUERY SELECT '" + outcome
                + "'::text," + string.Join(',', values) + "; END $function$;";
            await using var mutate = new NpgsqlCommand(replacement, admin);
            await mutate.ExecuteNonQueryAsync();
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task ProcessKill_RestartUsesDurablePublicationState(int cut)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_process_publication");
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var source = await Tip88C1C6BA3RetentionCheckpointTests.CreateVerifiedContinuationSource(isolated, minio);
        await using var observer = isolated.CreateDbContext();
        await using var logins = await Tip88C1C6BA3SyntheticComposition.CustodyLogins(observer.Database.GetConnectionString()!);
        var configs = new[] { ProvisionalObjectCapability.Writer, ProvisionalObjectCapability.Reconciler,
            ProvisionalObjectCapability.Lifecycle }.Select(x => Tip88C1C6BA3SyntheticComposition.ObjectConfiguration(minio.Options(x))
                .AsEnumerable().Where(x => x.Value is not null).ToDictionary(x => x.Key, x => x.Value)).ToArray();
        var input = new ContinuationChildInput(logins.Connections, configs, source, cut, false);
        var history = await History(observer);
        await using var probe = await ContinuationProbe.Compile();
        using var child = probe.Start(input);
        var stderr = child.StandardError.ReadToEndAsync();
        try
        {
            var barrier = await child.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(40));
            Assert.Equal($"A3-CUT:{child.Id}:{cut}", barrier);
            Assert.NotEqual(Environment.ProcessId, child.Id);
            Assert.False(child.HasExited);
            await using var readSource = NpgsqlDataSource.Create(observer.Database.GetConnectionString()!);
            var reader = new RawSourceRetentionContinuationRepository(readSource);
            var beforeKill = await reader.ReadAsync(source, default);
            Assert.Equal(cut == 0 ? "Reserved" : cut == 3 ? "Available" : "Staged", beforeKill!.CustodyState);
            Assert.Equal(cut < 2 ? null : cut == 2 ? "Committed" : "Available", beforeKill.PublicationState);
            var killedPid = child.Id;
            child.Kill(entireProcessTree: true);
            await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
            Assert.True(child.HasExited);
            Assert.NotEqual(0, child.ExitCode);
            Assert.Equal(beforeKill, await reader.ReadAsync(source, default));
            Assert.Equal(history, await History(observer));

            using var restarted = probe.Start(input with { Recover = true });
            var restartedError = restarted.StandardError.ReadToEndAsync();
            try
            {
                Assert.NotEqual(killedPid, restarted.Id);
                var recovered = await restarted.StandardOutput.ReadToEndAsync().WaitAsync(TimeSpan.FromSeconds(40));
                await restarted.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
                Assert.Equal(0, restarted.ExitCode);
                Assert.Equal($"A3-AVAILABLE:{restarted.Id}", recovered.Trim());
                Assert.Equal("", await restartedError);
                var after = await reader.ReadAsync(source, default);
                Assert.Equal("Available", after!.CustodyState);
                Assert.Equal("Available", after.PublicationState);
                Assert.Equal(beforeKill.AttemptId, after.AttemptId);
                Assert.Equal(beforeKill.AttemptKeyReservationId, after.AttemptKeyReservationId);
                Assert.Equal(beforeKill.ObjectCustodyId, after.ObjectCustodyId);
                if (cut >= 2) Assert.Equal(beforeKill.SourcePublicationId, after.SourcePublicationId);
                Assert.Equal(history, await History(observer));
                Assert.Equal(1, await observer.RawExportSourcePublications
                    .CountAsync(x => x.SourceArtifactId == source));
                output.WriteLine($"REAL_PROCESS_KILL cut={cut} killed={killedPid} restarted={restarted.Id}; observer Available; key/object bytes unchanged.");
            }
            finally
            {
                if (!restarted.HasExited) { restarted.Kill(true); await restarted.WaitForExitAsync(); }
            }
        }
        finally
        {
            if (!child.HasExited) { child.Kill(true); await child.WaitForExitAsync(); }
        }
        Assert.Equal("", await stderr);
    }

    public sealed record ContinuationChildInput(string[] Connections,
        Dictionary<string, string?>[] Configurations, Guid Source, int Cut, bool Recover,
        bool Terminal = false, string TerminalOutcomeCode = "RECAPTURE_REQUIRED",
        string? KeyJournalConnection = null, bool FullR2 = false, bool Cleanup = false);

    // Called by the generated SDK probe, not a separately passing/disabled Fact.
    // The input is synthetic connection/configuration metadata, never plaintext,
    // a receipt, or a checkpoint result. Every state decision is production CP08.
    internal static async Task RunContinuationChildAsync()
    {
        var input = JsonSerializer.Deserialize<ContinuationChildInput>(
            Environment.GetEnvironmentVariable("TAGEKYC_A3_CONTINUATION_PROBE")!)!;
        var config = input.Configurations.Select(x => new ConfigurationBuilder().AddInMemoryCollection(x).Build()).ToArray();
        await using var scopes = new CaptureRuntimeCustodyProviderScopes(input.Connections[0], config[0],
            input.Connections[1], config[1], input.Connections[2], config[2]);
        await using var journalWrite = input.KeyJournalConnection is null
            ? null : Context(input.KeyJournalConnection);
        await using var journalRead = input.KeyJournalConnection is null
            ? null : Context(input.KeyJournalConnection);
        IKekProvisioningRecoveryOperation? keyRecovery = journalWrite is null || journalRead is null
            ? null
            : new FixtureDurableKekOperationProvider(
                new PostgresFixtureKekJournal(journalWrite), new PostgresFixtureKekJournal(journalRead));
        await using var verificationServices = keyRecovery is null
            ? null : Tip88C1C6BA3SyntheticComposition.PreflightServices();
        var pipeline = keyRecovery is null
            ? new CaptureRuntimeSourcePipeline(scopes)
            : new CaptureRuntimeSourcePipeline(scopes, keyRecovery, null,
                (IKekOperationProvider)keyRecovery,
                verificationServices!.GetRequiredService<TagEkyc.Contracts.RawExport.IContentCommitmentService>(),
                DurableKeyCustodyOptions.Resolve(new ConfigurationManager()));
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        if (!input.Recover)
        {
            for (var i = 0; i < input.Cut; i++)
            {
                if (input.Terminal || input.FullR2 || input.Cleanup) await pipeline.AdvanceAsync(input.Source, 1000, stop.Token);
                else await pipeline.AdvancePublicationAsync(input.Source, stop.Token);
            }
            Console.WriteLine($"A3-CUT:{Environment.ProcessId}:{input.Cut}");
            Console.Out.Flush();
            await Task.Delay(Timeout.Infinite, stop.Token); // Parent must Process.Kill this live child.
            throw new InvalidOperationException("CHILD_MUST_BE_KILLED");
        }
        var worker = new CaptureRuntimeSourceContinuationWorker(pipeline,
            RawIngressBrokerOptions.Read(Tip88C1C6BA3SyntheticComposition.BrokerConfiguration()),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<CaptureRuntimeSourceContinuationWorker>.Instance);
        var run = worker.RunAsync(stop.Token);
        try
        {
            await using var role = await scopes.OpenReconcilerAsync(stop.Token);
            var reader = new RawSourceRetentionContinuationRepository(role.Services.GetRequiredService<NpgsqlDataSource>());
            while (true)
            {
                var state = await reader.ReadAsync(input.Source, stop.Token);
                if (input.Terminal ? state?.R2TerminalOutcomeCode == input.TerminalOutcomeCode
                    : input.Cleanup ? state?.CleanupDisposition == "Completed"
                    : state?.CustodyState == "Available") break;
                await Task.Delay(25, stop.Token);
            }
        }
        finally
        {
            stop.Cancel();
            try { await run; } catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
        }
        Console.WriteLine($"{(input.Terminal ? "A3-TERMINAL" : input.Cleanup ? "A3-CLEANUP" : "A3-AVAILABLE")}:{Environment.ProcessId}");
    }

    private static TagEkycDbContext Context(string connection) =>
        new(new DbContextOptionsBuilder<TagEkycDbContext>().UseNpgsql(connection).Options);

    internal sealed class ContinuationProbe(string directory, string dotnet, string assembly) : IAsyncDisposable
    {
        internal static async Task<ContinuationProbe> Compile()
        {
            var runtime = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
            var dotnet = Path.GetFullPath(Path.Combine(runtime, "..", "..", "..", "dotnet.exe"));
            Assert.True(File.Exists(dotnet));
            var directory = Directory.CreateTempSubdirectory("tagekyc-a3-continuation-").FullName;
            var owner = new ContinuationProbe(directory, dotnet, typeof(Tip88C1C6BA3ContinuationWorkerTests).Assembly.Location);
            try
            {
                var sdkOutput = await owner.Command(["--list-sdks"]);
                var sdk = sdkOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim())
                    .Last(x => x.Contains('[') && x.EndsWith(']'));
                var compiler = Path.Combine(sdk[(sdk.IndexOf('[') + 1)..^1], sdk[..sdk.IndexOf(' ')], "Roslyn", "bincore", "csc.dll");
                const string code = """
                    using System;
                    using System.IO;
                    using System.Reflection;
                    using System.Runtime.Loader;
                    using System.Threading.Tasks;
                    public static class Probe {
                      public static async Task<int> Main(string[] args) {
                        try {
                          var dir = Path.GetDirectoryName(args[0]);
                          AssemblyLoadContext.Default.Resolving += (_, name) => {
                            var path = Path.Combine(dir, name.Name + ".dll");
                            return File.Exists(path) ? AssemblyLoadContext.Default.LoadFromAssemblyPath(path) : null;
                          };
                          var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(args[0]);
                          var type = assembly.GetType("TagEkyc.IntegrationTests.Tip88C1C6BA3ContinuationWorkerTests", true);
                          await (Task)type.GetMethod("RunContinuationChildAsync", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
                          return 0;
                        } catch (Exception e) { Console.Error.WriteLine(e.GetType().Name); return 91; }
                      }
                    }
                    """;
                var source = Path.Combine(directory, "Probe.cs");
                await File.WriteAllTextAsync(source, code, new UTF8Encoding(false));
                var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
                    .Where(x => string.Equals(Path.GetDirectoryName(x)?.TrimEnd(Path.DirectorySeparatorChar),
                        runtime.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase));
                var response = Path.Combine(directory, "compile.rsp");
                await File.WriteAllLinesAsync(response, new[] { "/nologo", "/target:exe", "/langversion:12",
                        "/out:\"" + Path.Combine(directory, "Probe.dll") + "\"" }
                    .Concat(references.Select(x => "/reference:\"" + x + "\""))
                    .Append("\"" + source + "\""), new UTF8Encoding(false));
                await owner.Command([compiler, "@" + response]);
                return owner;
            }
            catch { await owner.DisposeAsync(); throw; }
        }

        internal System.Diagnostics.Process Start(ContinuationChildInput input)
        {
            var path = Path.GetDirectoryName(assembly)!;
            var start = Info(["exec", "--runtimeconfig", Path.Combine(path, "TagEkyc.IntegrationTests.runtimeconfig.json"),
                "--depsfile", Path.Combine(path, "TagEkyc.IntegrationTests.deps.json"), Path.Combine(directory, "Probe.dll"), assembly]);
            start.Environment["TAGEKYC_A3_CONTINUATION_PROBE"] = JsonSerializer.Serialize(input);
            return System.Diagnostics.Process.Start(start)!;
        }
        private System.Diagnostics.ProcessStartInfo Info(string[] arguments)
        {
            var start = new System.Diagnostics.ProcessStartInfo(dotnet)
                { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (var argument in arguments) start.ArgumentList.Add(argument);
            return start;
        }
        private async Task<string> Command(string[] arguments)
        {
            using var child = System.Diagnostics.Process.Start(Info(arguments))!;
            var stdout = child.StandardOutput.ReadToEndAsync(); var stderr = child.StandardError.ReadToEndAsync();
            try { await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(30)); }
            finally { if (!child.HasExited) { child.Kill(true); await child.WaitForExitAsync(); } }
            Assert.True(child.ExitCode == 0, (await stdout) + (await stderr));
            return await stdout;
        }
        public ValueTask DisposeAsync() { Directory.Delete(directory, recursive: true); return ValueTask.CompletedTask; }
    }

    [Fact]
    public async Task StageAndCommitUseObservedCasTupleWithoutReencrypting()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_worker_stages");
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var source = await Tip88C1C6BA3RetentionCheckpointTests.CreateVerifiedContinuationSource(isolated, minio);
        await using var observer = isolated.CreateDbContext();
        await using var logins = await Tip88C1C6BA3SyntheticComposition.CustodyLogins(observer.Database.GetConnectionString()!);
        await using var scopes = Owners(logins.Connections, minio);
        var pipeline = new CaptureRuntimeSourcePipeline(scopes);
        var before = await History(observer);
        var staged = await pipeline.AdvancePublicationAsync(source, default);
        Assert.Equal(RetainedPublicationStep.Stage, staged.Step);
        Assert.Equal("Staged", staged.Snapshot!.CustodyState);
        Assert.Null(staged.Snapshot.SourcePublicationId);
        Assert.Equal(2, staged.Snapshot.ReservationRevision);
        var committed = await pipeline.AdvancePublicationAsync(source, default);
        Assert.Equal(RetainedPublicationStep.Commit, committed.Step);
        Assert.Equal("Committed", committed.Snapshot!.PublicationState);
        Assert.Equal("Staged", committed.Snapshot.CustodyState);
        Assert.NotNull(committed.Snapshot.SourcePublicationId);
        var published = await pipeline.AdvancePublicationAsync(source, default);
        Assert.Equal(RetainedPublicationStep.Publish, published.Step);
        Assert.Equal("Available", published.Snapshot!.PublicationState);
        Assert.Equal("Available", published.Snapshot.CustodyState);
        Assert.Equal(committed.Snapshot.SourcePublicationId, published.Snapshot.SourcePublicationId);
        Assert.Equal(before, await History(observer));
        var replay = await pipeline.AdvancePublicationAsync(source, default);
        Assert.Equal(RetainedPublicationStep.None, replay.Step);
        Assert.Equal(published.Snapshot, replay.Snapshot);
        Assert.DoesNotContain(source, await pipeline.ScanAsync(null, default));
    }

    [Theory]
    [InlineData("intent")]
    [InlineData("operational")]
    [InlineData("final")]
    public void PublicationSelectionCannotOvertakeTerminalState(string terminal)
    {
        var row = new RawSourceRetentionContinuation(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, "Reserved", 1, 1,
            Guid.NewGuid(), 1, Guid.NewGuid(), Guid.NewGuid(), "VerifiedCompleted", 3,
            null, null, null, null, null, null, null, null, null, null);
        Assert.Equal(RetainedPublicationStep.Stage, CaptureRuntimeSourcePipeline.SelectPublicationStep(row));
        var changed = terminal switch
        {
            "intent" => row with { R2TerminalIntentCode = "CONTENT_COMMITMENT_MISMATCH",
                R2TerminalIntentDisposition = "Terminated", R2TerminalIntentAtUtc = DateTimeOffset.UtcNow },
            "operational" => row with { R2TerminationDisposition = "Terminated", R2TerminatedAtUtc = DateTimeOffset.UtcNow },
            _ => row with { R2TerminalOutcomeCode = "RECAPTURE_REQUIRED" }
        };
        Assert.Equal(RetainedPublicationStep.None, CaptureRuntimeSourcePipeline.SelectPublicationStep(changed));
        // Selector-only defense, not a fabricated SQL terminal fixture. Real
        // CP08 validates complete legal tuples before this selector is reached.
    }

    [Fact]
    public async Task WorkerRunsSequentiallyAndRefusesOverlappingLoop()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_worker_loop");
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var source = await Tip88C1C6BA3RetentionCheckpointTests.CreateVerifiedContinuationSource(isolated, minio);
        await using var observer = isolated.CreateDbContext();
        await using var logins = await Tip88C1C6BA3SyntheticComposition.CustodyLogins(observer.Database.GetConnectionString()!);
        await using var scopes = Owners(logins.Connections, minio);
        var pipeline = new CaptureRuntimeSourcePipeline(scopes);
        var worker = new CaptureRuntimeSourceContinuationWorker(pipeline,
            RawIngressBrokerOptions.Read(Tip88C1C6BA3SyntheticComposition.BrokerConfiguration()),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<CaptureRuntimeSourceContinuationWorker>.Instance);
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var run = worker.RunAsync(stop.Token);
        try
        {
            var overlap = await Assert.ThrowsAsync<InvalidOperationException>(() => worker.RunAsync(stop.Token));
            Assert.Equal("RAW_INGRESS_CONTINUATION_ALREADY_RUNNING", overlap.Message);
            await using var readSource = NpgsqlDataSource.Create(observer.Database.GetConnectionString()!);
            var reader = new RawSourceRetentionContinuationRepository(readSource);
            while ((await reader.ReadAsync(source, stop.Token))?.CustodyState != "Available")
                await Task.Delay(25, stop.Token);
            Assert.Single(await observer.RawExportSourceEncryptionAttempts.AsNoTracking()
                .Where(x => x.SourceArtifactId == source).ToListAsync());
            Assert.Single(await observer.RawExportProvisionalObjects.AsNoTracking()
                .Where(x => x.SourceArtifactId == source).ToListAsync());
        }
        finally
        {
            stop.Cancel();
            try { await run.WaitAsync(TimeSpan.FromSeconds(5)); }
            catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
        }
    }

    internal static CaptureRuntimeCustodyProviderScopes Owners(string[] connections, DurableObjectMinioFixture minio)
    {
        var c = new[] { ProvisionalObjectCapability.Writer, ProvisionalObjectCapability.Reconciler,
            ProvisionalObjectCapability.Lifecycle }.Select(x =>
                Tip88C1C6BA3SyntheticComposition.ObjectConfiguration(minio.Options(x))).ToArray();
        return new(connections[0], c[0], connections[1], c[1], connections[2], c[2]);
    }

    private static Task<string[]> History(TagEkycDbContext db) => db.Database.SqlQueryRaw<string>("""
        SELECT 'key:'||to_jsonb(k)::text AS "Value" FROM tagekyc.raw_export_attempt_key_reservations k
        UNION ALL SELECT 'object:'||to_jsonb(o)::text FROM tagekyc.raw_export_provisional_objects o
        ORDER BY "Value"
        """).ToArrayAsync();
}

// These initial tests exercise the extracted production preflight, not the
// future HTTP/B commit or recovery composition. No database proof is implied.
public sealed class Tip88C1C6BA3BrokerPipelineTests
{
    private static readonly SubjectTokenKeySelector SubjectSelector = new("synthetic-subject", 7);
    private static readonly SourceEncryptionProfileBundle Profile =
        new("synthetic-storage", "synthetic-source", 1, "synthetic-suite", 1, "random96-per-frame", 4096);

    [Fact]
    public void BrokerSettingsRejectUnqualifiedBoundsBeforeOpeningConnection()
    {
        using var source = NpgsqlDataSource.Create("Host=127.0.0.1;Port=1;Database=never_opened;Username=synthetic");
        var providers = new Providers();
        var valid = new RawIngressBrokerTransactionSettings(Guid.Parse("8bf2e6a5425a42689647465139fc46b0"),
            10, 100, new("fixture-content-commitment", 1), new("fixture-subject-ref-token", 1), 1000);
        Assert.NotNull(Tip88C1C6BA3SyntheticComposition.Broker(source, providers, providers, exactSettings: valid));
        foreach (var invalid in new[]
        {
            valid with { EvaluationOwnerId = Guid.Empty },
            valid with { EvaluationTokenTtlSeconds = 0 }, valid with { EvaluationTokenTtlSeconds = 3601 },
            valid with { IdempotencyLockTimeoutMilliseconds = 0 }, valid with { IdempotencyLockTimeoutMilliseconds = 30001 },
            valid with { RequestTimeoutMilliseconds = 0 }, valid with { RequestTimeoutMilliseconds = 30001 },
            valid with { IdempotencyLockTimeoutMilliseconds = 1000 },
            valid with { EvaluationTokenTtlSeconds = 1 }, valid with { RequestTimeoutMilliseconds = 2000 },
        })
            Assert.Equal("RAW_INGRESS_BROKER_STATE_INVALID", Assert.Throws<InvalidOperationException>(() =>
                Tip88C1C6BA3SyntheticComposition.Broker(source, providers, providers, exactSettings: invalid)).Message);
        Assert.Equal(0, providers.CommitmentCalls);
        Assert.Equal(0, providers.SubjectCalls);
    }

    [Theory]
    [InlineData("NewClaimEvaluationToken")]
    [InlineData("ExistingClaimComparisonToken")]
    public async Task PreflightPreservesExactC1RecipesAndExplicitSelectors(string variant)
    {
        var input = Input(variant);
        var providers = new Providers();
        var result = await new RetainedSourceClaimPreflight(providers, providers)
            .ComputeAsync(input, SubjectSelector, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(input.ProducerEnvelopeFingerprint.ToArray(), result.ProducerEnvelopeFingerprint);
        Assert.Equal(new CommitmentKeySelector("historic-commitment", 3), providers.CommitmentSelector);
        Assert.Equal(SubjectSelector, providers.SubjectSelector);
        Assert.Equal(Payload("TAG-EKYC:RAW-EXPORT:CONTENT-COMMITMENT:C1:V1",
            "scope-a", "controller-a", "11111111111111111111111111111111",
            "22222222222222222222222222222222", "2", "ChipDg2Portrait",
            new string('a', 64), "24", "image/jpeg"), providers.CommitmentPayload);
        Assert.Equal(Payload("TAG-EKYC:RAW-EXPORT:SUBJECT-TOKEN:C1:V1",
            "scope-a", "controller-a", "caf\u00e9"), providers.SubjectPayload);
        Assert.Equal(SHA256.HashData(providers.CommitmentPayload!), result.ContentCommitment);
        Assert.Equal(SHA256.HashData(providers.SubjectPayload!), result.SubjectToken);
        var profileDigests = RetainedSourceClaimPreflight.ComputeProfileDigests(Profile);
        Assert.Equal(SHA256.HashData(Payload("tip-88c1-nonce-seed-commitment-v1",
            "random96-per-frame", "none")), profileDigests.NonceSeedCommitment);
        Assert.Equal(SHA256.HashData(Payload("tip-88c1-framing-parameters-v1",
            "synthetic-suite", "1", "4096", "random96-per-frame")), profileDigests.FramingParametersDigest);
        Assert.Equal(1, providers.CommitmentCalls);
        Assert.Equal(1, providers.SubjectCalls);
    }

    [Fact]
    public async Task HistoricCommitmentDoesNotFallbackToLatest()
    {
        var providers = new Providers { FailCommitment = true };
        var result = await new RetainedSourceClaimPreflight(providers, providers)
            .ComputeAsync(Input("ExistingClaimComparisonToken"), SubjectSelector, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Null(result.ContentCommitment);
        Assert.Equal(new byte[32], result.SubjectToken);
        Assert.Equal(new CommitmentKeySelector("historic-commitment", 3), providers.CommitmentSelector);
        Assert.Equal(1, providers.CommitmentCalls);
        Assert.Equal(0, providers.SubjectCalls);
    }

    [Theory]
    [InlineData(true, "RAW_EXPORT_CONTENT_COMMITMENT_PROVIDER_FAILURE")]
    [InlineData(false, "RAW_EXPORT_SUBJECT_TOKEN_PROVIDER_FAILURE")]
    public async Task NewPreflightProviderFailureCannotBecomeHistoricUnavailable(bool commitment, string error)
    {
        var providers = new Providers { FailCommitment = commitment, FailSubject = !commitment };
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new RetainedSourceClaimPreflight(providers, providers)
                .ComputeAsync(Input("NewClaimEvaluationToken"), SubjectSelector, CancellationToken.None));
        Assert.Equal(error, ex.Message);
        Assert.Equal(1, providers.CommitmentCalls);
        Assert.Equal(commitment ? 0 : 1, providers.SubjectCalls);
    }

    [Theory]
    [InlineData("envelope")]
    [InlineData("length")]
    [InlineData("token")]
    [InlineData("invalid-selector-and-envelope")]
    public async Task LockedEnvelopeMismatchCallsNoProvider(string change)
    {
        var input = Input("ExistingClaimComparisonToken");
        input = change switch
        {
            "envelope" => input with { ProducerEnvelopeFingerprint = new byte[32] },
            "length" => input with { ClaimedPlaintextLength = 25 },
            "invalid-selector-and-envelope" => input with
                { CommitmentSelectorId = "bad key", ProducerEnvelopeFingerprint = new byte[32] },
            _ => input with { TokenVariant = "Unknown" }
        };
        var providers = new Providers();
        Assert.Null(await new RetainedSourceClaimPreflight(providers, providers)
            .ComputeAsync(input, SubjectSelector, CancellationToken.None));
        Assert.Equal(0, providers.CommitmentCalls);
        Assert.Equal(0, providers.SubjectCalls);
    }

    [Fact]
    public async Task PreflightPropagatesCancellationWithoutProviderWork()
    {
        using var cancel = new CancellationTokenSource();
        cancel.Cancel();
        var providers = new Providers();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new RetainedSourceClaimPreflight(providers, providers)
                .ComputeAsync(Input("NewClaimEvaluationToken"), SubjectSelector, cancel.Token));
        Assert.Equal(0, providers.CommitmentCalls);
        Assert.Equal(0, providers.SubjectCalls);
    }

    private static SourceClaimPreflightInput Input(string variant)
    {
        var at = new DateTimeOffset(2026, 9, 14, 1, 2, 3, TimeSpan.Zero).AddTicks(1234560);
        // Expected producer-envelope bytes are encoded independently of the
        // production helper. The C1 microsecond grammar is not CRT1's grammar.
        var envelope = SHA256.HashData(Payload("tip-88c1-producer-claim-envelope-v2",
            new string('0', 64), "24", "image/jpeg", "2026-09-14T01:02:03.123456Z",
            "2026-09-14T01:02:03.123456Z", "2026-09-14T01:03:03.123456Z", "60"));
        return new(new byte[32], envelope,
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            2, "ChipDg2Portrait", "scope-a", "controller-a", "cafe\u0301",
            "historic-commitment", 3, variant,
            Enumerable.Repeat((byte)0xaa, 32).ToArray(), 24, "image/jpeg", at, at, at.AddMinutes(1), 60);
    }

    private static byte[] Payload(params string[] parts)
    {
        using var stream = new MemoryStream();
        foreach (var part in parts)
        {
            var bytes = Encoding.UTF8.GetBytes(part.Normalize(NormalizationForm.FormC));
            var prefix = new byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(prefix, checked((uint)bytes.Length));
            stream.Write(prefix);
            stream.Write(bytes);
        }
        return stream.ToArray();
    }

    private sealed class Providers : IContentCommitmentService, ISubjectRefTokenService
    {
        internal bool FailCommitment { get; init; }
        internal bool FailSubject { get; init; }
        internal int CommitmentCalls { get; private set; }
        internal int SubjectCalls { get; private set; }
        internal CommitmentKeySelector? CommitmentSelector { get; private set; }
        internal SubjectTokenKeySelector? SubjectSelector { get; private set; }
        internal byte[]? CommitmentPayload { get; private set; }
        internal byte[]? SubjectPayload { get; private set; }

        public ValueTask<ContentCommitmentResult> ComputeAsync(CommitmentKeySelector selector,
            ReadOnlyMemory<byte> lpPayload, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CommitmentCalls++;
            CommitmentSelector = selector;
            CommitmentPayload = lpPayload.ToArray();
            return ValueTask.FromResult(FailCommitment
                ? ContentCommitmentResult.Failed(ContentCommitmentFailure.ProviderFailure)
                : ContentCommitmentResult.Success(SHA256.HashData(lpPayload.Span)));
        }

        public ValueTask<SubjectRefTokenResult> ComputeAsync(SubjectTokenKeySelector selector,
            ReadOnlyMemory<byte> lpPayload, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SubjectCalls++;
            SubjectSelector = selector;
            SubjectPayload = lpPayload.ToArray();
            return ValueTask.FromResult(FailSubject
                ? SubjectRefTokenResult.Failed(SubjectRefTokenFailure.ProviderFailure)
                : SubjectRefTokenResult.Success(SHA256.HashData(lpPayload.Span)));
        }
    }
}
