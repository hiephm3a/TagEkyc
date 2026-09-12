using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using TagEkyc.Api;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.CaptureAgent;
using TagEkyc.Contracts.CaptureRuntime;

namespace TagEkyc.UnitTests;

public sealed class Tip88C1C6BA1ResponseWireTests
{
    [Fact]
    public async Task NewSecretResponseUsesCanonicalAtoms_LandedResponseKeepsHostContract()
    {
        var id=Guid.NewGuid(); var secret=new string('A',43);
        var expiry=new DateTimeOffset(2026,9,12,3,0,0,TimeSpan.FromHours(3));
        var bootstrap=new CaptureRuntimeBootstrapIssueResponse(id,secret,expiry,1);
        var landed=new CaptureArtifactSubmissionResponseDto(id.ToString("D"),id.ToString("D"),null,true,"Created","c");
        var builder=WebApplication.CreateBuilder(); builder.WebHost.UseTestServer();
        await using var app=builder.Build();
        app.MapGet("/new",(HttpContext c)=>Respond(c,SessionOperationResult<CaptureRuntimeBootstrapIssueResponse>.Success(bootstrap),201));
        app.MapGet("/replay",(HttpContext c)=>Respond(c,SessionOperationResult<CaptureRuntimeBootstrapIssueResponse>.Failure("EXISTING_MATCH_SECRET_UNAVAILABLE","Unavailable",409),201));
        app.MapGet("/landed",(HttpContext c)=>Respond(c,SessionOperationResult<CaptureArtifactSubmissionResponseDto>.Success(landed),200));
        await app.StartAsync(); using var client=app.GetTestClient();
        using var response=await client.GetAsync("/new"); Assert.Equal(201,(int)response.StatusCode);
        using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(id.ToString("N"),json.RootElement.GetProperty("BootstrapIssuanceId").GetString());
        Assert.Equal("2026-09-12T00:00:00.0000000Z",json.RootElement.GetProperty("ExpiresAtUtc").GetString());
        Assert.Equal(secret,json.RootElement.GetProperty("BootstrapSecret").GetString());
        using var replay=await client.GetAsync("/replay"); Assert.Equal(409,(int)replay.StatusCode);
        var replayBody=await replay.Content.ReadAsStringAsync(); Assert.DoesNotContain(secret,replayBody);
        using var replayJson=JsonDocument.Parse(replayBody);
        Assert.Equal(new[] {"code","correlationId"},replayJson.RootElement.EnumerateObject().Select(p=>p.Name).Order().ToArray());
        Assert.Equal(JsonSerializer.Serialize(landed,new JsonSerializerOptions(JsonSerializerDefaults.Web)),await client.GetStringAsync("/landed"));
    }
    private static IResult Respond<T>(HttpContext context,SessionOperationResult<T> result,int status) =>
        (IResult)typeof(CaptureRuntimeHttpRoutes).GetMethod("Respond",BindingFlags.Static|BindingFlags.NonPublic)!
            .MakeGenericMethod(typeof(T)).Invoke(null,[context,result,status])!;
}
