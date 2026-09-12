using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TagEkyc.Application.CaptureRuntime;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.CaptureRuntime;
using TagEkyc.Infrastructure.Auth;
using TagEkyc.Infrastructure.CaptureRuntime;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88C1C6BA1RotationTests(PostgresPersistenceFixture postgres)
{
    [Fact]
    public async Task Rotation_DualProofAtomicCutoverAndSameRouteReplay()
    {
        using var predecessor=ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using var successor=ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var identity=await SeedAsync(predecessor);
        var input=Input(identity,successor);
        var gateway=new ObservedGateway(new CaptureRuntimeRotationPersistenceBoundary(
            new RoleFactory(postgres.ConnectionString,"tagekyc_capture_runtime_application")),()=>NonceCount(identity.Credential));
        var service=Service(gateway);
        var first=await service.CompleteRotationAsync(Signed(identity,1,predecessor,input),identity.Rotation,
            input.Request,input.Operation,input.Body);
        Assert.True(first.IsSuccess,
            $"initial-completion: {first.Error?.Code}; complete calls={gateway.Completes}; replay calls={gateway.Replays}; committed N at B entry={gateway.NoncesAtBusinessEntry}");
        Assert.Equal(2,first.Value!.Generation);
        Assert.Equal(1,gateway.NoncesAtBusinessEntry);
        Assert.Equal(1,gateway.Completes);
        Assert.Equal(0,gateway.Replays);
        Assert.Equal(1,await CompletionCount(identity.Rotation));
        Assert.Equal(1,await EventCount(identity.Rotation));
        var fingerprint=gateway.Fingerprint!.ToArray();

        // The assigned successor policy has Configuration, not CredentialRotation.
        // Only the classifier's exact committed successor path may admit this.
        var second=await service.CompleteRotationAsync(Signed(identity,2,successor,input),identity.Rotation,
            input.Request,input.Operation,input.Body);
        Assert.True(second.IsSuccess,
            $"successor-replay: {second.Error?.Code}; complete calls={gateway.Completes}; replay calls={gateway.Replays}; committed N at B entry={gateway.NoncesAtBusinessEntry}");
        Assert.True(second.IsReplay);
        Assert.Equal(first.Value,second.Value);
        Assert.Equal(fingerprint,gateway.Fingerprint);
        Assert.Equal(2,gateway.NoncesAtBusinessEntry);
        Assert.Equal(1,gateway.Completes);
        Assert.Equal(1,gateway.Replays);
        Assert.Equal(1,await CompletionCount(identity.Rotation));
        Assert.Equal(1,await EventCount(identity.Rotation));

        var changed=input with { Body=input.Body.Concat(new byte[]{32}).ToArray() };
        var denied=await service.CompleteRotationAsync(Signed(identity,2,successor,changed),identity.Rotation,
            changed.Request,changed.Operation,changed.Body);
        Assert.Equal(403,denied.Error!.StatusCode);
        Assert.Equal(2,await NonceCount(identity.Credential));
        Assert.Equal(2,gateway.Completes+gateway.Replays);
    }

    [Theory]
    [InlineData("crt1",0,0,403)]
    [InlineData("rotate1",1,0,403)]
    [InlineData("missing-role",0,0,403)]
    [InlineData("next-role-unassigned",1,1,409)]
    public async Task Rotation_DenialsDoNotFallbackAndKeepCommittedN(string scenario,int nonces,int calls,int status)
    {
        using var predecessor=ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using var successor=ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var identity=await SeedAsync(predecessor,missingRole:scenario=="missing-role",missingNext:scenario=="next-role-unassigned");
        var input=Input(identity,successor);
        if(scenario=="rotate1")
        {
            var bad=input.Request with {SuccessorProof=Url(new byte[64])};
            input=input with {Request=bad,Body=JsonSerializer.SerializeToUtf8Bytes(bad)};
        }
        var signed=Signed(identity,1,predecessor,input);
        if(scenario=="crt1") signed=signed with { Signature=new byte[64] };
        var gateway=new ObservedGateway(new CaptureRuntimeRotationPersistenceBoundary(
            new RoleFactory(postgres.ConnectionString,"tagekyc_capture_runtime_application")),()=>NonceCount(identity.Credential));
        var result=await Service(gateway).CompleteRotationAsync(signed,identity.Rotation,input.Request,input.Operation,input.Body);
        Assert.Equal(status,result.Error!.StatusCode);
        Assert.Equal(nonces,await NonceCount(identity.Credential));
        Assert.Equal(calls,gateway.Completes+gateway.Replays);
        Assert.Equal(0,await CompletionCount(identity.Rotation));
        Assert.Equal(0,await EventCount(identity.Rotation));
        Assert.Equal(1,await GenerationCount(identity.Credential));
    }

    [Fact]
    public async Task Rotation_ExpiredAuthorizationCommitsTerminalPairDespiteDenial()
    {
        using var predecessor=ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using var successor=ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var identity=await SeedAsync(predecessor,expired:true);
        var input=Input(identity,successor);
        var gateway=new ObservedGateway(new CaptureRuntimeRotationPersistenceBoundary(
            new RoleFactory(postgres.ConnectionString,"tagekyc_capture_runtime_application")),()=>NonceCount(identity.Credential));
        var result=await Service(gateway).CompleteRotationAsync(Signed(identity,1,predecessor,input),
            identity.Rotation,input.Request,input.Operation,input.Body);
        Assert.Equal(403,result.Error!.StatusCode);
        Assert.Equal(1,await NonceCount(identity.Credential));
        Assert.Equal(1,await CompletionCount(identity.Rotation));
        Assert.Equal(1,await EventCount(identity.Rotation));
        Assert.Equal(1,await GenerationCount(identity.Credential));
    }

    private CaptureRuntimeRotationApplicationService Service(ICaptureRuntimeRotationGateway gateway)=>
        new(new CaptureRuntimeRequestAuthenticator(new RoleFactory(postgres.ConnectionString,"tagekyc_capture_runtime_authenticator")),gateway);

    private async Task<Identity> SeedAsync(ECDsa predecessor,bool missingRole=false,bool missingNext=false,bool expired=false)
    {
        var identity=new Identity(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid());
        await using var connection=new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using var tx=await connection.BeginTransactionAsync();
        await using var command=connection.CreateCommand();
        command.Transaction=tx;
        command.CommandText="""
            INSERT INTO tagekyc.platform_operator_credentials VALUES
             (@operator,@prefix,decode(repeat('11',32),'hex'),1,@principal,ARRAY['operator.capture-runtime.manage'],
              'Active',1,now()-interval '1 hour',now()+interval '1 day',NULL,NULL);
            INSERT INTO tagekyc.capture_runtime_role_policy_revisions VALUES
             (@role,1,@roles,now()-interval '1 hour',@operator,now()),
             (@nextrole,1,ARRAY['Configuration'],now()-interval '1 hour',@operator,now());
            INSERT INTO tagekyc.capture_runtime_role_policy_heads VALUES (@role,1,1,now()),(@nextrole,1,1,now());
            INSERT INTO tagekyc.capture_runtime_trust_profile_revisions VALUES
             (@trust,1,'Managed',true,true,false,now()-interval '1 hour',now()+interval '1 day',@operator,now());
            INSERT INTO tagekyc.capture_runtime_trust_profile_heads VALUES (@trust,1,1,now());
            INSERT INTO tagekyc.capture_runtime_configuration_revisions VALUES
             (@config,1,now()-interval '1 hour',now()+interval '1 day',true,60,100,60,1000,1000,10000,10000,10000,1024,@operator,now());
            INSERT INTO tagekyc.capture_runtime_configuration_heads VALUES (@config,1,1,now());
            INSERT INTO tagekyc.capture_runtime_registrations VALUES
             (@agent,'Managed',@trust,1,@config,1,NULL,NULL,NULL,'Active',1,now(),NULL,NULL,NULL,NULL);
            INSERT INTO tagekyc.capture_runtime_installations VALUES
             (@installation,@agent,NULL,NULL,'Pending',1,now(),NULL,NULL,NULL,NULL);
            INSERT INTO tagekyc.capture_runtime_credential_generations VALUES
             (@credential,1,@installation,@candidate,@spki,'TAG-EKYC-CRT1-ECDSA-P256-SHA256',@thumb,
              @role,1,now()-interval '1 hour',now()+interval '1 day','Pending',1,NULL,NULL,NULL,NULL);
            UPDATE tagekyc.capture_runtime_installations SET "CurrentCredentialId"=@credential,
             "CurrentCredentialGeneration"=1,"LifecycleState"='Active',"Revision"=2 WHERE "DeviceInstallationId"=@installation;
            UPDATE tagekyc.capture_runtime_credential_generations SET "State"='Active',"Revision"=2 WHERE "CredentialId"=@credential;
            INSERT INTO tagekyc.capture_runtime_rotation_authorizations
             ("RotationAuthorizationId","CaptureAgentId","DeviceInstallationId","CredentialId","CurrentGeneration",
              "AuthorizeOperationId","RequestFingerprint","AuthorizedByCredentialId","AuthorizedAtUtc","ExpiresAtUtc","State","Revision")
            VALUES (@rotation,@agent,@installation,@credential,1,@authorize,decode(repeat('44',32),'hex'),
              @operator,now()-interval '2 minutes',@expiry,'Active',1);
            """;
        foreach(var (name,value) in new (string,object)[]
        {
            ("operator",Guid.NewGuid()),("prefix",Guid.NewGuid().ToString("N")[..12]),("principal",Guid.NewGuid()),
            ("role",Guid.NewGuid()),("nextrole",Guid.NewGuid()),("roles",new[]{missingRole?"Configuration":"CredentialRotation"}),
            ("trust",Guid.NewGuid()),("config",Guid.NewGuid()),("agent",identity.Agent),("installation",identity.Installation),
            ("credential",identity.Credential),("candidate",Guid.NewGuid()),("spki",predecessor.ExportSubjectPublicKeyInfo()),
            ("thumb",SHA256.HashData(predecessor.ExportSubjectPublicKeyInfo())),("rotation",identity.Rotation),("authorize",Guid.NewGuid()),
            ("expiry",expired?DateTimeOffset.UtcNow.AddMinutes(-1):DateTimeOffset.UtcNow.AddMinutes(5))
        }) command.Parameters.AddWithValue(name,value);
        await command.ExecuteNonQueryAsync();
        if(!missingNext)
        {
            command.CommandText="""UPDATE tagekyc.capture_runtime_registrations SET "NextRolePolicyId"=@nextrole,"NextRolePolicyRevision"=1 WHERE "CaptureAgentId"=@agent""";
            await command.ExecuteNonQueryAsync();
        }
        await tx.CommitAsync();
        return identity;
    }

    private static RotationInput Input(Identity identity,ECDsa successor)
    {
        var operation=Guid.NewGuid();
        var spki=successor.ExportSubjectPublicKeyInfo();
        var request=new CaptureRuntimeRotationCompleteRequest(Guid.NewGuid(),Url(spki),
            Convert.ToHexString(SHA256.HashData(spki)).ToLowerInvariant(),string.Empty);
        var preimage=Encoding.UTF8.GetBytes(string.Join('\n',"TAG-EKYC-ROTATE1",identity.Rotation.ToString("N"),
            identity.Agent.ToString("N"),identity.Installation.ToString("N"),identity.Credential.ToString("N"),"1",
            request.CandidateKeyId.ToString("N"),request.SuccessorPublicVerifierSpki,request.SuccessorPublicKeyThumbprint,
            operation.ToString("N"))+"\n");
        request=request with {SuccessorProof=Url(successor.SignData(preimage,HashAlgorithmName.SHA256,DSASignatureFormat.IeeeP1363FixedFieldConcatenation))};
        return new(request,operation,JsonSerializer.SerializeToUtf8Bytes(request));
    }

    private static CaptureRuntimeSignedRequest Signed(Identity identity,long generation,ECDsa signer,RotationInput input)
    {
        var nonce=RandomNumberGenerator.GetBytes(32);
        var now=DateTimeOffset.UtcNow;
        var preimage=Encoding.UTF8.GetBytes(string.Join('\n',"TAG-EKYC-CRT1","POST",
            $"/api/ekyc/capture-runtime/credential-rotations/{identity.Rotation:N}/complete",identity.Credential.ToString("N"),
            generation.ToString(CultureInfo.InvariantCulture),now.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'",CultureInfo.InvariantCulture),
            Url(nonce),"application/json",input.Body.Length.ToString(CultureInfo.InvariantCulture),
            Convert.ToHexString(SHA256.HashData(input.Body)).ToLowerInvariant(),string.Empty)+"\n");
        return new(identity.Credential,generation,now,nonce,signer.SignData(preimage,HashAlgorithmName.SHA256,
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation),"CredentialRotation",preimage);
    }

    private Task<long> NonceCount(Guid credential)=>Count("capture_runtime_request_nonces","CredentialId",credential);
    private Task<long> GenerationCount(Guid credential)=>Count("capture_runtime_credential_generations","CredentialId",credential);
    private Task<long> CompletionCount(Guid rotation)=>Count("capture_runtime_rotation_completion_operations","RotationAuthorizationId",rotation);
    private Task<long> EventCount(Guid rotation)=>Count("capture_runtime_rotation_completion_events","RotationAuthorizationId",rotation);
    private async Task<long> Count(string table,string column,Guid id)
    {
        await using var connection=new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using var command=connection.CreateCommand();
        command.CommandText=$"SELECT count(*) FROM tagekyc.{table} WHERE \"{column}\"=@id";
        command.Parameters.AddWithValue("id",id);
        return (long)(await command.ExecuteScalarAsync())!;
    }
    private static string Url(byte[] bytes)=>Convert.ToBase64String(bytes).TrimEnd('=').Replace('+','-').Replace('/','_');
    private sealed record Identity(Guid Agent,Guid Installation,Guid Credential,Guid Rotation);
    private sealed record RotationInput(CaptureRuntimeRotationCompleteRequest Request,Guid Operation,byte[] Body);
    private sealed class RoleFactory(string connectionString,string role):ICaptureRuntimeDbContextFactory
    {
        public async ValueTask<TagEkycDbContext> CreateAsync(CancellationToken cancellationToken=default)
        {
            var db=new TagEkycDbContext(new DbContextOptionsBuilder<TagEkycDbContext>().UseNpgsql(connectionString).Options);
            await db.Database.OpenConnectionAsync(cancellationToken);
            var setRole = role switch
            {
                "tagekyc_capture_runtime_authenticator" => "SET ROLE tagekyc_capture_runtime_authenticator",
                "tagekyc_capture_runtime_application" => "SET ROLE tagekyc_capture_runtime_application",
                _ => throw new InvalidOperationException("Unknown proof capability role.")
            };
            await db.Database.ExecuteSqlRawAsync(setRole,cancellationToken);
            return db;
        }
    }
    private sealed class ObservedGateway(ICaptureRuntimeRotationGateway inner,Func<Task<long>> countNonces):ICaptureRuntimeRotationGateway
    {
        public long NoncesAtBusinessEntry {get;private set;}
        public int Completes {get;private set;}
        public int Replays {get;private set;}
        public byte[]? Fingerprint {get;private set;}
        public async Task<SessionOperationResult<CaptureRuntimeRotationCompletionResponse>> CompleteRotationAsync(CaptureRuntimeRotationCommand command,CancellationToken token)
        {
            Completes++;NoncesAtBusinessEntry=await countNonces();Fingerprint=command.RequestFingerprint.ToArray();
            return await inner.CompleteRotationAsync(command,token);
        }
        public async Task<SessionOperationResult<CaptureRuntimeRotationCompletionResponse>> ReplayCompletedRotationAsync(CaptureRuntimeRotationCommand command,CancellationToken token)
        {
            Replays++;NoncesAtBusinessEntry=await countNonces();Fingerprint=command.RequestFingerprint.ToArray();
            return await inner.ReplayCompletedRotationAsync(command,token);
        }
    }
}
