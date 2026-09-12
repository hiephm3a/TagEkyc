using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TagEkyc.Application.CaptureRuntime;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.CaptureRuntime;

namespace TagEkyc.UnitTests;

public sealed class Tip88C1C6BA1EnrollmentTests
{
    [Fact]
    public async Task Enrollment_ExactVersionAndEnroll1_ProducesDigestAndFrozenReplay()
    {
        using var fixture = new EnrollmentFixture();
        var gateway = new Gateway();
        var peppers = new Peppers();
        gateway.BeforeRedeem = () => Assert.True(peppers.Lease!.Disposed);
        var service = new CaptureRuntimeEnrollmentApplicationService(gateway, peppers);
        var first = await service.RedeemAsync(fixture.Request, fixture.Operation, fixture.Body);
        Assert.True(first.IsSuccess);
        Assert.False(first.IsReplay);
        Assert.Equal(7, peppers.RequestedVersion);
        Assert.Equal(CaptureRuntimeVerifierPepperDomain.BootstrapDigest, peppers.RequestedDomain);
        Assert.Equal(0, peppers.CurrentVersionReads);
        Assert.Equal(fixture.Request.BootstrapIssuanceId, gateway.ResolvedIssuance);
        var expectedDigest = CaptureRuntimeVerifierCryptography.ComputeDigest(Enumerable.Repeat((byte)9, 32).ToArray(),
            CaptureRuntimeVerifierPepperDomain.BootstrapDigest, fixture.Secret);
        Assert.Equal(expectedDigest, gateway.Digest);
        Assert.Equal(32, gateway.Fingerprint!.Length);
        var fingerprint = gateway.Fingerprint.ToArray();
        Assert.All(gateway.LastCommand!.BootstrapDigest, value => Assert.Equal((byte)0, value));
        Assert.All(gateway.LastCommand.RequestFingerprint, value => Assert.Equal((byte)0, value));
        Assert.All(peppers.Lease!.Key.ToArray(), value => Assert.Equal((byte)0, value));

        gateway.Replay = true;
        var replay = await service.RedeemAsync(fixture.Request, fixture.Operation, fixture.Body);
        Assert.True(replay.IsSuccess);
        Assert.True(replay.IsReplay);
        Assert.Equal(first.Value, replay.Value);
        Assert.Equal(fingerprint, gateway.Fingerprint);
        Assert.Equal(2, gateway.RedeemCalls);
    }

    [Theory]
    [InlineData(null, 403)]
    [InlineData(0, 503)]
    [InlineData(-1, 503)]
    public async Task Enrollment_AbsentOrCorruptVersion_DoesNotRedeem(int? version, int status)
    {
        using var fixture = new EnrollmentFixture();
        var gateway = new Gateway { Version = version };
        var peppers = new Peppers();
        var result = await new CaptureRuntimeEnrollmentApplicationService(gateway, peppers)
            .RedeemAsync(fixture.Request, fixture.Operation, fixture.Body);
        Assert.Equal(status, result.Error!.StatusCode);
        Assert.Equal(0, peppers.ResolveCalls);
        Assert.Equal(0, gateway.RedeemCalls);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("wrong-version")]
    [InlineData("wrong-domain")]
    public async Task Enrollment_PepperDependency_IsNotReadyWithoutFallback(string mutation)
    {
        using var fixture = new EnrollmentFixture();
        var gateway = new Gateway();
        var peppers = new Peppers { Mutation = mutation };
        var result = await new CaptureRuntimeEnrollmentApplicationService(gateway, peppers)
            .RedeemAsync(fixture.Request, fixture.Operation, fixture.Body);
        Assert.Equal(CaptureRuntimeErrorCodes.NotReady, result.Error!.Code);
        Assert.Equal(503, result.Error.StatusCode);
        Assert.Equal(0, gateway.RedeemCalls);
        Assert.Equal(0, peppers.CurrentVersionReads);
    }

    [Theory]
    [InlineData("signature")]
    [InlineData("thumbprint")]
    [InlineData("key")]
    [InlineData("nonce")]
    [InlineData("operation")]
    [InlineData("secret")]
    [InlineData("time")]
    public async Task Enrollment_ProofBindsEveryCeremonyInput(string mutation)
    {
        using var fixture = new EnrollmentFixture();
        var request = fixture.Request;
        var operation = fixture.Operation;
        switch (mutation)
        {
            case "signature": request = request with { CandidateProof = Url(new byte[64]) }; break;
            case "thumbprint": request = request with { PublicKeyThumbprint = new string('a',64) }; break;
            case "key": request = request with { CandidateKeyId = Guid.NewGuid() }; break;
            case "nonce": request = request with { Nonce = Url(Enumerable.Repeat((byte)8,32).ToArray()) }; break;
            case "operation": operation = Guid.NewGuid(); break;
            case "secret": request = request with { BootstrapSecret = Url(new byte[32]) }; break;
            case "time": request = request with { SignedAtUtc = request.SignedAtUtc.AddTicks(1) }; break;
        }
        var gateway = new Gateway();
        var result = await new CaptureRuntimeEnrollmentApplicationService(gateway, new Peppers())
            .RedeemAsync(request, operation, fixture.Body);
        Assert.Equal(CaptureRuntimeErrorCodes.AccessDenied, result.Error!.Code);
        Assert.Equal(0, gateway.RedeemCalls);
    }

    [Fact]
    public async Task Enrollment_ExactBodyFingerprint_IsNotReserialized()
    {
        using var fixture = new EnrollmentFixture();
        var gateway = new Gateway();
        var service = new CaptureRuntimeEnrollmentApplicationService(gateway, new Peppers());
        Assert.True((await service.RedeemAsync(fixture.Request, fixture.Operation, fixture.Body)).IsSuccess);
        var original = gateway.Fingerprint!.ToArray();
        var whitespaceVariant = fixture.Body.Concat(new byte[] { 32 }).ToArray();
        Assert.True((await service.RedeemAsync(fixture.Request, fixture.Operation, whitespaceVariant)).IsSuccess);
        Assert.NotEqual(original, gateway.Fingerprint);
    }

    [Theory]
    [InlineData("secret-alias")]
    [InlineData("nonce-alias")]
    [InlineData("spki-padding")]
    [InlineData("invalid-id")]
    [InlineData("body-empty")]
    public async Task Enrollment_NoncanonicalInput_DoesNotRedeem(string mutation)
    {
        using var fixture = new EnrollmentFixture();
        var request = fixture.Request;
        var body = fixture.Body;
        switch (mutation)
        {
            case "secret-alias": request = request with { BootstrapSecret = request.BootstrapSecret[..^1] + "9" }; break;
            case "nonce-alias": request = request with { Nonce = request.Nonce[..^1] + "9" }; break;
            case "spki-padding": request = request with { PublicVerifierSpki = request.PublicVerifierSpki + "=" }; break;
            case "invalid-id": request = request with { CandidateKeyId = Guid.Empty }; break;
            case "body-empty": body = []; break;
        }
        var gateway = new Gateway();
        var result = await new CaptureRuntimeEnrollmentApplicationService(gateway, new Peppers())
            .RedeemAsync(request, fixture.Operation, body);
        Assert.Equal(400, result.Error!.StatusCode);
        Assert.Equal(0, gateway.RedeemCalls);
    }

    private static string Url(byte[] value) => Convert.ToBase64String(value).TrimEnd('=').Replace('+','-').Replace('/','_');
    private sealed class EnrollmentFixture : IDisposable
    {
        private readonly ECDsa signer = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        public Guid Operation { get; } = Guid.NewGuid();
        public byte[] Secret { get; } = Enumerable.Range(0,32).Select(i => (byte)i).ToArray();
        public CaptureRuntimeEnrollmentRedeemRequest Request { get; }
        public byte[] Body { get; }
        public EnrollmentFixture()
        {
            var spki = signer.ExportSubjectPublicKeyInfo();
            var request = new CaptureRuntimeEnrollmentRedeemRequest(Guid.NewGuid(),Url(Secret),Guid.NewGuid(),
                Url(spki),Convert.ToHexString(SHA256.HashData(spki)).ToLowerInvariant(),
                DateTimeOffset.UtcNow,Url(Secret),string.Empty);
            var bytes = Encoding.UTF8.GetBytes(string.Join('\n',"TAG-EKYC-ENROLL1",
                request.BootstrapIssuanceId.ToString("N"),request.CandidateKeyId.ToString("N"),
                request.PublicVerifierSpki,request.PublicKeyThumbprint,
                request.SignedAtUtc.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'",CultureInfo.InvariantCulture),
                request.Nonce,Operation.ToString("N"),Convert.ToHexString(SHA256.HashData(Secret)).ToLowerInvariant())+"\n");
            Request = request with { CandidateProof = Url(signer.SignData(bytes,HashAlgorithmName.SHA256,DSASignatureFormat.IeeeP1363FixedFieldConcatenation)) };
            Body = JsonSerializer.SerializeToUtf8Bytes(Request);
        }
        public void Dispose() => signer.Dispose();
    }

    private sealed class Gateway : ICaptureRuntimeEnrollmentGateway
    {
        public int? Version { get; init; } = 7;
        public Guid ResolvedIssuance { get; private set; }
        public int RedeemCalls { get; private set; }
        public bool Replay { get; set; }
        public Action? BeforeRedeem { get; set; }
        public byte[]? Digest { get; private set; }
        public byte[]? Fingerprint { get; private set; }
        public CaptureRuntimeEnrollmentCommand? LastCommand { get; private set; }
        private readonly CaptureRuntimeEnrollmentResponse response = new(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),1,1,2,2);
        public Task<SessionOperationResult<int?>> ResolveBootstrapVerifierVersionAsync(Guid issuance, CancellationToken token)
        {
            ResolvedIssuance = issuance;
            return Task.FromResult(SessionOperationResult<int?>.Success(Version));
        }
        public Task<SessionOperationResult<CaptureRuntimeEnrollmentResponse>> RedeemBootstrapAsync(CaptureRuntimeEnrollmentCommand command,CancellationToken token)
        {
            BeforeRedeem?.Invoke();
            RedeemCalls++;
            LastCommand = command;
            Digest = command.BootstrapDigest.ToArray();
            Fingerprint = command.RequestFingerprint.ToArray();
            return Task.FromResult(SessionOperationResult<CaptureRuntimeEnrollmentResponse>.Success(response,isReplay:Replay));
        }
    }
    private sealed class Peppers : ICaptureRuntimeVerifierPepperSource
    {
        public string? Mutation { get; init; }
        public int CurrentVersionReads { get; private set; }
        public int CurrentVersion { get { CurrentVersionReads++; return 99; } }
        public int ResolveCalls { get; private set; }
        public int RequestedVersion { get; private set; }
        public CaptureRuntimeVerifierPepperDomain RequestedDomain { get; private set; }
        public Lease? Lease { get; private set; }
        public ValueTask<ICaptureRuntimeVerifierPepperLease?> TryResolveAsync(int version,CaptureRuntimeVerifierPepperDomain domain,CancellationToken token)
        {
            ResolveCalls++; RequestedVersion=version; RequestedDomain=domain;
            Lease = new Lease(Mutation=="wrong-version"?8:version,Mutation=="wrong-domain"?CaptureRuntimeVerifierPepperDomain.PlatformCredential:domain);
            return ValueTask.FromResult<ICaptureRuntimeVerifierPepperLease?>(Mutation=="missing"?null:Lease);
        }
    }
    private sealed class Lease(int version, CaptureRuntimeVerifierPepperDomain domain) : ICaptureRuntimeVerifierPepperLease
    {
        private readonly byte[] key = Enumerable.Repeat((byte)9,32).ToArray();
        public int Version => version;
        public CaptureRuntimeVerifierPepperDomain Domain => domain;
        public ReadOnlyMemory<byte> Key => key;
        public bool Disposed { get; private set; }
        public void Dispose() { Disposed=true; CryptographicOperations.ZeroMemory(key); }
    }
}
