using TagEkyc.Application;
using System.Globalization;
using TagEkyc.Application.Ports;
using TagEkyc.Application.RawExport;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Infrastructure.RawExport;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using TagEkyc.Api;
using TagEkyc.Application.VerificationSessions;

namespace TagEkyc.UnitTests;

public sealed class Tip88C1C6BRawIngressTransportTests
{
    private static readonly Guid ClientId = Guid.Parse("11111111-1111-4111-8111-111111111111");
    [Fact]
    public void Prepared_real_registration_validates_without_broker_pipeline_or_database()
    {
        var services = new ServiceCollection();
        services.AddPreparedRawExportSourceIngressServices();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
            { ValidateScopes = true, ValidateOnBuild = true });
        using var scope = provider.CreateScope();
        Assert.Null(scope.ServiceProvider.GetService<IRawExportSourceIngressBroker>());
        Assert.Null(scope.ServiceProvider.GetService<IRawExportSourceBodyPipeline>());
        Assert.Throws<InvalidOperationException>(() => scope.ServiceProvider.GetRequiredService<RawExportSourceIngressApplicationService>());
    }

    [Fact]
    public async Task Prepared_missing_real_ingress_dependency_returns_closed_503_without_body_read()
    {
        var body = new UnreadBody();
        var builder = new WebHostBuilder().ConfigureServices(services =>
        {
            services.AddRouting();
            services.AddSingleton<IApiKeyAuthenticator, Authenticator>();
            services.AddPreparedRawExportSourceIngressServices();
        }).Configure(app =>
        {
            app.Use(async (context, next) => { context.Request.Body = body; await next(context); });
            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapRawExportSourceIngressEndpoints());
        });
        using var server = new TestServer(builder);
        using var client = server.CreateClient();
        var metadata = Metadata();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/ekyc/raw-export/source-ingress")
            { Content = new ByteArrayContent(new byte[10]) };
        request.Content.Headers.ContentType = new("image/jpeg");
        Add(request, "X-TagEkyc-Agent-Configuration-Revision", metadata.AgentConfigurationRevision.ToString(CultureInfo.InvariantCulture));
        Add(request, "X-TagEkyc-Verification-Session-Id", metadata.VerificationSessionId.ToString());
        Add(request, "X-TagEkyc-Capture-Artifact-Id", metadata.CaptureArtifactId.ToString());
        Add(request, "X-TagEkyc-Capture-Revision", metadata.CaptureRevision.ToString(CultureInfo.InvariantCulture));
        Add(request, "X-TagEkyc-Raw-Class", metadata.RawClass);
        Add(request, "Idempotency-Key", metadata.IngressIdempotencyKey.ToString("N"));
        Add(request, "X-TagEkyc-Plaintext-Sha256", metadata.ClaimedPlaintextDigest);
        Add(request, "X-TagEkyc-Captured-At-Utc", metadata.CapturedAtUtc.ToString("O"));
        Add(request, "X-TagEkyc-Retention-Started-At-Utc", metadata.PlaintextRetentionStartedAtUtc.ToString("O"));
        Add(request, "X-TagEkyc-Retention-Expires-At-Utc", metadata.PlaintextRetentionExpiresAtUtc.ToString("O"));
        Add(request, "X-TagEkyc-Retention-Budget-Seconds", metadata.PlaintextRetentionBudgetSeconds.ToString(CultureInfo.InvariantCulture));
        using var response = await client.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Equal(System.Net.HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("RAW_EXPORT_SOURCE_TEMPORARILY_UNAVAILABLE", json);
        Assert.DoesNotContain("IRawExport", json);
        Assert.Equal(0, body.Reads);
    }

    private sealed class UnreadBody : MemoryStream
    {
        public int Reads;
        public override int Read(byte[] buffer, int offset, int count) { Reads++; throw new InvalidOperationException("Unexpected body read."); }
        public override int Read(Span<byte> buffer) { Reads++; throw new InvalidOperationException("Unexpected body read."); }
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct) { Reads++; throw new InvalidOperationException("Unexpected body read."); }
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default) { Reads++; throw new InvalidOperationException("Unexpected body read."); }
    }

    [Fact]
    public async Task Ingress_route_rejects_invalid_metadata_without_calling_broker_or_pipeline()
    {
        var sequence = new List<string>();
        var handoff = new RawExportR2Handoff(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, 1);
        var builder = new WebHostBuilder().ConfigureServices(services =>
        {
            services.AddRouting();
            services.AddSingleton<IApiKeyAuthenticator, Authenticator>();
            services.AddSingleton<IRawExportIngressCapacity>(new RawExportIngressCapacity(1, 1, 1024, 1024));
            services.AddSingleton<ICaptureAgentConfigurationProvider, ConfigurationProvider>();
            services.AddSingleton<IRawExportCaptureAcceptanceResolver, AcceptanceResolver>();
            services.AddSingleton<IRawExportSourceIngressBroker>(new Broker(new(null, handoff), sequence));
            services.AddSingleton<IRawExportSourceBodyPipeline>(new Pipeline(sequence));
            services.AddScoped<RawExportSourceIngressApplicationService>();
        }).Configure(app =>
        {
            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapRawExportSourceIngressEndpoints());
        });
        using var server = new TestServer(builder);
        using var client = server.CreateClient();
        var metadata = Metadata();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/ekyc/raw-export/source-ingress");
        request.Content = new TrackingContent(sequence, new byte[10]);
        request.Content.Headers.ContentType = new("image/jpeg");
        Add(request, "X-TagEkyc-Client-Application-Id", metadata.ClientApplicationId.ToString());
        Add(request, "X-TagEkyc-Producer-Id", metadata.ProducerId);
        request.Headers.TryAddWithoutValidation("X-TagEkyc-Producer-Id", "duplicate");
        Add(request, "X-TagEkyc-Capture-Agent-Instance-Id", metadata.CaptureAgentInstanceId);
        Add(request, "X-TagEkyc-Agent-Configuration-Revision", metadata.AgentConfigurationRevision.ToString(CultureInfo.InvariantCulture));
        Add(request, "X-TagEkyc-Verification-Session-Id", metadata.VerificationSessionId.ToString());
        Add(request, "X-TagEkyc-Capture-Artifact-Id", metadata.CaptureArtifactId.ToString());
        Add(request, "X-TagEkyc-Capture-Revision", metadata.CaptureRevision.ToString(CultureInfo.InvariantCulture));
        Add(request, "X-TagEkyc-Raw-Class", metadata.RawClass);
        Add(request, "Idempotency-Key", metadata.IngressIdempotencyKey.ToString("N"));
        Add(request, "X-TagEkyc-Plaintext-Sha256", metadata.ClaimedPlaintextDigest);
        Add(request, "X-TagEkyc-Captured-At-Utc", metadata.CapturedAtUtc.ToString("O"));
        Add(request, "X-TagEkyc-Retention-Started-At-Utc", metadata.PlaintextRetentionStartedAtUtc.ToString("O"));
        Add(request, "X-TagEkyc-Retention-Expires-At-Utc", metadata.PlaintextRetentionExpiresAtUtc.ToString("O"));
        Add(request, "X-TagEkyc-Retention-Budget-Seconds", metadata.PlaintextRetentionBudgetSeconds.ToString(CultureInfo.InvariantCulture));

        using var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        Assert.DoesNotContain("broker-committed-r1", sequence);
        Assert.DoesNotContain("pipeline", sequence);
    }

    private static void Add(HttpRequestMessage request, string name, string value) =>
        Assert.True(request.Headers.TryAddWithoutValidation(name, value));
    [Fact]
    public async Task Configuration_get_is_self_bound_and_honors_etag()
    {
        var builder = new WebHostBuilder().ConfigureServices(services =>
        {
            services.AddRouting();
            services.AddSingleton<IApiKeyAuthenticator, Authenticator>();
            services.AddSingleton<ICaptureAgentConfigurationProvider, ConfigurationProvider>();
        }).Configure(app =>
        {
            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapCaptureAgentConfigurationEndpoints());
        });
        using var server = new TestServer(builder);
        using var client = server.CreateClient();

        var first = await client.GetAsync("/api/ekyc/capture-agents/self/configuration");
        Assert.Equal(System.Net.HttpStatusCode.OK, first.StatusCode);
        var etag = first.Headers.ETag;
        Assert.NotNull(etag);

        using var secondRequest = new HttpRequestMessage(HttpMethod.Get,
            "/api/ekyc/capture-agents/self/configuration");
        secondRequest.Headers.IfNoneMatch.Add(etag!);
        var second = await client.SendAsync(secondRequest);
        Assert.Equal(System.Net.HttpStatusCode.NotModified, second.StatusCode);
    }

    [Fact]
    public async Task Metadata_outcome_never_opens_body()
    {
        var opens = 0;
        var expected = new CaptureAgentFinalResult(RawExportSourceIngressCodes.AlreadyAvailable, Guid.NewGuid(), "Available", "Available");
        var service = new RawExportSourceIngressApplicationService(
            new RawExportIngressCapacity(1, 1, 1024, 1024),
            new ConfigurationProvider(),
            new AcceptanceResolver(),
            new Broker(new RawExportBrokerAdmission(expected, null)),
            new Pipeline());

        var result = await service.ExecuteAsync(Caller(), Metadata(), () =>
        {
            opens++;
            return Stream.Null;
        }, CancellationToken.None);

        Assert.Same(expected, result);
        Assert.Equal(0, opens);
    }

    [Fact]
    public async Task Body_is_opened_only_after_broker_returns_committed_handoff()
    {
        var sequence = new List<string>();
        var handoff = new RawExportR2Handoff(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, 1);
        var service = new RawExportSourceIngressApplicationService(
            new RawExportIngressCapacity(1, 1, 1024, 1024),
            new ConfigurationProvider(),
            new AcceptanceResolver(),
            new Broker(new RawExportBrokerAdmission(null, handoff), sequence),
            new Pipeline(sequence));

        await service.ExecuteAsync(Caller(), Metadata(), () =>
        {
            sequence.Add("open-body");
            return Stream.Null;
        }, CancellationToken.None);

        Assert.Equal(["broker-committed-r1", "open-body", "pipeline"], sequence);
    }

    [Fact]
    public void Capacity_is_atomic_and_dispose_is_idempotent()
    {
        var capacity = new RawExportIngressCapacity(1, 1, 10, 10);
        var first = capacity.TryAcquire("agent", 10);
        Assert.NotNull(first);
        Assert.Null(capacity.TryAcquire("agent", 1));
        first!.Dispose();
        first.Dispose();
        Assert.NotNull(capacity.TryAcquire("agent", 10));
    }

    [Fact]
    public async Task Stale_agent_configuration_fails_before_broker_and_body()
    {
        var brokerCalls = new List<string>();
        var opens = 0;
        var service = new RawExportSourceIngressApplicationService(
            new RawExportIngressCapacity(1, 1, 1024, 1024),
            new ConfigurationProvider(revision: 2),
            new AcceptanceResolver(),
            new Broker(new RawExportBrokerAdmission(null, null), brokerCalls),
            new Pipeline());

        var result = await service.ExecuteAsync(Caller(), Metadata(), () =>
        {
            opens++;
            return Stream.Null;
        }, CancellationToken.None);

        Assert.Equal(RawExportSourceIngressCodes.BindingInvalid, result.OutcomeCode);
        Assert.Empty(brokerCalls);
        Assert.Equal(0, opens);
    }

    private static AuthenticatedClientContext Caller() => new(
        ClientId, Guid.NewGuid(), "prefix", AuthenticatedCallerCategory.CaptureAgent,
        new HashSet<string> { "capture.raw-export.source.ingress" }, null, new HashSet<string> { "agent" });

    private static RawExportSourceIngressMetadata Metadata()
    {
        var started = DateTimeOffset.UtcNow;
        return new(
        ClientId, "agent", "instance", 1, Guid.NewGuid(), Guid.NewGuid(), 1,
        "ChipDg2Portrait", Guid.NewGuid(), "image/jpeg", 10, new string('a', 64),
        started, started, started.AddMinutes(1), 60);
    }

    private sealed class ConfigurationProvider(long revision = 1) : ICaptureAgentConfigurationProvider
    {
        public Task<CaptureAgentConfigurationProjection?> GetSelfAsync(AuthenticatedClientContext caller,
            CancellationToken cancellationToken) => GetCurrentAsync(caller, "agent", cancellationToken);

        public Task<CaptureAgentConfigurationProjection?> GetCurrentAsync(AuthenticatedClientContext caller,
            string producerId,
            CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;
            return Task.FromResult<CaptureAgentConfigurationProjection?>(new(
                new(revision, now.AddMinutes(-1), now.AddMinutes(5), true, 60, 1), "\"etag\""));
        }
    }

    private sealed class Authenticator : IApiKeyAuthenticator
    {
        public Task<SessionOperationResult<AuthenticatedClientContext>> AuthenticateAsync(HttpContext httpContext,
            string? requiredScope = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(SessionOperationResult<AuthenticatedClientContext>.Success(Caller()));
    }

    private sealed class Broker(RawExportBrokerAdmission result, List<string>? sequence = null)
        : IRawExportSourceIngressBroker
    {
        public Task<RawExportBrokerAdmission> AdmitAsync(AuthenticatedClientContext caller,
            RawExportSourceIngressMetadata metadata, RawExportResolvedCaptureAcceptance acceptance,
            CancellationToken cancellationToken)
        {
            sequence?.Add("broker-committed-r1");
            return Task.FromResult(result);
        }
    }

    private sealed class AcceptanceResolver : IRawExportCaptureAcceptanceResolver
    {
        public Task<RawExportResolvedCaptureAcceptance?> ResolveExactAsync(
            AuthenticatedClientContext caller, RawExportSourceIngressMetadata metadata,
            CancellationToken cancellationToken) =>
            Task.FromResult<RawExportResolvedCaptureAcceptance?>(new(Guid.NewGuid(), "challenge", "agent", "instance"));
    }

    private sealed class Pipeline(List<string>? sequence = null) : IRawExportSourceBodyPipeline
    {
        public Task<CaptureAgentFinalResult> ProcessAsync(RawExportSourceIngressMetadata metadata,
            RawExportR2Handoff handoff, Stream body, CancellationToken cancellationToken)
        {
            sequence?.Add("pipeline");
            return Task.FromResult(new CaptureAgentFinalResult(RawExportSourceIngressCodes.Available,
                handoff.SourceArtifactId, "Available", "Available"));
        }
    }

    private sealed class TrackingContent(List<string> sequence, byte[] bytes) : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, System.Net.TransportContext? context)
        {
            sequence.Add("content-copy");
            return stream.WriteAsync(bytes).AsTask();
        }

        protected override bool TryComputeLength(out long length)
        {
            length = bytes.Length;
            return true;
        }
    }
}
