using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using TagEkyc.Api;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.CaptureRuntime;

namespace TagEkyc.UnitTests;

// HTTP boundary only. Cryptography and PostgreSQL are covered by separate real-provider proofs.
public sealed class Tip88C1C6BA1EnrollmentHttpTests
{
    private const string Id = "10000000000040008000000000000001";
    private const string Route = "/api/ekyc/capture-runtime/enrollments/redeem";

    [Theory]
    [InlineData("success", 201, 1)]
    [InlineData("replay", 200, 1)]
    [InlineData("auth-denial", 403, 1)]
    [InlineData("conflict", 409, 1)]
    [InlineData("missing-dependency", 503, 0)]
    [InlineData("throwing-dependency", 503, 1)]
    [InlineData("malformed", 400, 0)]
    [InlineData("missing-member", 400, 0)]
    [InlineData("unknown-member", 400, 0)]
    [InlineData("missing-idempotency", 400, 0)]
    [InlineData("mixed-client", 403, 0)]
    [InlineData("mixed-platform", 403, 0)]
    [InlineData("mixed-runtime", 403, 0)]
    public async Task R05_ActualHttpBoundary_ClosedMatrix(string scenario, int status, int calls)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var service = new Enrollment(scenario);
        if (scenario != "missing-dependency") builder.Services.AddSingleton<ICaptureRuntimeEnrollmentService>(service);
        await using var app = builder.Build();
        app.MapCaptureRuntimeManagementEndpoints();
        await app.StartAsync();
        using var client = app.GetTestClient();
        var json = new JsonObject
        {
            ["BootstrapIssuanceId"] = Id, ["BootstrapSecret"] = "synthetic-secret-not-a-crypto-proof",
            ["CandidateKeyId"] = Id, ["PublicVerifierSpki"] = "synthetic-spki",
            ["PublicKeyThumbprint"] = "synthetic-thumbprint", ["SignedAtUtc"] = "2026-09-12T00:00:00.0000000Z",
            ["Nonce"] = "synthetic-nonce", ["CandidateProof"] = "synthetic-proof"
        };
        if (scenario == "missing-member") json.Remove("CandidateProof");
        if (scenario == "unknown-member") json["ClientApplicationId"] = Id;
        var bytes = Encoding.UTF8.GetBytes(scenario == "malformed" ? "{" : json.ToJsonString());
        using var request = new HttpRequestMessage(HttpMethod.Post, Route);
        if (scenario != "missing-idempotency") request.Headers.Add("Idempotency-Key", Id);
        if (scenario == "mixed-client") request.Headers.Add("X-TagEkyc-Api-Key", "synthetic");
        if (scenario == "mixed-platform") request.Headers.Add("X-TagEkyc-Platform-Operator-Key", "synthetic");
        if (scenario == "mixed-runtime") request.Headers.Add("X-TagEkyc-Capture-Runtime-Signature", "synthetic");
        request.Content = new ByteArrayContent(bytes);
        request.Content.Headers.ContentType = new("application/json");
        using var response = await client.SendAsync(request);
        Assert.Equal((HttpStatusCode)status, response.StatusCode);
        Assert.Equal(calls, service.Calls);
        var wire = await response.Content.ReadAsStringAsync();
        var result = JsonNode.Parse(wire)!.AsObject();
        if (calls == 1)
        {
            Assert.Equal(bytes, service.ExactBytes);
            Assert.Equal(Guid.ParseExact(Id, "N"), service.Key);
            Assert.Equal(Guid.ParseExact(Id, "N"), service.Request!.BootstrapIssuanceId);
        }
        Assert.DoesNotContain("synthetic-secret", wire, StringComparison.Ordinal);
        Assert.DoesNotContain("private-provider-detail", wire, StringComparison.Ordinal);
        if (status < 300)
        {
            Assert.Equal(7, result.Count);
            Assert.Equal(Id, result["CaptureAgentId"]!.GetValue<string>());
            Assert.Equal(Id, result["DeviceInstallationId"]!.GetValue<string>());
            Assert.Equal(Id, result["CredentialId"]!.GetValue<string>());
            Assert.Equal(1, result["Generation"]!.GetValue<int>());
            Assert.Equal(2, result["RuntimeRevision"]!.GetValue<int>());
            Assert.Equal(3, result["InstallationRevision"]!.GetValue<int>());
            Assert.Equal(4, result["CredentialRevision"]!.GetValue<int>());
            Assert.True(response.Headers.CacheControl?.NoStore);
        }
        else
        {
            Assert.Equal(new[] { "code", "correlationId" }, result.Select(x => x.Key).OrderBy(x => x, StringComparer.Ordinal));
            Assert.False(string.IsNullOrEmpty(result["correlationId"]!.GetValue<string>()));
            var code = status switch { 400 => "REQUEST_INVALID", 403 => "ACCESS_DENIED", 409 => "CONFLICT", _ => "NOT_READY" };
            Assert.Equal(code, result["code"]!.GetValue<string>());
        }
    }

    private sealed class Enrollment(string scenario) : ICaptureRuntimeEnrollmentService
    {
        public int Calls;
        public byte[]? ExactBytes;
        public Guid Key;
        public CaptureRuntimeEnrollmentRedeemRequest? Request;
        public Task<SessionOperationResult<CaptureRuntimeEnrollmentResponse>> RedeemAsync(
            CaptureRuntimeEnrollmentRedeemRequest request, Guid idempotencyKey,
            ReadOnlyMemory<byte> exactRequestBody, CancellationToken cancellationToken = default)
        {
            Calls++; ExactBytes = exactRequestBody.ToArray(); Key = idempotencyKey; Request = request;
            if (scenario == "throwing-dependency") throw new InvalidOperationException("private-provider-detail");
            if (scenario is "auth-denial" or "conflict")
                return Task.FromResult(SessionOperationResult<CaptureRuntimeEnrollmentResponse>.Failure(
                    scenario == "conflict" ? "CONFLICT" : "ACCESS_DENIED", "private-provider-detail", scenario == "conflict" ? 409 : 403));
            var id = Guid.ParseExact(Id, "N");
            return Task.FromResult(SessionOperationResult<CaptureRuntimeEnrollmentResponse>.Success(
                new(id, id, id, 1, 2, 3, 4), scenario == "replay"));
        }
    }
}
