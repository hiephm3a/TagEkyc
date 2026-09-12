using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TagEkyc.Application.CaptureRuntime;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.CaptureRuntime;

namespace TagEkyc.UnitTests;

public sealed class Tip88C1C6BA1RotationTests
{
    [Fact]
    public async Task Rotation_PredecessorAndSuccessorSelectIdenticalDurableFingerprint()
    {
        using var fixture=new Fixture();
        var auth=new Auth(fixture.Actor);
        var gateway=new Gateway();
        var service=new CaptureRuntimeRotationApplicationService(auth,gateway);
        var first=await service.CompleteRotationAsync(fixture.Signed,fixture.Rotation,fixture.Request,fixture.Operation,fixture.Body);
        Assert.True(first.IsSuccess);
        var selected=gateway.Fingerprint!.ToArray();
        Assert.Equal(1,auth.Calls);
        Assert.Equal(1,gateway.Completes);
        Assert.Equal(0,gateway.Replays);
        Assert.Null(auth.LastSuccessor);
        auth.Branch=CaptureRuntimeRotationBranch.Successor;
        auth.Actor=fixture.Actor with { CredentialGeneration=2 };
        var second=await service.CompleteRotationAsync(fixture.Signed with { CredentialGeneration=2 },
            fixture.Rotation,fixture.Request,fixture.Operation,fixture.Body);
        Assert.True(second.IsSuccess);
        Assert.True(second.IsReplay);
        Assert.Equal(selected,gateway.Fingerprint);
        Assert.Equal(2,auth.Calls);
        Assert.Equal(1,gateway.Completes);
        Assert.Equal(1,gateway.Replays);
    }

    [Theory]
    [InlineData(1L,true,false)]
    [InlineData(2L,true,true)]
    [InlineData(long.MaxValue,false,true)]
    public async Task Rotation_UnrepresentableAlternateIsAbsentNotOverflow(long generation,bool predecessor,bool successor)
    {
        using var fixture=new Fixture();
        var auth=new Auth(fixture.Actor) { Deny=true };
        var service=new CaptureRuntimeRotationApplicationService(auth,new Gateway());
        var result=await service.CompleteRotationAsync(fixture.Signed with { CredentialGeneration=generation },
            fixture.Rotation,fixture.Request,fixture.Operation,fixture.Body);
        Assert.Equal(403,result.Error!.StatusCode);
        Assert.Equal(predecessor,auth.LastPredecessor is not null);
        Assert.Equal(successor,auth.LastSuccessor is not null);
        Assert.Equal(1,auth.Calls);
    }

    [Theory]
    [InlineData("rotation")]
    [InlineData("candidate")]
    [InlineData("spki")]
    [InlineData("thumbprint")]
    [InlineData("idempotency")]
    [InlineData("generation")]
    [InlineData("credential")]
    [InlineData("whitespace")]
    public async Task Rotation_CandidatesBindAllInputsBeforeAuthentication(string mutation)
    {
        using var fixture=new Fixture();
        var auth=new Auth(fixture.Actor) { Deny=true };
        var service=new CaptureRuntimeRotationApplicationService(auth,new Gateway());
        await service.CompleteRotationAsync(fixture.Signed,fixture.Rotation,fixture.Request,fixture.Operation,fixture.Body);
        var baseline=auth.LastPredecessor!.ToArray();
        var request=fixture.Request;
        var signed=fixture.Signed;
        var rotation=fixture.Rotation;
        var operation=fixture.Operation;
        var body=fixture.Body;
        switch(mutation)
        {
            case "rotation": rotation=Guid.NewGuid();break;
            case "candidate": request=request with { CandidateKeyId=Guid.NewGuid() };body=JsonSerializer.SerializeToUtf8Bytes(request);break;
            case "spki": using(var other=ECDsa.Create(ECCurve.NamedCurves.nistP256)) request=request with { SuccessorPublicVerifierSpki=Url(other.ExportSubjectPublicKeyInfo()) };body=JsonSerializer.SerializeToUtf8Bytes(request);break;
            case "thumbprint": request=request with { SuccessorPublicKeyThumbprint=new string('a',64) };body=JsonSerializer.SerializeToUtf8Bytes(request);break;
            case "idempotency": operation=Guid.NewGuid();break;
            case "generation": signed=signed with { CredentialGeneration=2 };break;
            case "credential": signed=signed with { CredentialId=Guid.NewGuid() };break;
            case "whitespace": body=body.Concat(new byte[] {32}).ToArray();break;
        }
        await service.CompleteRotationAsync(signed,rotation,request,operation,body);
        Assert.NotEqual(baseline,auth.LastPredecessor);
        Assert.Equal(2,auth.Calls);
    }

    [Theory]
    [InlineData("swap")]
    [InlineData("undefined-branch")]
    [InlineData("wrong-actor")]
    public async Task Rotation_ClassifierCorruptionNeverCallsBusiness(string mutation)
    {
        using var fixture=new Fixture();
        var auth=new Auth(fixture.Actor with { CredentialGeneration=2 }) { Mutation=mutation };
        var gateway=new Gateway();
        var result=await new CaptureRuntimeRotationApplicationService(auth,gateway).CompleteRotationAsync(
            fixture.Signed with { CredentialGeneration=2 },fixture.Rotation,fixture.Request,fixture.Operation,fixture.Body);
        Assert.Equal(503,result.Error!.StatusCode);
        Assert.Equal(1,auth.Calls);
        Assert.Equal(0,gateway.Completes+gateway.Replays);
    }

    [Fact]
    public async Task Rotation_InvalidSuccessorProofDoesNotInvokeBusinessAfterN()
    {
        using var fixture=new Fixture();
        var auth=new Auth(fixture.Actor);
        var gateway=new Gateway();
        var request=fixture.Request with { SuccessorProof=Url(new byte[64]) };
        var result=await new CaptureRuntimeRotationApplicationService(auth,gateway).CompleteRotationAsync(
            fixture.Signed,fixture.Rotation,request,fixture.Operation,JsonSerializer.SerializeToUtf8Bytes(request));
        Assert.Equal(403,result.Error!.StatusCode);
        Assert.Equal(1,auth.Calls);
        Assert.Equal(0,gateway.Completes+gateway.Replays);
    }

    private static string Url(byte[] bytes)=>Convert.ToBase64String(bytes).TrimEnd('=').Replace('+','-').Replace('/','_');
    private sealed class Fixture:IDisposable
    {
        private readonly ECDsa signer=ECDsa.Create(ECCurve.NamedCurves.nistP256);
        public Guid Rotation {get;}=Guid.NewGuid();
        public Guid Operation {get;}=Guid.NewGuid();
        public AuthenticatedCaptureRuntimeContext Actor {get;}=new(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),
            1,new byte[32],Guid.NewGuid(),1,1,2,2,DateTimeOffset.UtcNow,new byte[32],new byte[32]);
        public CaptureRuntimeSignedRequest Signed {get;}
        public CaptureRuntimeRotationCompleteRequest Request {get;}
        public byte[] Body {get;}
        public Fixture()
        {
            var spki=signer.ExportSubjectPublicKeyInfo();
            var request=new CaptureRuntimeRotationCompleteRequest(Guid.NewGuid(),Url(spki),
                Convert.ToHexString(SHA256.HashData(spki)).ToLowerInvariant(),string.Empty);
            var preimage=Encoding.UTF8.GetBytes(string.Join('\n',"TAG-EKYC-ROTATE1",Rotation.ToString("N"),
                Actor.CaptureAgentId.ToString("N"),Actor.DeviceInstallationId.ToString("N"),Actor.CredentialId.ToString("N"),
                "1",request.CandidateKeyId.ToString("N"),request.SuccessorPublicVerifierSpki,
                request.SuccessorPublicKeyThumbprint,Operation.ToString("N"))+"\n");
            Request=request with { SuccessorProof=Url(signer.SignData(preimage,HashAlgorithmName.SHA256,DSASignatureFormat.IeeeP1363FixedFieldConcatenation)) };
            Body=JsonSerializer.SerializeToUtf8Bytes(Request);
            Signed=new(Actor.CredentialId,1,DateTimeOffset.UtcNow,new byte[32],new byte[64],"CredentialRotation","synthetic-envelope"u8.ToArray());
        }
        public void Dispose()=>signer.Dispose();
    }
    private sealed class Auth(AuthenticatedCaptureRuntimeContext actor):ICaptureRuntimeRotationCompletionAuthenticator
    {
        public AuthenticatedCaptureRuntimeContext Actor {get;set;}=actor;
        public CaptureRuntimeRotationBranch Branch {get;set;}=CaptureRuntimeRotationBranch.Predecessor;
        public bool Deny {get;init;}
        public string? Mutation {get;init;}
        public int Calls {get;private set;}
        public byte[]? LastPredecessor {get;private set;}
        public byte[]? LastSuccessor {get;private set;}
        public Task<SessionOperationResult<CaptureRuntimeRotationAuthentication>> AuthenticateRotationCompletionAsync(
            CaptureRuntimeSignedRequest request,Guid rotation,CaptureRuntimeRotationCompleteRequest body,
            CaptureRuntimeRotationFingerprintCandidates candidates,CancellationToken token=default)
        {
            Calls++;
            LastPredecessor=candidates.AsPredecessor?.ToArray();
            LastSuccessor=candidates.AsSuccessor?.ToArray();
            if(Deny) return Task.FromResult(SessionOperationResult<CaptureRuntimeRotationAuthentication>.Failure(CaptureRuntimeErrorCodes.AccessDenied,"Denied",403));
            var selected=(Branch==CaptureRuntimeRotationBranch.Predecessor?candidates.AsPredecessor:candidates.AsSuccessor)!.ToArray();
            if(Mutation=="swap") selected=candidates.AsSuccessor!.ToArray();
            var returnedActor=Mutation=="wrong-actor"?Actor with {CredentialId=Guid.NewGuid()}:Actor;
            return Task.FromResult(SessionOperationResult<CaptureRuntimeRotationAuthentication>.Success(new(
                returnedActor,Mutation=="undefined-branch"?(CaptureRuntimeRotationBranch)999:Branch,selected)));
        }
    }
    private sealed class Gateway:ICaptureRuntimeRotationGateway
    {
        public int Completes {get;private set;}
        public int Replays {get;private set;}
        public byte[]? Fingerprint {get;private set;}
        public Task<SessionOperationResult<CaptureRuntimeRotationCompletionResponse>> CompleteRotationAsync(CaptureRuntimeRotationCommand command,CancellationToken token)
        {
            Completes++;return Result(command,false);
        }
        public Task<SessionOperationResult<CaptureRuntimeRotationCompletionResponse>> ReplayCompletedRotationAsync(CaptureRuntimeRotationCommand command,CancellationToken token)
        {
            Replays++;return Result(command,true);
        }
        private Task<SessionOperationResult<CaptureRuntimeRotationCompletionResponse>> Result(CaptureRuntimeRotationCommand command,bool replay)
        {
            Fingerprint=command.RequestFingerprint.ToArray();
            return Task.FromResult(SessionOperationResult<CaptureRuntimeRotationCompletionResponse>.Success(new(
                command.CredentialId,2,command.CandidateKeyId,Convert.ToHexString(command.PublicKeyThumbprint).ToLowerInvariant(),1,3,2),isReplay:replay));
        }
    }
}

