using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TagEkyc.Application.CaptureRuntime;
using TagEkyc.Application.Ports;
using TagEkyc.Contracts.CaptureRuntime;
using TagEkyc.Infrastructure.CaptureRuntime;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88C1C6BA1EnrollmentTests(PostgresPersistenceFixture postgres)
{
    [Fact]
    public async Task Enrollment_AtomicActivationAndLostResponseReplay()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var issuance = await SeedAsync();
        var (request, operation, body) = Request(issuance, key);
        var service = Service();
        var replies = await Task.WhenAll(
            service.RedeemAsync(request, operation, body),
            service.RedeemAsync(request, operation, body));
        Assert.All(replies, reply => Assert.True(reply.IsSuccess, reply.Error?.Code));
        Assert.Single(replies, reply => reply.IsReplay);
        Assert.Equal(replies[0].Value, replies[1].Value);
        var response = replies[0].Value!;
        Assert.Equal(1, response.Generation);
        Assert.Equal(1, await CountAsync("capture_runtime_bootstrap_redemption_operations", issuance));
        Assert.Equal(1, await CountAsync("capture_runtime_bootstrap_redemption_events", issuance));
        Assert.Equal("Redeemed", await StateAsync(issuance));

        // Same signed request after response loss uses the exact persisted version,
        // even though the provider's current version differs.
        var replay = await service.RedeemAsync(request, operation, body);
        Assert.True(replay.IsSuccess);
        Assert.True(replay.IsReplay);
        Assert.Equal(response, replay.Value);

        // Whitespace changes preserve ENROLL1 but change the exact JSON fingerprint.
        var conflict = await service.RedeemAsync(request, operation, body.Concat(new byte[] {32}).ToArray());
        Assert.Equal(409, conflict.Error!.StatusCode);
        Assert.Equal(1, await CountAsync("capture_runtime_bootstrap_redemption_events", issuance));
    }

    [Fact]
    public async Task Enrollment_ExpiryDenialCommitsAndReplaysWithoutSecondEvent()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var issuance = await SeedAsync(expired:true);
        var (request, operation, body) = Request(issuance,key);
        var service = Service();
        for (var pass=0;pass<2;pass++)
        {
            var result=await service.RedeemAsync(request,operation,body);
            Assert.Equal(403,result.Error!.StatusCode);
            Assert.Equal("Expired",await StateAsync(issuance));
            Assert.Equal(1,await CountAsync("capture_runtime_bootstrap_redemption_operations",issuance));
            Assert.Equal(1,await CountAsync("capture_runtime_bootstrap_redemption_events",issuance));
        }
    }

    [Fact]
    public async Task Enrollment_InvalidSecretOrPop_HasNoMutation()
    {
        using var key=ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var issuance=await SeedAsync();
        var (request,operation,body)=Request(issuance,key,wrongSecret:true);
        var result=await Service().RedeemAsync(request,operation,body);
        Assert.Equal(403,result.Error!.StatusCode);
        Assert.Equal("Active",await StateAsync(issuance));
        Assert.Equal(0,await CountAsync("capture_runtime_bootstrap_redemption_operations",issuance));
        Assert.Equal(0,await CountAsync("capture_runtime_bootstrap_redemption_events",issuance));
    }

    [Fact]
    public async Task Enrollment_ReferencedPepperUnavailable_Is503AndNotCredentialMismatch()
    {
        using var key=ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var issuance=await SeedAsync();
        var (request,operation,body)=Request(issuance,key);
        var result=await Service(missingPepper:true).RedeemAsync(request,operation,body);
        Assert.Equal(503,result.Error!.StatusCode);
        Assert.Equal(CaptureRuntimeErrorCodes.NotReady,result.Error.Code);
        Assert.Equal(0,await CountAsync("capture_runtime_bootstrap_redemption_events",issuance));
    }

    private CaptureRuntimeEnrollmentApplicationService Service(bool missingPepper=false) =>
        new(new CaptureRuntimeEnrollmentPersistenceBoundary(new RuntimeFactory(postgres.ConnectionString)),
            new Peppers(missingPepper));

    private async Task<Guid> SeedAsync(bool expired=false)
    {
        var issuance=Guid.NewGuid();
        await using var connection=new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using var command=connection.CreateCommand();
        command.CommandText="""
            INSERT INTO tagekyc.platform_operator_credentials VALUES
              (@operator,@prefix,decode(repeat('11',32),'hex'),1,@principal,
               ARRAY['operator.capture-runtime.manage'],'Active',1,now()-interval '1 hour',now()+interval '1 day',NULL,NULL);
            INSERT INTO tagekyc.capture_runtime_role_policy_revisions VALUES
              (@role,1,ARRAY['Configuration'],now()-interval '1 hour',@operator,now());
            INSERT INTO tagekyc.capture_runtime_role_policy_heads VALUES (@role,1,1,now());
            INSERT INTO tagekyc.capture_runtime_trust_profile_revisions VALUES
              (@trust,1,'Managed',true,true,false,now()-interval '1 hour',now()+interval '1 day',@operator,now());
            INSERT INTO tagekyc.capture_runtime_trust_profile_heads VALUES (@trust,1,1,now());
            INSERT INTO tagekyc.capture_runtime_configuration_revisions VALUES
              (@config,1,now()-interval '1 hour',now()+interval '1 day',true,60,100,60,1000,1000,10000,10000,10000,1024,@operator,now());
            INSERT INTO tagekyc.capture_runtime_configuration_heads VALUES (@config,1,1,now());
            INSERT INTO tagekyc.capture_runtime_bootstrap_issuances
             ("BootstrapIssuanceId","KeyLookupPrefix","SecretDigest","VerifierPepperVersion",
              "RuntimeType","TrustProfileId","TrustProfileRevision","RolePolicyId","RolePolicyRevision",
              "ConfigurationId","ConfigurationRevision","AttestationRequirementDigest","RequestFingerprint",
              "IssueOperationId","IssuedByCredentialId","IssuedAtUtc","ExpiresAtUtc","State","Revision")
            VALUES(@issuance,@bootstrap_prefix,@digest,7,'Managed',@trust,1,@role,1,@config,1,
              decode(repeat('22',32),'hex'),decode(repeat('33',32),'hex'),@issue_operation,@operator,
              now()-interval '1 hour',@expiry,'Active',1);
            """;
        foreach(var (name,value) in new (string,object)[]
        {
            ("operator",Guid.NewGuid()),("prefix",Guid.NewGuid().ToString("N")[..12]),("principal",Guid.NewGuid()),
            ("role",Guid.NewGuid()),("trust",Guid.NewGuid()),("config",Guid.NewGuid()),("issuance",issuance),
            ("bootstrap_prefix",Guid.NewGuid().ToString("N")[..12]),("issue_operation",Guid.NewGuid()),
            ("digest",CaptureRuntimeVerifierCryptography.ComputeDigest(Enumerable.Repeat((byte)9,32).ToArray(),
                CaptureRuntimeVerifierPepperDomain.BootstrapDigest,Enumerable.Range(0,32).Select(i=>(byte)i).ToArray())),
            ("expiry",expired?DateTimeOffset.UtcNow.AddMinutes(-1):DateTimeOffset.UtcNow.AddHours(1))
        }) command.Parameters.AddWithValue(name,value);
        await command.ExecuteNonQueryAsync();
        return issuance;
    }

    private static (CaptureRuntimeEnrollmentRedeemRequest,Guid,byte[]) Request(Guid issuance,ECDsa key,bool wrongSecret=false)
    {
        var operation=Guid.NewGuid();
        var secret=wrongSecret?new byte[32]:Enumerable.Range(0,32).Select(i=>(byte)i).ToArray();
        var spki=key.ExportSubjectPublicKeyInfo();
        var request=new CaptureRuntimeEnrollmentRedeemRequest(issuance,Url(secret),Guid.NewGuid(),
            Url(spki),Convert.ToHexString(SHA256.HashData(spki)).ToLowerInvariant(),DateTimeOffset.UtcNow,
            Url(RandomNumberGenerator.GetBytes(32)),string.Empty);
        var preimage=Encoding.UTF8.GetBytes(string.Join('\n',"TAG-EKYC-ENROLL1",issuance.ToString("N"),
            request.CandidateKeyId.ToString("N"),request.PublicVerifierSpki,request.PublicKeyThumbprint,
            request.SignedAtUtc.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'",CultureInfo.InvariantCulture),request.Nonce,
            operation.ToString("N"),Convert.ToHexString(SHA256.HashData(secret)).ToLowerInvariant())+"\n");
        request=request with { CandidateProof=Url(key.SignData(preimage,HashAlgorithmName.SHA256,DSASignatureFormat.IeeeP1363FixedFieldConcatenation)) };
        return(request,operation,JsonSerializer.SerializeToUtf8Bytes(request));
    }
    private static string Url(byte[] bytes)=>Convert.ToBase64String(bytes).TrimEnd('=').Replace('+','-').Replace('/','_');
    private async Task<long> CountAsync(string table,Guid issuance)
    {
        await using var connection=new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using var command=connection.CreateCommand();
        command.CommandText=$"SELECT count(*) FROM tagekyc.{table} WHERE \"BootstrapIssuanceId\"=@issuance";
        command.Parameters.AddWithValue("issuance",issuance);
        return (long)(await command.ExecuteScalarAsync())!;
    }
    private async Task<string> StateAsync(Guid issuance)
    {
        await using var connection=new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using var command=connection.CreateCommand();
        command.CommandText="SELECT \"State\" FROM tagekyc.capture_runtime_bootstrap_issuances WHERE \"BootstrapIssuanceId\"=@issuance";
        command.Parameters.AddWithValue("issuance",issuance);
        return (string)(await command.ExecuteScalarAsync())!;
    }
    private sealed class RuntimeFactory(string connectionString):ICaptureRuntimeDbContextFactory
    {
        public async ValueTask<TagEkycDbContext> CreateAsync(CancellationToken cancellationToken=default)
        {
            var db=new TagEkycDbContext(new DbContextOptionsBuilder<TagEkycDbContext>().UseNpgsql(connectionString).Options);
            await db.Database.OpenConnectionAsync(cancellationToken);
            await db.Database.ExecuteSqlRawAsync("SET ROLE tagekyc_capture_runtime_application",cancellationToken);
            return db;
        }
    }
    private sealed class Peppers(bool missing):ICaptureRuntimeVerifierPepperSource
    {
        public int CurrentVersion => throw new InvalidOperationException("R05 must never request CurrentVersion.");
        public ValueTask<ICaptureRuntimeVerifierPepperLease?> TryResolveAsync(int version,CaptureRuntimeVerifierPepperDomain domain,CancellationToken token)
        {
            Assert.Equal(7,version);
            Assert.Equal(CaptureRuntimeVerifierPepperDomain.BootstrapDigest,domain);
            return ValueTask.FromResult<ICaptureRuntimeVerifierPepperLease?>(missing?null:new Lease());
        }
    }
    private sealed class Lease:ICaptureRuntimeVerifierPepperLease
    {
        private readonly byte[] key=Enumerable.Repeat((byte)9,32).ToArray();
        public int Version=>7;
        public CaptureRuntimeVerifierPepperDomain Domain=>CaptureRuntimeVerifierPepperDomain.BootstrapDigest;
        public ReadOnlyMemory<byte> Key=>key;
        public void Dispose()=>CryptographicOperations.ZeroMemory(key);
    }
}
