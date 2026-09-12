using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using TagEkyc.Api;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.CaptureRuntime;

namespace TagEkyc.UnitTests;

// Handler-contract evidence only: real routing, closed parser and HTTP mapper;
// deliberately fake typed services/authenticator. PostgreSQL/crypto/business
// behavior is proved separately, never inferred from this matrix.
public sealed class Tip88C1C6BA1ManagementHttpContractTests
{
    private static readonly Guid Id = Guid.ParseExact("10000000000040008000000000000001", "N");
    private static readonly DateTimeOffset At = new(2026, 9, 12, 0, 0, 0, TimeSpan.Zero);
    private const string Prefix = "/api/ekyc/operator/capture-runtimes/";
    private const string ControlPrefix = "/api/ekyc/operator/capture-runtime-control/";
    private static readonly JsonSerializerOptions Wire = new()
    { Converters = { new GuidWire(), new TimeWire() } };

    public static IEnumerable<object[]> Routes()
    {
        yield return Row("R03", Prefix + "bootstrap-issuances", "IssueBootstrapAsync", 201,
            new CaptureRuntimeBootstrapIssueRequest("Managed", Id, 1, Id, 1, Id, 1, At, new string('a', 64)),
            new CaptureRuntimeBootstrapIssueResponse(Id, "synthetic-secret-once", At, 1));
        yield return Row("R04", Prefix + "bootstrap-issuances/revoke", "RevokeBootstrapAsync", 200,
            new CaptureRuntimeBootstrapRevokeRequest(Id, 1, "OperatorRevocation"), new CaptureRuntimeBootstrapLifecycleResponse(Id, "Revoked", 2, At));
        foreach (var (operation, suffix, method, state, reason) in new[] {
            ("R06", "suspend", "SuspendRuntimeAsync", "Suspended", "OperatorSuspension"), ("R07", "reactivate", "ReactivateRuntimeAsync", "Active", "OperatorReactivation"),
            ("R08", "revoke", "RevokeRuntimeAsync", "Revoked", "OperatorRevocation"), ("R09", "retire", "RetireRuntimeAsync", "Retired", "OperatorRetirement") })
            yield return Row(operation, Prefix + suffix, method, 200, new RuntimeLifecycleRequest(Id, 1, reason),
                new CaptureRuntimeLifecycleResponse(Id, state, 2, At));
        yield return Row("R10", Prefix + "credentials/revoke", "RevokeCredentialAsync", 200,
            new CaptureRuntimeCredentialRevokeRequest(Id, Id, Id, 1, 1, "CredentialCompromise"), new CaptureRuntimeCredentialResponse(Id, 1, "Revoked", 2, At));
        yield return Row("R11", Prefix + "credential-rotations/authorize", "AuthorizeRotationAsync", 201,
            new CaptureRuntimeRotationAuthorizeRequest(Id, Id, Id, 1, 1, At), new CaptureRuntimeRotationResponse(Id, Id, Id, Id, 1, "Active", 1, At));
        yield return Row("R12", Prefix + "credential-rotations/revoke", "RevokeRotationAsync", 200,
            new CaptureRuntimeRotationRevokeRequest(Id, 1, "OperatorRevocation"), new CaptureRuntimeRotationLifecycleResponse(Id, "Revoked", 2, At));
        yield return Row("R14", ControlPrefix + "trust-profiles/publish", "PublishTrustProfileAsync", 201,
            new CaptureRuntimeTrustProfilePublicationRequest(Id, 0, At, At.AddDays(1), "Managed", false, false, false), new CaptureRuntimeCatalogPublicationResponse(Id, 1, 1));
        yield return Row("R15", ControlPrefix + "role-policies/publish", "PublishRolePolicyAsync", 201,
            new CaptureRuntimeRolePolicyPublicationRequest(Id, 0, At, new[] { "Configuration" }), new CaptureRuntimeCatalogPublicationResponse(Id, 1, 1));
        yield return Row("R16", ControlPrefix + "configurations/publish", "PublishConfigurationAsync", 201,
            new CaptureRuntimeConfigurationPublicationRequest(Id, 0, At, At.AddDays(1), false, 60, 100, 1, 1024, 1024, 4096, 1024, 4096, 1024),
            new CaptureRuntimeCatalogPublicationResponse(Id, 1, 1));
        yield return Row("R17", Prefix + "role-policies/assign", "AssignRolePolicyAsync", 200,
            new CaptureRuntimeRolePolicyAssignmentRequest(Id, Id, 1, 1), new CaptureRuntimeRolePolicyAssignmentResponse(Id, Id, 1, 2));
        yield return Row("R18", Prefix + "configurations/assign", "AssignConfigurationAsync", 200,
            new CaptureRuntimeConfigurationAssignmentRequest(Id, Id, 1, 1, null), new CaptureRuntimeConfigurationAssignmentResponse(Id, Id, 1, null, 2));
        yield return Row("R19", Prefix + Id.ToString("N") + "/readiness", "ReadReadinessAsync", 200, null,
            new CaptureRuntimeReadinessResponse(Id, "Active", 1, "Active", 1, "Active", 1, 1, 1, 1, 1, new[] { 7 }, true, true, true, true, true));
    }
    private static object[] Row(string id, string path, string method, int status, object? request, object response) =>
        [id, path, method, status, request!, response];

    [Theory]
    [MemberData(nameof(Routes))]
    public async Task OperatorRoutes_ActualHandler_ClosedBranchesAndExactTypedSelection(
        string operation, string path, string method, int status, object? requestDto, object responseDto)
    {
        var get = operation == "R19";
        var bytes = requestDto is null ? Array.Empty<byte>() : JsonSerializer.SerializeToUtf8Bytes(requestDto, requestDto.GetType(), Wire);
        var scenarios = new List<string> { "success", "authentication", "mixed-auth", "authority", "resource", "dependency-result", "dependency-missing", "auth-missing", "dependency-throws", "invalid-wire" };
        if (!get) scenarios.AddRange(["replay", "conflict", "invalid-idempotency"]);
        foreach (var scenario in scenarios)
        {
            var builder = WebApplication.CreateBuilder(); builder.WebHost.UseTestServer();
            var auth = new Auth { Deny = scenario == "authentication" };
            if (scenario != "auth-missing") builder.Services.AddSingleton<IPlatformOperatorCredentialAuthenticator>(auth);
            var serviceType = method.StartsWith("Publish", StringComparison.Ordinal) ? typeof(ICaptureRuntimeControlService) : typeof(ICaptureRuntimeManagementService);
            var proxy = (TypedService)DispatchProxy.Create(serviceType, typeof(TypedService));
            proxy.ExpectedMethod = method; proxy.Response = responseDto; proxy.Scenario = scenario;
            if (scenario != "dependency-missing") builder.Services.AddSingleton(serviceType, proxy);
            await using var app = builder.Build(); app.MapCaptureRuntimeManagementEndpoints(); await app.StartAsync();
            using var client = app.GetTestClient();
            using var message = new HttpRequestMessage(get ? HttpMethod.Get : HttpMethod.Post,
                scenario == "invalid-wire" && get ? path + "?unexpected=1" : path);
            message.Headers.Add("X-TagEkyc-Platform-Operator-Key", "synthetic-platform-credential");
            if (scenario == "mixed-auth") message.Headers.Add("X-TagEkyc-Api-Key", "forbidden-client-key");
            if (!get)
            {
                message.Headers.Add("Idempotency-Key", scenario == "invalid-idempotency" ? "invalid" : Id.ToString("N"));
                message.Content = new ByteArrayContent(scenario == "invalid-wire" ? "{}"u8.ToArray() : bytes);
                message.Content.Headers.ContentType = new("application/json");
            }
            using var response = await client.SendAsync(message);
            var actual = await response.Content.ReadAsStringAsync();
            var expectedStatus = scenario switch
            {
                "success" => status, "replay" => operation == "R03" ? 409 : 200,
                "authentication" or "mixed-auth" or "authority" => 403,
                "conflict" => 409,
                "resource" => 404,
                "invalid-wire" or "invalid-idempotency" => 400,
                _ => 503
            };
            Assert.True((int)response.StatusCode == expectedStatus, $"{operation}/{scenario}: {(int)response.StatusCode}: {actual}");
            var reachesService = scenario is "success" or "replay" or "authority" or "resource" or "conflict" or "dependency-result" or "dependency-throws";
            Assert.Equal(reachesService ? 1 : 0, proxy.Calls);
            Assert.Equal(scenario is "mixed-auth" or "auth-missing" ? 0 : 1, auth.Calls);
            if (reachesService)
            {
                Assert.Same(auth.Actor, proxy.Arguments![0]);
                if (get) Assert.Equal(Id, proxy.Arguments[1]);
                else
                {
                    Assert.Equal(requestDto!.GetType(), proxy.Arguments[1]!.GetType());
                    Assert.Equal(bytes, JsonSerializer.SerializeToUtf8Bytes(proxy.Arguments[1], requestDto.GetType(), Wire));
                    Assert.Equal(Id, proxy.Arguments[2]);
                    Assert.Equal(bytes, proxy.BodySnapshot);
                }
            }
            if (scenario == "success" || (scenario == "replay" && operation != "R03"))
            {
                var expectedDto = responseDto;
                if (scenario == "replay" && responseDto is CaptureRuntimeBootstrapIssueResponse bootstrap)
                    expectedDto = bootstrap with { BootstrapSecret = null };
                Assert.Equal(JsonSerializer.Serialize(expectedDto, expectedDto.GetType(), Wire), actual);
                Assert.Contains("no-store", response.Headers.CacheControl!.ToString());
            }
            else
            {
                using var json = JsonDocument.Parse(actual);
                Assert.Equal(new[] { "code", "correlationId" }, json.RootElement.EnumerateObject().Select(p => p.Name).OrderBy(p => p));
                var expectedCode = scenario == "replay" && operation == "R03" ? "EXISTING_MATCH_SECRET_UNAVAILABLE"
                    : expectedStatus switch { 400 => "REQUEST_INVALID", 403 => "ACCESS_DENIED", 404 => "RESOURCE_NOT_AVAILABLE", 409 => "CONFLICT", _ => "NOT_READY" };
                Assert.Equal(expectedCode, json.RootElement.GetProperty("code").GetString());
                Assert.False(string.IsNullOrEmpty(json.RootElement.GetProperty("correlationId").GetString()));
                Assert.DoesNotContain("synthetic-sensitive-detail", actual);
                Assert.DoesNotContain("synthetic-secret-once", actual);
                Assert.DoesNotContain("synthetic-platform-credential", actual);
            }
        }
    }

    public class TypedService : DispatchProxy
    {
        public string ExpectedMethod = "", Scenario = "";
        public object Response = null!;
        public object?[]? Arguments;
        public byte[]? BodySnapshot;
        public int Calls;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            Calls++; Assert.Equal(ExpectedMethod, targetMethod!.Name); Arguments = args;
            if (args!.Length == 5) BodySnapshot = ((ReadOnlyMemory<byte>)args[3]!).ToArray();
            if (Scenario == "dependency-throws") throw new InvalidOperationException("synthetic-sensitive-detail");
            return typeof(TypedService).GetMethod(nameof(Result), BindingFlags.Instance | BindingFlags.NonPublic)!
                .MakeGenericMethod(targetMethod.ReturnType.GenericTypeArguments[0].GenericTypeArguments[0]).Invoke(this, null);
        }
        private Task<SessionOperationResult<T>> Result<T>()
        {
            if (Scenario == "replay" && Response is CaptureRuntimeBootstrapIssueResponse)
                return Task.FromResult(SessionOperationResult<T>.Failure("EXISTING_MATCH_SECRET_UNAVAILABLE", "synthetic-sensitive-detail", 409));
            if (Scenario is "authority" or "resource" or "conflict" or "dependency-result")
            {
                var status = Scenario == "authority" ? 403 : Scenario == "resource" ? 404 : Scenario == "conflict" ? 409 : 503;
                return Task.FromResult(SessionOperationResult<T>.Failure(status == 403 ? "ACCESS_DENIED" : status == 404 ? "RESOURCE_NOT_AVAILABLE" : status == 409 ? "CONFLICT" : "NOT_READY", "synthetic-sensitive-detail", status));
            }
            var value = Response;
            if (Scenario == "replay" && value is CaptureRuntimeBootstrapIssueResponse bootstrap)
                value = bootstrap with { BootstrapSecret = null };
            return Task.FromResult(SessionOperationResult<T>.Success((T)value, isReplay: Scenario == "replay"));
        }
    }
    private sealed class Auth : IPlatformOperatorCredentialAuthenticator
    {
        public bool Deny; public int Calls;
        public readonly AuthenticatedPlatformOperatorContext Actor = new(Id, Id, "OperatorAdmin", new HashSet<string> { "operator.capture-runtime.manage" }, "teo_test", 1, At);
        public Task<SessionOperationResult<AuthenticatedPlatformOperatorContext>> AuthenticateAsync(string? key, CancellationToken ct = default)
        {
            Calls++;
            return Task.FromResult(Deny ? SessionOperationResult<AuthenticatedPlatformOperatorContext>.Failure("ACCESS_DENIED", "synthetic-sensitive-detail", 403)
                : SessionOperationResult<AuthenticatedPlatformOperatorContext>.Success(Actor));
        }
    }
    private sealed class GuidWire : JsonConverter<Guid>
    {
        public override Guid Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) => throw new NotSupportedException();
        public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString("N"));
    }
    private sealed class TimeWire : JsonConverter<DateTimeOffset>
    {
        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) => throw new NotSupportedException();
        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) => writer.WriteStringValue(value.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", System.Globalization.CultureInfo.InvariantCulture));
    }
}
