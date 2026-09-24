using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TagEkyc.Api;
using TagEkyc.Application;
using TagEkyc.Application.CaptureRuntime;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.CaptureAgent;
using TagEkyc.Contracts.CaptureRuntime;
using TagEkyc.Contracts.TrustedAdapter;
using TagEkyc.Domain;
using TagEkyc.Infrastructure.CaptureRuntime;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88C1C6BA3CapabilityLineageTests(PostgresPersistenceFixture postgres)
{
    private static readonly Guid ClientId = Guid.Parse("a3000000-0000-4000-8000-000000000030");
    private static readonly Guid Principal = Guid.Parse("a3000000-0000-4000-8000-000000000031");
    private static readonly Guid OtherPrincipal = Guid.Parse("a3000000-0000-4000-8000-000000000032");

    [Fact]
    public async Task A3_R20_PrincipalPartitionAndSecretOnceReplay()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_r20_partition");
        await Tip88C1C6BA3ConsentRetentionTests.Prepare(isolated);
        await using var db = isolated.CreateDbContext();
        var session = await Session(db);
        var identity = new Identity();
        await using var app = await Start(db.Database.GetConnectionString()!, identity);
        using var client = app.GetTestClient(); var key = Guid.NewGuid();
        using var created = await Post(client, session, "{\"action\":\"Issue\"}", key);
        Assert.True(created.StatusCode == HttpStatusCode.Created, await created.Content.ReadAsStringAsync());
        using var json = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        Assert.Equal(43, json.RootElement.GetProperty("Secret").GetString()!.Length);
        await Counts(isolated, 1, 1, 1);
        await using var observer = isolated.CreateDbContext();
        Assert.Equal(Principal, await observer.Database.SqlQueryRaw<Guid>(
            "SELECT \"PrincipalId\" AS \"Value\" FROM tagekyc.capture_capabilities").SingleAsync());
        Assert.Equal("NonRetained", await observer.Database.SqlQueryRaw<string>(
            "SELECT \"AuthorityMode\" AS \"Value\" FROM tagekyc.capture_capabilities").SingleAsync());
        var fingerprint = await observer.Database.SqlQueryRaw<byte[]>(
            "SELECT \"RequestFingerprint\" AS \"Value\" FROM tagekyc.capture_capability_operations").SingleAsync();
        Assert.Equal(32, fingerprint.Length);
        identity.Actor = identity.Actor with { ApiKeyId = Guid.NewGuid() };
        using var replay = await Post(client, session, "{\"action\":\"Issue\"}", key);
        Assert.Contains("EXISTING_MATCH_SECRET_UNAVAILABLE", await replay.Content.ReadAsStringAsync());
        Assert.DoesNotContain("\"Secret\"", await replay.Content.ReadAsStringAsync());
        await Counts(isolated, 1, 1, 1);
        Assert.Equal(fingerprint, await observer.Database.SqlQueryRaw<byte[]>(
            "SELECT \"RequestFingerprint\" AS \"Value\" FROM tagekyc.capture_capability_operations").SingleAsync());
        identity.Actor = identity.Actor with { PrincipalId = OtherPrincipal };
        using var differentPrincipal = await Post(client, session, "{\"action\":\"Issue\"}", key);
        Assert.Equal(HttpStatusCode.Conflict, differentPrincipal.StatusCode);
        await Counts(isolated, 1, 1, 1);
    }

    [Fact]
    public async Task A3_R20_ReplaceCannotTransferPrincipal()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_r20_replace");
        await Tip88C1C6BA3ConsentRetentionTests.Prepare(isolated);
        await using var db = isolated.CreateDbContext(); var session = await Session(db);
        var identity = new Identity();
        await using var app = await Start(db.Database.GetConnectionString()!, identity);
        using var client = app.GetTestClient();
        using var created = await Post(client, session, "{\"action\":\"Issue\"}", Guid.NewGuid());
        Assert.True(created.IsSuccessStatusCode, await created.Content.ReadAsStringAsync());
        using var json = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var id = json.RootElement.GetProperty("CaptureCapabilityId").GetString();
        var replace = $"{{\"action\":\"Replace\",\"currentCapabilityId\":\"{id}\",\"expectedRevision\":1}}";
        var key = Guid.NewGuid(); identity.Actor = identity.Actor with { PrincipalId = OtherPrincipal };
        using var denied = await Post(client, session, replace, key);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        await Counts(isolated, 1, 1, 1);
        identity.Actor = identity.Actor with { PrincipalId = Principal };
        using var accepted = await Post(client, session, replace, key);
        Assert.True(accepted.StatusCode == HttpStatusCode.Created, await accepted.Content.ReadAsStringAsync());
        await Counts(isolated, 2, 2, 2);
        await using var observer = isolated.CreateDbContext();
        Assert.All(await observer.Database.SqlQueryRaw<Guid>(
            "SELECT \"PrincipalId\" AS \"Value\" FROM tagekyc.capture_capabilities").ToListAsync(), p => Assert.Equal(Principal, p));
        using var replay = await Post(client, session, replace, key);
        Assert.Contains("EXISTING_MATCH_SECRET_UNAVAILABLE", await replay.Content.ReadAsStringAsync());
        await Counts(isolated, 2, 2, 2);
    }

    [Theory]
    [InlineData("{\"Action\":\"Issue\"}")]
    [InlineData("{\"action\":\"Issue\",\"consentBindingId\":null}")]
    [InlineData("{\"action\":\"Issue\",\"consentBindingId\":\"00000000000000000000000000000000\"}")]
    [InlineData("{\"action\":\"Issue\",\"currentCapabilityId\":null}")]
    [InlineData("{\"action\":\"Issue\",\"expectedRevision\":1}")]
    [InlineData("{\"action\":\"Issue\",\"principalId\":\"a3000000000040008000000000000031\"}")]
    [InlineData("{\"action\":\"Issue\",\"action\":\"Issue\"}")]
    [InlineData("{\"action\":null}")]
    [InlineData("{\"action\":\"Replace\",\"currentCapabilityId\":\"a3000000000040008000000000000031\",\"expectedRevision\":1,\"consentBindingId\":null}")]
    [InlineData("{\"action\":\"Replace\",\"currentCapabilityId\":\"a3000000000040008000000000000031\",\"expectedRevision\":null}")]
    public async Task A3_R20_ClosedJsonRejectsBeforePersistence(string body)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_r20_json");
        await Tip88C1C6BA3ConsentRetentionTests.Prepare(isolated);
        await using var db = isolated.CreateDbContext(); var session = await Session(db);
        await using var app = await Start(db.Database.GetConnectionString()!, new Identity());
        using var client = app.GetTestClient();
        using var result = await Post(client, session, body, Guid.NewGuid());
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        await Counts(isolated, 0, 0, 0);
    }

    private static async Task<Guid> Session(TagEkycDbContext db)
    {
        var now = DateTimeOffset.UtcNow;
        var session = VerificationSession.Create(ClientId, "synthetic-subject", VerificationProfile.ChallengeBoundEkycProfile,
            "synthetic-a3", [RequiredCheckType.DocumentNfc], now.AddHours(1), now, challenge: "synthetic-challenge");
        await new EfVerificationSessionRepository(db).AddAsync(session); return session.Id;
    }
    private static async Task Counts(PostgresPersistenceFixture.DisposableCurrentDatabase isolated, int capabilities, int operations, int events)
    {
        await using var observer = isolated.CreateDbContext();
        Assert.Equal(capabilities, await observer.Database.SqlQueryRaw<int>("SELECT count(*)::integer AS \"Value\" FROM tagekyc.capture_capabilities").SingleAsync());
        Assert.Equal(operations, await observer.Database.SqlQueryRaw<int>("SELECT count(*)::integer AS \"Value\" FROM tagekyc.capture_capability_operations").SingleAsync());
        Assert.Equal(events, await observer.Database.SqlQueryRaw<int>("SELECT count(*)::integer AS \"Value\" FROM tagekyc.capture_capability_events").SingleAsync());
        Assert.Equal(0, await observer.Database.SqlQueryRaw<int>("SELECT count(*)::integer AS \"Value\" FROM tagekyc.raw_source_retention_permits").SingleAsync());
    }
    private static async Task<HttpResponseMessage> Post(HttpClient client, Guid session, string body, Guid key)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/ekyc/verification-sessions/{session:N}/capture-capabilities");
        request.Headers.Add("Idempotency-Key", key.ToString("N"));
        request.Content = new ByteArrayContent(Encoding.UTF8.GetBytes(body));
        request.Content.Headers.ContentType = new("application/json");
        return await client.SendAsync(request);
    }
    private static async Task<WebApplication> Start(string connection, Identity identity)
    {
        var builder = WebApplication.CreateBuilder(); builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IApiKeyAuthenticator>(identity);
        builder.Services.AddSingleton<ICaptureRuntimeDbContextFactory>(new Contexts(connection));
        builder.Services.AddScoped<ICaptureRuntimeExecutionGateway, CaptureRuntimeExecutionPersistenceBoundary>();
        builder.Services.AddSingleton<ICaptureRuntimeVerifierPepperSource, SyntheticPeppers>();
        builder.Services.AddSingleton<ICaptureRuntimeAppendGateway, NoAppend>();
        builder.Services.AddScoped<ICaptureRuntimeExecutionService, CaptureRuntimeExecutionApplicationService>();
        var app = builder.Build(); app.MapCaptureRuntimeExecutionEndpoints(); await app.StartAsync(); return app;
    }
    private sealed class Contexts(string connection) : ICaptureRuntimeDbContextFactory
    {
        public ValueTask<TagEkycDbContext> CreateAsync(CancellationToken ct = default) => ValueTask.FromResult(
            new TagEkycDbContext(new DbContextOptionsBuilder<TagEkycDbContext>().UseNpgsql(connection).Options));
    }
    private sealed class Identity : IApiKeyAuthenticator
    {
        public AuthenticatedClientContext Actor = new(Guid.NewGuid(), ClientId, "synthetic",
            AuthenticatedCallerCategory.BusinessConsumer, new HashSet<string>(), PrincipalId: Principal);
        public Task<SessionOperationResult<AuthenticatedClientContext>> AuthenticateAsync(HttpContext context, string? requiredScope = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(SessionOperationResult<AuthenticatedClientContext>.Success(Actor));
    }
    private sealed class SyntheticPeppers : ICaptureRuntimeVerifierPepperSource
    {
        public int CurrentVersion => 1;
        public ValueTask<ICaptureRuntimeVerifierPepperLease?> TryResolveAsync(int version, CaptureRuntimeVerifierPepperDomain domain, CancellationToken ct = default) =>
            ValueTask.FromResult<ICaptureRuntimeVerifierPepperLease?>(new Lease(version, domain));
    }
    private sealed class Lease(int version, CaptureRuntimeVerifierPepperDomain domain) : ICaptureRuntimeVerifierPepperLease
    {
        public int Version => version;
        public CaptureRuntimeVerifierPepperDomain Domain => domain;
        public ReadOnlyMemory<byte> Key { get; } = new byte[32];
        public void Dispose() { }
    }
    private sealed class NoAppend : ICaptureRuntimeAppendGateway
    {
        public Task<SessionOperationResult<CaptureArtifactSubmissionResponseDto>> AppendCaptureArtifactAsync(AuthenticatedCaptureRuntimeContext actor, Guid bindingId, CaptureRuntimeCaptureArtifactRequest request, Guid key, CancellationToken ct) => throw new InvalidOperationException("R20_MUST_NOT_APPEND");
        public Task<SessionOperationResult<EvidenceResultSubmissionResponseDto>> AppendEvidenceResultAsync(AuthenticatedCaptureRuntimeContext actor, Guid sessionId, CaptureRuntimeEvidenceResultRequest request, Guid key, CancellationToken ct) => throw new InvalidOperationException("R20_MUST_NOT_APPEND");
    }
}
