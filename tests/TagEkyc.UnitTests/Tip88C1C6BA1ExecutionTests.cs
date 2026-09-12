using System.Security.Cryptography;
using System.Text;
using TagEkyc.Application;
using TagEkyc.Application.CaptureRuntime;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.CaptureRuntime;
using TagEkyc.Contracts.CaptureAgent;
using TagEkyc.Contracts.TrustedAdapter;

namespace TagEkyc.UnitTests;

public sealed class Tip88C1C6BA1ExecutionTests
{
    private static readonly AuthenticatedClientContext Client = new(Guid.NewGuid(), Guid.NewGuid(), "client",
        AuthenticatedCallerCategory.BusinessConsumer, new HashSet<string>());
    private static readonly AuthenticatedCaptureRuntimeContext Runtime = new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, new byte[32], Guid.NewGuid(),
        1, 1, 1, 1, DateTimeOffset.UtcNow, new byte[32], new byte[32]);

    [Fact]
    public async Task Issue_UsesCurrentPepper_AndLostResponseReplayDoesNotReturnSecret()
    {
        var gateway = new Gateway(); var peppers = new Peppers();
        var service = new CaptureRuntimeExecutionApplicationService(gateway, peppers, gateway);
        var session = Guid.NewGuid(); var key = Guid.NewGuid(); var body = Encoding.UTF8.GetBytes("{\"Action\":\"Issue\"}");
        var first = await service.IssueOrReplaceCapabilityAsync(Client, session, new("Issue"), key, body);
        Assert.True(first.IsSuccess); Assert.Equal(43, first.Value!.Secret!.Length);
        Assert.Equal(7, gateway.Request!.PepperVersion);
        Assert.Equal(12, gateway.Request.LookupPrefix.Length);
        Assert.Equal(7, peppers.ResolvedVersion);
        var firstFingerprint = gateway.Request.RequestFingerprint.ToArray();
        gateway.Replay = true;
        var replay = await service.IssueOrReplaceCapabilityAsync(Client, session, new("Issue"), key, body);
        Assert.Equal("EXISTING_MATCH_SECRET_UNAVAILABLE", replay.Error!.Code);
        Assert.Null(replay.Value);
        Assert.Equal(firstFingerprint, gateway.Request.RequestFingerprint);
        Assert.All(gateway.Request.Digest, b => Assert.Equal(0, b));
    }

    [Fact]
    public async Task Issue_FingerprintBindsExactReceivedBytes()
    {
        var gateway = new Gateway(); var service = new CaptureRuntimeExecutionApplicationService(gateway, new Peppers(), gateway);
        var session = Guid.NewGuid(); var key = Guid.NewGuid();
        await service.IssueOrReplaceCapabilityAsync(Client, session, new("Issue"), key, Encoding.UTF8.GetBytes("{\"Action\":\"Issue\"}"));
        var first = gateway.Request!.RequestFingerprint;
        await service.IssueOrReplaceCapabilityAsync(Client, session, new("Issue"), key, Encoding.UTF8.GetBytes("{ \"Action\":\"Issue\"}"));
        Assert.NotEqual(first, gateway.Request!.RequestFingerprint);
    }

    [Theory]
    [InlineData("Issue", true, 1)]
    [InlineData("Replace", false, 1)]
    [InlineData("Replace", true, 0)]
    [InlineData("Delete", false, 0)]
    public async Task CapabilityClosedUnionRejectsBeforeGateway(string action, bool current, long revision)
    {
        var gateway = new Gateway(); var service = new CaptureRuntimeExecutionApplicationService(gateway, new Peppers(), gateway);
        var result = await service.IssueOrReplaceCapabilityAsync(Client, Guid.NewGuid(),
            new(action, current ? Guid.NewGuid() : null, revision == 0 ? null : revision),
            Guid.NewGuid(), "{}"u8.ToArray());
        Assert.Equal(400, result.Error!.StatusCode); Assert.Null(gateway.Request);
    }

    [Fact]
    public async Task Bind_ResolvesPersistedPepperVersion_AndPassesVerifiedBooleanOnly()
    {
        var gateway = new Gateway(); var peppers = new Peppers();
        var secret = Enumerable.Repeat((byte)9, 32).ToArray();
        gateway.Verifier = new(CaptureRuntimeVerifierCryptography.ComputeDigest(new byte[32],
            CaptureRuntimeVerifierPepperDomain.CapabilityDigest, secret), 3);
        var service = new CaptureRuntimeExecutionApplicationService(gateway, peppers, gateway);
        var operation = Guid.NewGuid();
        var result = await service.BindAsync(Runtime, new(Guid.NewGuid(),
            CaptureRuntimeVerifierCryptography.EncodeCanonicalSecret(secret), operation), operation, "{}"u8.ToArray());
        Assert.True(result.IsSuccess);
        Assert.Equal(3, peppers.ResolvedVersion);
        Assert.True(gateway.BindRequest!.SecretVerified);
        Assert.Equal(32, gateway.BindRequest.RequestFingerprint.Length);
    }

    [Fact]
    public async Task Bind_MismatchDoesNotReachMutation()
    {
        var gateway = new Gateway { Verifier = new(new byte[32], 3) };
        var service = new CaptureRuntimeExecutionApplicationService(gateway, new Peppers(), gateway);
        var operation = Guid.NewGuid();
        var result = await service.BindAsync(Runtime, new(Guid.NewGuid(),
            CaptureRuntimeVerifierCryptography.EncodeCanonicalSecret(new byte[32]), operation), operation, "{}"u8.ToArray());
        Assert.Equal(403, result.Error!.StatusCode); Assert.Null(gateway.BindRequest);
    }

    [Fact]
    public async Task MissingReferencedPepper_Is503_NotSecretMismatch()
    {
        var gateway = new Gateway { Verifier = new(new byte[32], 3) };
        var service = new CaptureRuntimeExecutionApplicationService(gateway, new Peppers { Missing = true }, gateway);
        var operation = Guid.NewGuid();
        var result = await service.BindAsync(Runtime, new(Guid.NewGuid(),
            CaptureRuntimeVerifierCryptography.EncodeCanonicalSecret(new byte[32]), operation), operation, "{}"u8.ToArray());
        Assert.Equal(503, result.Error!.StatusCode); Assert.Null(gateway.BindRequest);
    }

    private sealed class Peppers : ICaptureRuntimeVerifierPepperSource
    {
        public int CurrentVersion => 7;
        public int ResolvedVersion;
        public bool Missing;
        public ValueTask<ICaptureRuntimeVerifierPepperLease?> TryResolveAsync(int version,
            CaptureRuntimeVerifierPepperDomain domain, CancellationToken cancellationToken = default)
        {
            ResolvedVersion = version;
            return ValueTask.FromResult<ICaptureRuntimeVerifierPepperLease?>(Missing ? null : new Lease(version, domain));
        }
    }
    private sealed class Lease(int version, CaptureRuntimeVerifierPepperDomain domain) : ICaptureRuntimeVerifierPepperLease
    {
        public int Version => version;
        public CaptureRuntimeVerifierPepperDomain Domain => domain;
        public ReadOnlyMemory<byte> Key { get; } = new byte[32];
        public void Dispose() { }
    }

    private sealed class Gateway : ICaptureRuntimeExecutionGateway, ICaptureRuntimeAppendGateway
    {
        public CaptureCapabilityPersistenceRequest? Request;
        public CaptureRuntimeBindPersistenceRequest? BindRequest;
        public CaptureCapabilityVerifier? Verifier;
        public bool Replay;
        public Task<CaptureCapabilityPersistenceResult> IssueOrReplaceCapabilityAsync(CaptureCapabilityPersistenceRequest request, CancellationToken ct)
        {
            Request = request;
            return Task.FromResult(new CaptureCapabilityPersistenceResult(Replay ? "EXISTING_MATCH_SECRET_UNAVAILABLE" : "CREATED",
                request.NewCapabilityId, !Replay, DateTimeOffset.UtcNow.AddMinutes(5), "ActiveUnbound", 1));
        }
        public Task<SessionOperationResult<CaptureCapabilityVerifier>> ResolveCapabilityVerifierAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(Verifier is null ? SessionOperationResult<CaptureCapabilityVerifier>.Failure("ACCESS_DENIED", "Denied", 403) :
                SessionOperationResult<CaptureCapabilityVerifier>.Success(Verifier));
        public Task<SessionOperationResult<CaptureRuntimeBindingResponse>> BindCapabilityAsync(CaptureRuntimeBindPersistenceRequest request, CancellationToken ct)
        {
            BindRequest = request;
            return Task.FromResult(SessionOperationResult<CaptureRuntimeBindingResponse>.Success(new(Guid.NewGuid(), DateTimeOffset.UtcNow.AddMinutes(5), 1, 1, 1, 2)));
        }
        public Task<SessionOperationResult<CaptureRuntimeBindingResponse>> ReconcileBindingAsync(AuthenticatedCaptureRuntimeContext actor, CaptureRuntimeReconcileRequest request, CancellationToken ct) => throw new NotSupportedException();
        public Task<SessionOperationResult<CaptureRuntimeConfigurationResponse>> ResolveConfigurationAsync(AuthenticatedCaptureRuntimeContext actor, CancellationToken ct) => throw new NotSupportedException();
        public Task<SessionOperationResult<CaptureArtifactSubmissionResponseDto>> AppendCaptureArtifactAsync(AuthenticatedCaptureRuntimeContext actor, Guid bindingId, CaptureRuntimeCaptureArtifactRequest request, Guid key, CancellationToken ct) => throw new NotSupportedException();
        public Task<SessionOperationResult<EvidenceResultSubmissionResponseDto>> AppendEvidenceResultAsync(AuthenticatedCaptureRuntimeContext actor, Guid sessionId, CaptureRuntimeEvidenceResultRequest request, Guid key, CancellationToken ct) => throw new NotSupportedException();
    }
}
