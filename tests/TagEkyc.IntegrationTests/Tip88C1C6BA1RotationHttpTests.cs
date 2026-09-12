using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using TagEkyc.Api;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.CaptureRuntime;

namespace TagEkyc.IntegrationTests;

public sealed class Tip88C1C6BA1RotationHttpTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RotationHttp_OneTypedCallReceivesExactBytesAndEmptyBinding(bool replay)
    {
        using var fixture=new RequestFixture();
        var service=new Service(fixture.Signer) {Replay=replay};
        await using var app=await Start(service);
        using var response=await app.GetTestClient().SendAsync(fixture.Request());
        Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        Assert.Equal(1,service.Calls);
        Assert.Equal(fixture.Rotation,service.Rotation);
        Assert.Equal(fixture.Operation,service.Operation);
        Assert.Equal(fixture.Body,service.Body);
        Assert.Equal(fixture.Preimage,service.Preimage);
        Assert.EndsWith("\n\n",Encoding.UTF8.GetString(service.Preimage!));
        Assert.Equal(12,Encoding.UTF8.GetString(service.Preimage!).Split('\n').Length);
        Assert.Equal("CredentialRotation",service.Signed!.RequiredRole);
        using var responseJson=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(new[] { "CandidateKeyId","CredentialId","CredentialRevision","Generation",
                "InstallationRevision","PublicKeyThumbprint","RotationRevision" },
            responseJson.RootElement.EnumerateObject().Select(p=>p.Name).Order(StringComparer.Ordinal).ToArray());
        Assert.Equal(fixture.Credential.ToString("N"),responseJson.RootElement.GetProperty("CredentialId").GetString());
        Assert.Equal(fixture.Candidate.ToString("N"),responseJson.RootElement.GetProperty("CandidateKeyId").GetString());
    }

    [Theory]
    [InlineData("mixed-client",403)]
    [InlineData("mixed-platform",403)]
    [InlineData("bad-key",400)]
    [InlineData("duplicate-key",400)]
    [InlineData("unknown-member",400)]
    [InlineData("duplicate-member",400)]
    [InlineData("noncanonical-uuid",400)]
    [InlineData("missing-crt-signature",400)]
    [InlineData("bad-timestamp",400)]
    [InlineData("query",400)]
    [InlineData("bad-route",400)]
    [InlineData("trailing-json",400)]
    public async Task RotationHttp_InvalidEnvelopeNeverCallsTypedService(string mutation,int status)
    {
        using var fixture=new RequestFixture();
        var service=new Service(fixture.Signer);
        await using var app=await Start(service);
        using var request=fixture.Request();
        switch(mutation)
        {
            case "mixed-client": request.Headers.Add("X-TagEkyc-Api-Key","not-an-accepted-runtime-credential");break;
            case "mixed-platform": request.Headers.Add("X-TagEkyc-Platform-Operator-Key","teo_notaccepted");break;
            case "bad-key": request.Headers.Remove("Idempotency-Key");request.Headers.Add("Idempotency-Key","x");break;
            case "duplicate-key": request.Headers.Add("Idempotency-Key",Guid.NewGuid().ToString("N"));break;
            case "unknown-member": SetBody(request,Encoding.UTF8.GetString(fixture.Body)[..^1]+",\"ClientApplicationId\":\"no\"}");break;
            case "duplicate-member": SetBody(request,Encoding.UTF8.GetString(fixture.Body)[..^1]+",\"CandidateKeyId\":\""+Guid.NewGuid().ToString("N")+"\"}");break;
            case "noncanonical-uuid": SetBody(request,Encoding.UTF8.GetString(fixture.Body).Replace(fixture.Candidate.ToString("N"),fixture.Candidate.ToString("D")));break;
            case "missing-crt-signature": request.Headers.Remove(CaptureRuntimeCrt1RequestParser.SignatureHeader);break;
            case "bad-timestamp": request.Headers.Remove(CaptureRuntimeCrt1RequestParser.TimestampHeader);request.Headers.Add(CaptureRuntimeCrt1RequestParser.TimestampHeader,"2026-09-11T00:00:00Z");break;
            case "query": request.RequestUri=new Uri(fixture.Path+"?mode=successor",UriKind.Relative);break;
            case "bad-route": request.RequestUri=new Uri(fixture.Path.Replace(fixture.Rotation.ToString("N"),"not-a-uuid"),UriKind.Relative);break;
            case "trailing-json": SetBody(request,Encoding.UTF8.GetString(fixture.Body)+"{}");break;
        }
        using var response=await app.GetTestClient().SendAsync(request);
        Assert.Equal(status,(int)response.StatusCode);
        Assert.Equal(0,service.Calls);
        using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(new[]{"code","correlationId"},json.RootElement.EnumerateObject().Select(p=>p.Name).Order().ToArray());
    }

    [Fact]
    public async Task RotationHttp_ChangedExactBodyFailsSignature_NotFallback()
    {
        using var fixture=new RequestFixture();
        var service=new Service(fixture.Signer);
        await using var app=await Start(service);
        using var request=fixture.Request();
        SetBody(request,Encoding.UTF8.GetString(fixture.Body)+" ");
        using var response=await app.GetTestClient().SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden,response.StatusCode);
        Assert.Equal(1,service.Calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RotationHttp_MissingOrFailedDependency_IsClosed503(bool throws)
    {
        using var fixture=new RequestFixture();
        var service=throws?new Service(fixture.Signer) {Throw=true}:null;
        await using var app=await Start(service);
        using var response=await app.GetTestClient().SendAsync(fixture.Request());
        Assert.Equal(HttpStatusCode.ServiceUnavailable,response.StatusCode);
        var raw=await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("dependency-secret",raw);
        using var json=JsonDocument.Parse(raw);
        Assert.Equal("NOT_READY",json.RootElement.GetProperty("code").GetString());
        Assert.Equal(new[]{"code","correlationId"},json.RootElement.EnumerateObject().Select(p=>p.Name).Order().ToArray());
    }

    private static async Task<WebApplication> Start(Service? service)
    {
        var builder=WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        if(service is not null) builder.Services.AddSingleton<ICaptureRuntimeRotationService>(service);
        // No ordinary authenticator is registered. A double-authentication endpoint
        // would fail instead of reaching the sole typed R13 service.
        var app=builder.Build();
        app.MapCaptureRuntimeRotationEndpoints();
        await app.StartAsync();
        return app;
    }
    private static void SetBody(HttpRequestMessage request,string body)
    {
        request.Content?.Dispose();
        request.Content=new ByteArrayContent(Encoding.UTF8.GetBytes(body));
        request.Content.Headers.ContentType=new MediaTypeHeaderValue("application/json");
    }
    private static string Url(byte[] bytes)=>Convert.ToBase64String(bytes).TrimEnd('=').Replace('+','-').Replace('/','_');
    private sealed class RequestFixture:IDisposable
    {
        public ECDsa Signer {get;}=ECDsa.Create(ECCurve.NamedCurves.nistP256);
        public Guid Rotation {get;}=Guid.NewGuid();
        public Guid Credential {get;}=Guid.NewGuid();
        public Guid Candidate {get;}=Guid.NewGuid();
        public Guid Operation {get;}=Guid.NewGuid();
        public string Path=>$"/api/ekyc/capture-runtime/credential-rotations/{Rotation:N}/complete";
        public byte[] Body {get;}
        public byte[] Preimage {get;}
        private readonly Dictionary<string,string> headers;
        public RequestFixture()
        {
            var nonce=Url(RandomNumberGenerator.GetBytes(32));
            var timestamp=DateTimeOffset.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'",CultureInfo.InvariantCulture);
            Body=JsonSerializer.SerializeToUtf8Bytes(new {CandidateKeyId=Candidate.ToString("N"),
                SuccessorPublicVerifierSpki=Url(Signer.ExportSubjectPublicKeyInfo()),
                SuccessorPublicKeyThumbprint=Convert.ToHexString(SHA256.HashData(Signer.ExportSubjectPublicKeyInfo())).ToLowerInvariant(),
                SuccessorProof=Url(new byte[64])});
            Preimage=Encoding.UTF8.GetBytes(string.Join('\n',"TAG-EKYC-CRT1","POST",Path,Credential.ToString("N"),"1",
                timestamp,nonce,"application/json",Body.Length.ToString(CultureInfo.InvariantCulture),
                Convert.ToHexString(SHA256.HashData(Body)).ToLowerInvariant(),string.Empty)+"\n");
            headers=new()
            {
                ["Idempotency-Key"]=Operation.ToString("N"),
                [CaptureRuntimeCrt1RequestParser.CredentialIdHeader]=Credential.ToString("N"),
                [CaptureRuntimeCrt1RequestParser.CredentialGenerationHeader]="1",
                [CaptureRuntimeCrt1RequestParser.TimestampHeader]=timestamp,
                [CaptureRuntimeCrt1RequestParser.NonceHeader]=nonce,
                [CaptureRuntimeCrt1RequestParser.SignatureHeader]=Url(Signer.SignData(Preimage,HashAlgorithmName.SHA256,DSASignatureFormat.IeeeP1363FixedFieldConcatenation))
            };
        }
        public HttpRequestMessage Request()
        {
            var request=new HttpRequestMessage(HttpMethod.Post,Path) {Content=new ByteArrayContent(Body)};
            request.Content.Headers.ContentType=new MediaTypeHeaderValue("application/json");
            foreach(var item in headers) request.Headers.Add(item.Key,item.Value);
            return request;
        }
        public void Dispose()=>Signer.Dispose();
    }
    private sealed class Service(ECDsa verifier):ICaptureRuntimeRotationService
    {
        public bool Replay {get;init;}
        public bool Throw {get;init;}
        public int Calls {get;private set;}
        public Guid Rotation {get;private set;}
        public Guid Operation {get;private set;}
        public byte[]? Body {get;private set;}
        public byte[]? Preimage {get;private set;}
        public CaptureRuntimeSignedRequest? Signed {get;private set;}
        public Task<SessionOperationResult<CaptureRuntimeRotationCompletionResponse>> CompleteRotationAsync(
            CaptureRuntimeSignedRequest signed,Guid rotation,CaptureRuntimeRotationCompleteRequest request,
            Guid key,ReadOnlyMemory<byte> body,CancellationToken ct=default)
        {
            Calls++;Rotation=rotation;Operation=key;Body=body.ToArray();Preimage=signed.ExactSignedPreimage.ToArray();Signed=signed;
            if(Throw) throw new InvalidOperationException("dependency-secret");
            if(!verifier.VerifyData(Preimage,signed.Signature,HashAlgorithmName.SHA256,DSASignatureFormat.IeeeP1363FixedFieldConcatenation))
                return Task.FromResult(SessionOperationResult<CaptureRuntimeRotationCompletionResponse>.Failure("ACCESS_DENIED","Denied",403));
            return Task.FromResult(SessionOperationResult<CaptureRuntimeRotationCompletionResponse>.Success(new(
                signed.CredentialId,2,request.CandidateKeyId,request.SuccessorPublicKeyThumbprint,1,3,2),isReplay:Replay));
        }
    }
}
