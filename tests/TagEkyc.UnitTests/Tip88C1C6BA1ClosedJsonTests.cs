using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using TagEkyc.Api;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.CaptureRuntime;

namespace TagEkyc.UnitTests;

public sealed class Tip88C1C6BA1ClosedJsonTests
{
    private const string Id = "10000000000040008000000000000001";
    private const string Time = "2026-09-12T00:00:00.0000000Z";
    private static JsonObject Trust() => new()
    {
        ["CatalogId"] = Id, ["ExpectedHeadRevision"] = 0, ["EffectiveAtUtc"] = Time,
        ["ExpiresAtUtc"] = "2026-09-13T00:00:00.0000000Z", ["RuntimeType"] = "Managed",
        ["RetainedRawEnabled"] = false, ["AllowTrustedEvidence"] = false, ["RequireHandoffAttestation"] = false
    };

    [Fact]
    public async Task EveryRequiredTrustMember_MissingOrNull_Is400BeforeTypedService()
    {
        var builder = WebApplication.CreateBuilder(); builder.WebHost.UseTestServer();
        var service = new Control();
        builder.Services.AddSingleton<IPlatformOperatorCredentialAuthenticator>(new Auth());
        builder.Services.AddSingleton<ICaptureRuntimeControlService>(service);
        await using var app = builder.Build(); app.MapCaptureRuntimeManagementEndpoints(); await app.StartAsync();
        using var client = app.GetTestClient();
        async Task<HttpStatusCode> Send(JsonObject json)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/ekyc/operator/capture-runtime-control/trust-profiles/publish");
            request.Headers.Add("X-TagEkyc-Platform-Operator-Key", "synthetic");
            request.Headers.Add("Idempotency-Key", Id);
            request.Content = new ByteArrayContent(Encoding.UTF8.GetBytes(json.ToJsonString()));
            request.Content.Headers.ContentType = new("application/json");
            using var response = await client.SendAsync(request); return response.StatusCode;
        }
        Assert.Equal(HttpStatusCode.Created, await Send(Trust())); Assert.Equal(1, service.Calls);
        foreach (var property in Trust().Select(p => p.Key))
        {
            var missing = Trust(); missing.Remove(property);
            Assert.Equal(HttpStatusCode.BadRequest, await Send(missing));
            var nulled = Trust(); nulled[property] = null;
            Assert.Equal(HttpStatusCode.BadRequest, await Send(nulled));
            Assert.Equal(1, service.Calls);
        }
    }

    [Fact]
    public async Task NullableOverrideAndIssueUnionRemainOptional_RequiredNestedEnumsDoNotDefault()
    {
        var assignment = new JsonObject { ["AgentId"] = Id, ["ConfigurationId"] = Id,
            ["ConfigurationRevision"] = 1, ["ExpectedRuntimeRevision"] = 1 };
        Assert.True(await Parse(typeof(CaptureRuntimeConfigurationAssignmentRequest), assignment));
        assignment["OverrideId"] = null;
        Assert.True(await Parse(typeof(CaptureRuntimeConfigurationAssignmentRequest), assignment));
        Assert.True(await Parse(typeof(CaptureCapabilityRequest), new JsonObject { ["Action"] = "Issue" }));
        var capture = new JsonObject { ["BindingId"] = Id, ["Payload"] = new JsonObject {
            ["ArtifactType"] = "DeviceCaptureMetadata", ["CaptureSource"] = "PcAgent" } };
        Assert.True(await Parse(typeof(CaptureRuntimeCaptureArtifactRequest), capture));
        ((JsonObject)capture["Payload"]!).Remove("ArtifactType");
        Assert.False(await Parse(typeof(CaptureRuntimeCaptureArtifactRequest), capture));
    }

    [Fact]
    public async Task ZeroHeadRevisionAndFalseEnabledRequireExplicitPresence()
    {
        var role = new JsonObject { ["CatalogId"] = Id, ["ExpectedHeadRevision"] = 0,
            ["EffectiveAtUtc"] = Time, ["Roles"] = new JsonArray("Bind") };
        Assert.True(await Parse(typeof(CaptureRuntimeRolePolicyPublicationRequest), role));
        role.Remove("ExpectedHeadRevision");
        Assert.False(await Parse(typeof(CaptureRuntimeRolePolicyPublicationRequest), role));
        var configuration = new JsonObject { ["CatalogId"] = Id, ["ExpectedHeadRevision"] = 0,
            ["EffectiveAtUtc"] = Time, ["ExpiresAtUtc"] = "2026-09-13T00:00:00.0000000Z",
            ["RawExportEnabled"] = false, ["PlaintextBudgetSeconds"] = 60,
            ["RawExportSourceClaimSafetyMarginMilliseconds"] = 0, ["CaptureAgentConfigurationPollingIntervalSeconds"] = 1,
            ["RawExportSourceMaximumChipDg2PortraitBytes"] = 1, ["RawExportSourceMaximumLiveSelfieImageBytes"] = 1,
            ["RawExportCaptureMaximumAggregatePlaintextBytesPerHost"] = 1,
            ["RawExportCustodyMaximumPlaintextWindowBytesPerStream"] = 1,
            ["RawExportCustodyMaximumAggregatePlaintextWindowBytesPerDeployment"] = 1,
            ["RawExportIngressMaximumPreAdmissionBufferedBytes"] = 1 };
        Assert.True(await Parse(typeof(CaptureRuntimeConfigurationPublicationRequest), configuration));
        configuration.Remove("RawExportEnabled");
        Assert.False(await Parse(typeof(CaptureRuntimeConfigurationPublicationRequest), configuration));
    }

    private static async Task<bool> Parse(Type dto, JsonObject json)
    {
        var bytes = Encoding.UTF8.GetBytes(json.ToJsonString());
        var request = new DefaultHttpContext().Request; request.ContentType = "application/json";
        request.ContentLength = bytes.Length; request.Body = new MemoryStream(bytes);
        var method = typeof(CaptureRuntimeHttpRoutes).GetMethod("ReadBodyAsync", BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(dto);
        var task = (Task)method.Invoke(null, [request, 65536, CancellationToken.None])!;
        await task;
        var parsed = task.GetType().GetProperty("Result")!.GetValue(task);
        (parsed as IDisposable)?.Dispose();
        return parsed is not null;
    }
    private sealed class Auth : IPlatformOperatorCredentialAuthenticator
    {
        public Task<SessionOperationResult<AuthenticatedPlatformOperatorContext>> AuthenticateAsync(string? presentedKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(SessionOperationResult<AuthenticatedPlatformOperatorContext>.Success(new(
                Guid.ParseExact(Id,"N"), Guid.NewGuid(), "OperatorAdmin", new HashSet<string> { "operator.capture-runtime.manage" }, "teo_test", 1, DateTimeOffset.UtcNow)));
    }
    private sealed class Control : ICaptureRuntimeControlService
    {
        public int Calls;
        public Task<SessionOperationResult<CaptureRuntimeCatalogPublicationResponse>> PublishTrustProfileAsync(AuthenticatedPlatformOperatorContext actor,
            CaptureRuntimeTrustProfilePublicationRequest request, Guid idempotencyKey, ReadOnlyMemory<byte> exactRequestBody, CancellationToken cancellationToken = default)
        { Calls++; return Task.FromResult(SessionOperationResult<CaptureRuntimeCatalogPublicationResponse>.Success(new(request.CatalogId,1,1))); }
        public Task<SessionOperationResult<CaptureRuntimeCatalogPublicationResponse>> PublishRolePolicyAsync(AuthenticatedPlatformOperatorContext actor,
            CaptureRuntimeRolePolicyPublicationRequest request, Guid idempotencyKey, ReadOnlyMemory<byte> exactRequestBody, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<SessionOperationResult<CaptureRuntimeCatalogPublicationResponse>> PublishConfigurationAsync(AuthenticatedPlatformOperatorContext actor,
            CaptureRuntimeConfigurationPublicationRequest request, Guid idempotencyKey, ReadOnlyMemory<byte> exactRequestBody, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
