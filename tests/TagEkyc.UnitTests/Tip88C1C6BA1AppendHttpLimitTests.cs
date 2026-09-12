using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using TagEkyc.Api;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.CaptureRuntime;

namespace TagEkyc.UnitTests;

public sealed class Tip88C1C6BA1AppendHttpLimitTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SixteenKiBLimit_AcceptsBoundary_RejectsNextByteBeforeAuthentication(bool evidence)
    {
        var builder=WebApplication.CreateBuilder(); builder.WebHost.UseTestServer();
        var auth=new Auth(); builder.Services.AddSingleton<ICaptureRuntimeRequestAuthenticator>(auth);
        // No application gateway/DB is registered: this proof must end at auth or earlier.
        await using var app=builder.Build(); app.MapCaptureRuntimeExecutionEndpoints(); await app.StartAsync();
        var id=Guid.NewGuid().ToString("N");
        var path=evidence ? $"/api/ekyc/verification-sessions/{id}/evidence-results"
            : $"/api/ekyc/capture-runtime/executions/{id}/capture-artifacts";
        var payload=evidence ? "\"ResultType\":\"CaptureQuality\",\"InputCaptureArtifactIds\":[],\"Result\":\"Passed\",\"ReasonCodes\":[],\"PayloadSignatureStatus\":\"PlaceholderUnverified\",\"EngineName\":\"test\",\"EngineVersion\":\"1\""
            : "\"ArtifactType\":\"DeviceCaptureMetadata\",\"CaptureSource\":\"PcAgent\"";
        var json=$"{{\"BindingId\":\"{id}\",\"Payload\":{{{payload}}}}}";
        async Task<HttpStatusCode> Send(int length)
        {
            var body=Encoding.UTF8.GetBytes(json.PadRight(length));
            using var request=new HttpRequestMessage(HttpMethod.Post,path) { Content=new ByteArrayContent(body) };
            request.Content.Headers.ContentType=new("application/json");
            request.Headers.Add("Idempotency-Key",Guid.NewGuid().ToString("N"));
            request.Headers.Add(CaptureRuntimeCrt1RequestParser.CredentialIdHeader,Guid.NewGuid().ToString("N"));
            request.Headers.Add(CaptureRuntimeCrt1RequestParser.CredentialGenerationHeader,"1");
            request.Headers.Add(CaptureRuntimeCrt1RequestParser.TimestampHeader,DateTimeOffset.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'",CultureInfo.InvariantCulture));
            request.Headers.Add(CaptureRuntimeCrt1RequestParser.NonceHeader,new string('A',43));
            request.Headers.Add(CaptureRuntimeCrt1RequestParser.SignatureHeader,new string('A',86));
            using var response=await app.GetTestClient().SendAsync(request); return response.StatusCode;
        }
        Assert.Equal(HttpStatusCode.Forbidden,await Send(16384)); Assert.Equal(1,auth.Calls);
        Assert.Equal(HttpStatusCode.BadRequest,await Send(16385)); Assert.Equal(1,auth.Calls);
    }
    private sealed class Auth : ICaptureRuntimeRequestAuthenticator
    {
        public int Calls;
        public Task<SessionOperationResult<AuthenticatedCaptureRuntimeContext>> AuthenticateAsync(CaptureRuntimeSignedRequest request,
            CancellationToken cancellationToken=default)
        { Calls++; return Task.FromResult(SessionOperationResult<AuthenticatedCaptureRuntimeContext>.Failure("ACCESS_DENIED","Denied",403)); }
    }
}
