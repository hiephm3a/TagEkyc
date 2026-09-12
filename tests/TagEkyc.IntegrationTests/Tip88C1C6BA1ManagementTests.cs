using System.Security.Cryptography;
using TagEkyc.Application.CaptureRuntime;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.CaptureRuntime;

namespace TagEkyc.IntegrationTests;

public sealed class Tip88C1C6BA1ManagementTests
{
    private static readonly Guid Id = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private static readonly Guid Key = Guid.Parse("22222222-2222-4222-8222-222222222222");
    private static AuthenticatedPlatformOperatorContext Actor => new(Id, Key, "OperatorAdmin",
        new HashSet<string> { "operator.capture-runtime.manage" }, "abcdefgh1234", 1, DateTimeOffset.UtcNow);

    [Fact]
    public async Task BootstrapIssue_SecretOnceAndExactReplay()
    {
        var gateway = new ManagementGateway();
        var peppers = new PepperSource();
        var service = new CaptureRuntimeManagementApplicationService(gateway, peppers);
        var request = new CaptureRuntimeBootstrapIssueRequest("Managed", Id, 1, Id, 1, Id, 1,
            DateTimeOffset.UtcNow.AddMinutes(3), new string('a', 64));
        var first = await service.IssueBootstrapAsync(Actor, request, Key, "{}"u8.ToArray());
        Assert.True(first.IsSuccess);
        Assert.Equal(43, first.Value!.BootstrapSecret!.Length);
        Assert.Equal(7, gateway.Version);
        Assert.Equal(32, gateway.Digest!.Length);
        Assert.All(gateway.PassedDigest!, b => Assert.Equal(0, b));
        var previousFingerprint = gateway.Fingerprint!.ToArray();
        gateway.SecretUnavailable = true;
        var replay = await service.IssueBootstrapAsync(Actor, request, Key, "{}"u8.ToArray());
        Assert.Equal("EXISTING_MATCH_SECRET_UNAVAILABLE", replay.Error!.Code);
        Assert.Equal(previousFingerprint, gateway.Fingerprint);
        Assert.Null(replay.Value);
        Assert.NotEqual(gateway.PreviousDigest, gateway.Digest);
    }

    [Fact]
    public async Task ManagementFingerprint_BindsExactReceivedBytesActorAndMethod()
    {
        var gateway = new ManagementGateway();
        var service = new CaptureRuntimeManagementApplicationService(gateway, new PepperSource());
        var request = new RuntimeLifecycleRequest(Id, 1, "OperatorSuspension");
        await service.SuspendRuntimeAsync(Actor, request, Key, "{\"a\":1}"u8.ToArray());
        var baseline = gateway.Fingerprint!;
        await service.SuspendRuntimeAsync(Actor, request, Key, "{ \"a\":1}"u8.ToArray());
        Assert.NotEqual(baseline, gateway.Fingerprint);
        await service.SuspendRuntimeAsync(Actor with { CredentialId = Guid.NewGuid() }, request, Key, "{\"a\":1}"u8.ToArray());
        Assert.NotEqual(baseline, gateway.Fingerprint);
        await service.ReactivateRuntimeAsync(Actor, request with { Reason = "OperatorReactivation" }, Key, "{\"a\":1}"u8.ToArray());
        Assert.NotEqual(baseline, gateway.Fingerprint);
        Assert.Equal(Key, gateway.Idempotency);
    }

    [Fact]
    public async Task Management_RejectsActorBeforeCallingGateway()
    {
        var gateway = new ManagementGateway();
        var service = new CaptureRuntimeManagementApplicationService(gateway, new PepperSource());
        var result = await service.SuspendRuntimeAsync(Actor with { CredentialId = Guid.Empty },
            new(Id, 1, "OperatorSuspension"), Key, "{}"u8.ToArray());
        Assert.Equal(403, result.Error!.StatusCode);
        Assert.Equal(0, gateway.Calls);
    }

    [Fact]
    public async Task Management_RejectsWrongReasonWithoutMutation()
    {
        var gateway = new ManagementGateway();
        var service = new CaptureRuntimeManagementApplicationService(gateway, new PepperSource());
        var result = await service.SuspendRuntimeAsync(Actor, new(Id, 1, "OperatorRevocation"), Key, "{}"u8.ToArray());
        Assert.Equal(400, result.Error!.StatusCode);
        Assert.Equal(0, gateway.Calls);
    }

    [Fact]
    public async Task Readiness_ResolvesCurrentAndEveryReferencedPepperVersion()
    {
        var source = new PepperSource();
        var gateway = new ManagementGateway();
        var service = new CaptureRuntimeManagementApplicationService(gateway, source);
        var result = await service.ReadReadinessAsync(Actor, Id);
        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsReady);
        Assert.Equal(new[] { 3, 3, 3, 7, 7, 7 }, source.Resolved.Order().ToArray());
        Assert.Equal(6, source.Domains.Distinct().Count());
        source.Missing = 3;
        result = await service.ReadReadinessAsync(Actor, Id);
        Assert.Equal("NOT_READY", result.Error!.Code);
    }

    [Theory]
    [InlineData(CaptureRuntimeVerifierPepperDomain.BootstrapDigest)]
    [InlineData(CaptureRuntimeVerifierPepperDomain.CapabilityDigest)]
    public async Task Readiness_MissingNonPlatformDomainFailsClosed(CaptureRuntimeVerifierPepperDomain domain)
    {
        var source = new PepperSource { MissingDomain = domain };
        var result = await new CaptureRuntimeManagementApplicationService(new ManagementGateway(), source)
            .ReadReadinessAsync(Actor, Id);
        Assert.Equal("NOT_READY", result.Error!.Code);
        Assert.Contains(source.Domains, d => d.Domain == domain);
    }

    [Fact]
    public async Task BootstrapIssue_MissingCurrentPepperDoesNotCallSql()
    {
        var gateway = new ManagementGateway();
        var service = new CaptureRuntimeManagementApplicationService(gateway, new PepperSource { Missing = 7 });
        var result = await service.IssueBootstrapAsync(Actor, new("Managed", Id, 1, Id, 1, Id, 1,
            DateTimeOffset.UtcNow.AddMinutes(3), new string('a', 64)), Key, "{}"u8.ToArray());
        Assert.Equal("NOT_READY", result.Error!.Code);
        Assert.Equal(0, gateway.Calls);
    }

    [Fact]
    public async Task Publications_AreDistinctTypedBoundariesAndRejectUnsortedRoleSet()
    {
        var gateway = new ControlGateway();
        var service = new CaptureRuntimeControlApplicationService(gateway);
        await service.PublishRolePolicyAsync(Actor, new(Id, 0, DateTimeOffset.UtcNow, new[] { "Bind", "Configuration" }), Key, "{}"u8.ToArray());
        Assert.Equal(1, gateway.Calls);
        Assert.Equal("PublishRolePolicy", gateway.Last);
        var fingerprint = gateway.Fingerprint!;
        await service.PublishTrustProfileAsync(Actor, new(Id, 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1), "Managed", false, false, true), Key, "{}"u8.ToArray());
        Assert.Equal("PublishTrustProfile", gateway.Last);
        Assert.NotEqual(fingerprint, gateway.Fingerprint);
        var invalid = await service.PublishRolePolicyAsync(Actor, new(Id, 0, DateTimeOffset.UtcNow, new[] { "Configuration", "Bind" }), Key, "{}"u8.ToArray());
        Assert.Equal(400, invalid.Error!.StatusCode);
        Assert.Equal(2, gateway.Calls);
    }

    [Fact]
    public async Task Management_AllTypedRoutesSelectTheirOwnGateway()
    {
        var gateway = new ManagementGateway();
        var service = new CaptureRuntimeManagementApplicationService(gateway, new PepperSource());
        var json = "{}"u8.ToArray();
        await service.RevokeBootstrapAsync(Actor, new(Id, 1, "OperatorRevocation"), Key, json);
        Assert.Equal("RevokeBootstrap", gateway.Last);
        await service.SuspendRuntimeAsync(Actor, new(Id, 1, "OperatorSuspension"), Key, json);
        Assert.Equal("SuspendRuntime", gateway.Last);
        await service.ReactivateRuntimeAsync(Actor, new(Id, 1, "OperatorReactivation"), Key, json);
        Assert.Equal("ReactivateRuntime", gateway.Last);
        await service.RevokeRuntimeAsync(Actor, new(Id, 1, "OperatorRevocation"), Key, json);
        Assert.Equal("RevokeRuntime", gateway.Last);
        await service.RetireRuntimeAsync(Actor, new(Id, 1, "OperatorRetirement"), Key, json);
        Assert.Equal("RetireRuntime", gateway.Last);
        await service.RevokeCredentialAsync(Actor, new(Id, Id, Id, 1, 1, "CredentialCompromise"), Key, json);
        Assert.Equal("RevokeCredential", gateway.Last);
        await service.AuthorizeRotationAsync(Actor, new(Id, Id, Id, 1, 1, DateTimeOffset.UtcNow.AddMinutes(3)), Key, json);
        Assert.Equal("AuthorizeRotation", gateway.Last);
        await service.RevokeRotationAsync(Actor, new(Id, 1, "OperatorRevocation"), Key, json);
        Assert.Equal("RevokeRotation", gateway.Last);
        await service.AssignRolePolicyAsync(Actor, new(Id, Id, 1, 1), Key, json);
        Assert.Equal("AssignRolePolicy", gateway.Last);
        await service.AssignConfigurationAsync(Actor, new(Id, Id, 1, 1, null), Key, json);
        Assert.Equal("AssignConfiguration", gateway.Last);
        Assert.Equal(10, gateway.Calls);
    }

    [Fact]
    public async Task ConfigurationPublication_UsesExactDdlRangesAndZeroMarginIsAllowed()
    {
        var gateway = new ControlGateway();
        var service = new CaptureRuntimeControlApplicationService(gateway);
        var request = new CaptureRuntimeConfigurationPublicationRequest(Id, 0, DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddHours(1), true, 60, 0, 10, 1024, 1024, 1024, 1024, 1024, 4096);
        await service.PublishConfigurationAsync(Actor, request, Key, "{}"u8.ToArray());
        Assert.Equal(1, gateway.Calls);
        Assert.Equal("PublishConfiguration", gateway.Last);
        var invalid = await service.PublishConfigurationAsync(Actor,
            request with { RawExportSourceMaximumChipDg2PortraitBytes = 67108865 }, Key, "{}"u8.ToArray());
        Assert.Equal(400, invalid.Error!.StatusCode);
        Assert.Equal(1, gateway.Calls);
        await service.PublishRolePolicyAsync(Actor, new(Id, 0, DateTimeOffset.UtcNow, new[] { "RawIngress" }), Key, "{}"u8.ToArray());
        Assert.Equal(2, gateway.Calls);
    }

    private sealed class PepperSource : ICaptureRuntimeVerifierPepperSource
    {
        public int CurrentVersion => 7;
        public int Missing { get; set; }
        public CaptureRuntimeVerifierPepperDomain? MissingDomain { get; set; }
        public List<int> Resolved { get; } = new();
        public List<(int Version, CaptureRuntimeVerifierPepperDomain Domain)> Domains { get; } = new();
        public ValueTask<ICaptureRuntimeVerifierPepperLease?> TryResolveAsync(int version, CaptureRuntimeVerifierPepperDomain domain, CancellationToken cancellationToken = default)
        {
            Resolved.Add(version);
            Domains.Add((version, domain));
            return ValueTask.FromResult<ICaptureRuntimeVerifierPepperLease?>(version == Missing || domain == MissingDomain ? null : new Lease(version, domain));
        }
    }
    private sealed class Lease(int version, CaptureRuntimeVerifierPepperDomain domain) : ICaptureRuntimeVerifierPepperLease
    {
        private readonly byte[] bytes = Enumerable.Repeat((byte)17, 32).ToArray();
        public int Version => version;
        public CaptureRuntimeVerifierPepperDomain Domain => domain;
        public ReadOnlyMemory<byte> Key => bytes;
        public void Dispose() => CryptographicOperations.ZeroMemory(bytes);
    }

    private sealed class ManagementGateway : ICaptureRuntimeManagementGateway
    {
        public int Calls;
        public string? Last;
        public byte[]? Fingerprint;
        public Guid Idempotency;
        public int Version;
        public bool SecretUnavailable;
        public byte[]? Digest;
        public byte[]? PreviousDigest;
        public byte[]? PassedDigest;
        public Task<SessionOperationResult<CaptureRuntimeBootstrapPersistenceResult>> IssueBootstrapAsync(
            AuthenticatedPlatformOperatorContext actor, CaptureRuntimeBootstrapIssueRequest request,
            Guid idempotencyKey, byte[] requestFingerprint,
            string keyLookupPrefix, byte[] secretDigest, int verifierPepperVersion,
            DateTimeOffset now, CancellationToken cancellationToken)
        {
            Calls++; Last = "IssueBootstrap"; Fingerprint = requestFingerprint.ToArray(); Idempotency = idempotencyKey;
            PreviousDigest = Digest; Digest = secretDigest.ToArray(); PassedDigest = secretDigest; Version = verifierPepperVersion;
            return Task.FromResult(SecretUnavailable
                ? SessionOperationResult<CaptureRuntimeBootstrapPersistenceResult>.Failure("EXISTING_MATCH_SECRET_UNAVAILABLE", "Secret unavailable.", 409)
                : SessionOperationResult<CaptureRuntimeBootstrapPersistenceResult>.Success(new(Id, true, request.ExpiresAtUtc, 1)));
        }
        public Task<SessionOperationResult<CaptureRuntimeBootstrapLifecycleResponse>> RevokeBootstrapAsync(
            AuthenticatedPlatformOperatorContext actor, CaptureRuntimeBootstrapRevokeRequest request,
            Guid idempotencyKey, byte[] requestFingerprint,
            DateTimeOffset now, CancellationToken cancellationToken)
        {
            Calls++; Last = "RevokeBootstrap"; Fingerprint = requestFingerprint.ToArray(); Idempotency = idempotencyKey;
            return Task.FromResult(SessionOperationResult<CaptureRuntimeBootstrapLifecycleResponse>.Failure("CONFLICT", "Conflict.", 409));
        }
        public Task<SessionOperationResult<CaptureRuntimeLifecycleResponse>> SuspendRuntimeAsync(
            AuthenticatedPlatformOperatorContext actor, RuntimeLifecycleRequest request,
            Guid idempotencyKey, byte[] requestFingerprint,
            DateTimeOffset now, CancellationToken cancellationToken)
        {
            Calls++; Last = "SuspendRuntime"; Fingerprint = requestFingerprint.ToArray(); Idempotency = idempotencyKey;
            return Task.FromResult(SessionOperationResult<CaptureRuntimeLifecycleResponse>.Failure("CONFLICT", "Conflict.", 409));
        }
        public Task<SessionOperationResult<CaptureRuntimeLifecycleResponse>> ReactivateRuntimeAsync(
            AuthenticatedPlatformOperatorContext actor, RuntimeLifecycleRequest request,
            Guid idempotencyKey, byte[] requestFingerprint,
            DateTimeOffset now, CancellationToken cancellationToken)
        {
            Calls++; Last = "ReactivateRuntime"; Fingerprint = requestFingerprint.ToArray(); Idempotency = idempotencyKey;
            return Task.FromResult(SessionOperationResult<CaptureRuntimeLifecycleResponse>.Failure("CONFLICT", "Conflict.", 409));
        }
        public Task<SessionOperationResult<CaptureRuntimeLifecycleResponse>> RevokeRuntimeAsync(
            AuthenticatedPlatformOperatorContext actor, RuntimeLifecycleRequest request,
            Guid idempotencyKey, byte[] requestFingerprint,
            DateTimeOffset now, CancellationToken cancellationToken)
        {
            Calls++; Last = "RevokeRuntime"; Fingerprint = requestFingerprint.ToArray(); Idempotency = idempotencyKey;
            return Task.FromResult(SessionOperationResult<CaptureRuntimeLifecycleResponse>.Failure("CONFLICT", "Conflict.", 409));
        }
        public Task<SessionOperationResult<CaptureRuntimeLifecycleResponse>> RetireRuntimeAsync(
            AuthenticatedPlatformOperatorContext actor, RuntimeLifecycleRequest request,
            Guid idempotencyKey, byte[] requestFingerprint,
            DateTimeOffset now, CancellationToken cancellationToken)
        {
            Calls++; Last = "RetireRuntime"; Fingerprint = requestFingerprint.ToArray(); Idempotency = idempotencyKey;
            return Task.FromResult(SessionOperationResult<CaptureRuntimeLifecycleResponse>.Failure("CONFLICT", "Conflict.", 409));
        }
        public Task<SessionOperationResult<CaptureRuntimeCredentialResponse>> RevokeCredentialAsync(
            AuthenticatedPlatformOperatorContext actor, CaptureRuntimeCredentialRevokeRequest request,
            Guid idempotencyKey, byte[] requestFingerprint,
            DateTimeOffset now, CancellationToken cancellationToken)
        {
            Calls++; Last = "RevokeCredential"; Fingerprint = requestFingerprint.ToArray(); Idempotency = idempotencyKey;
            return Task.FromResult(SessionOperationResult<CaptureRuntimeCredentialResponse>.Failure("CONFLICT", "Conflict.", 409));
        }
        public Task<SessionOperationResult<CaptureRuntimeRotationResponse>> AuthorizeRotationAsync(
            AuthenticatedPlatformOperatorContext actor, CaptureRuntimeRotationAuthorizeRequest request,
            Guid idempotencyKey, byte[] requestFingerprint,
            DateTimeOffset now, CancellationToken cancellationToken)
        {
            Calls++; Last = "AuthorizeRotation"; Fingerprint = requestFingerprint.ToArray(); Idempotency = idempotencyKey;
            return Task.FromResult(SessionOperationResult<CaptureRuntimeRotationResponse>.Failure("CONFLICT", "Conflict.", 409));
        }
        public Task<SessionOperationResult<CaptureRuntimeRotationLifecycleResponse>> RevokeRotationAsync(
            AuthenticatedPlatformOperatorContext actor, CaptureRuntimeRotationRevokeRequest request,
            Guid idempotencyKey, byte[] requestFingerprint,
            DateTimeOffset now, CancellationToken cancellationToken)
        {
            Calls++; Last = "RevokeRotation"; Fingerprint = requestFingerprint.ToArray(); Idempotency = idempotencyKey;
            return Task.FromResult(SessionOperationResult<CaptureRuntimeRotationLifecycleResponse>.Failure("CONFLICT", "Conflict.", 409));
        }
        public Task<SessionOperationResult<CaptureRuntimeRolePolicyAssignmentResponse>> AssignRolePolicyAsync(
            AuthenticatedPlatformOperatorContext actor, CaptureRuntimeRolePolicyAssignmentRequest request,
            Guid idempotencyKey, byte[] requestFingerprint,
            DateTimeOffset now, CancellationToken cancellationToken)
        {
            Calls++; Last = "AssignRolePolicy"; Fingerprint = requestFingerprint.ToArray(); Idempotency = idempotencyKey;
            return Task.FromResult(SessionOperationResult<CaptureRuntimeRolePolicyAssignmentResponse>.Failure("CONFLICT", "Conflict.", 409));
        }
        public Task<SessionOperationResult<CaptureRuntimeConfigurationAssignmentResponse>> AssignConfigurationAsync(
            AuthenticatedPlatformOperatorContext actor, CaptureRuntimeConfigurationAssignmentRequest request,
            Guid idempotencyKey, byte[] requestFingerprint,
            DateTimeOffset now, CancellationToken cancellationToken)
        {
            Calls++; Last = "AssignConfiguration"; Fingerprint = requestFingerprint.ToArray(); Idempotency = idempotencyKey;
            return Task.FromResult(SessionOperationResult<CaptureRuntimeConfigurationAssignmentResponse>.Failure("CONFLICT", "Conflict.", 409));
        }
        public Task<SessionOperationResult<CaptureRuntimeReadinessResponse>> ReadReadinessAsync(AuthenticatedPlatformOperatorContext actor, Guid captureAgentId, DateTimeOffset now, CancellationToken cancellationToken) =>
            Task.FromResult(SessionOperationResult<CaptureRuntimeReadinessResponse>.Success(new(
                Id, "Active", 1, "Active", 1, "Active", 1, 1, 1, 1, 1, new[] { 3 }, true, true, true, false, false)));
    }
    private sealed class ControlGateway : ICaptureRuntimeControlGateway
    {
        public int Calls;
        public string? Last;
        public byte[]? Fingerprint;
        public Guid Idempotency;
        public Task<SessionOperationResult<CaptureRuntimeCatalogPublicationResponse>> PublishTrustProfileAsync(
            AuthenticatedPlatformOperatorContext actor, CaptureRuntimeTrustProfilePublicationRequest request,
            Guid idempotencyKey, byte[] requestFingerprint,
            DateTimeOffset now, CancellationToken cancellationToken)
        {
            Calls++; Last = "PublishTrustProfile"; Fingerprint = requestFingerprint.ToArray(); Idempotency = idempotencyKey;
            return Task.FromResult(SessionOperationResult<CaptureRuntimeCatalogPublicationResponse>.Failure("CONFLICT", "Conflict.", 409));
        }
        public Task<SessionOperationResult<CaptureRuntimeCatalogPublicationResponse>> PublishRolePolicyAsync(
            AuthenticatedPlatformOperatorContext actor, CaptureRuntimeRolePolicyPublicationRequest request,
            Guid idempotencyKey, byte[] requestFingerprint,
            DateTimeOffset now, CancellationToken cancellationToken)
        {
            Calls++; Last = "PublishRolePolicy"; Fingerprint = requestFingerprint.ToArray(); Idempotency = idempotencyKey;
            return Task.FromResult(SessionOperationResult<CaptureRuntimeCatalogPublicationResponse>.Failure("CONFLICT", "Conflict.", 409));
        }
        public Task<SessionOperationResult<CaptureRuntimeCatalogPublicationResponse>> PublishConfigurationAsync(
            AuthenticatedPlatformOperatorContext actor, CaptureRuntimeConfigurationPublicationRequest request,
            Guid idempotencyKey, byte[] requestFingerprint,
            DateTimeOffset now, CancellationToken cancellationToken)
        {
            Calls++; Last = "PublishConfiguration"; Fingerprint = requestFingerprint.ToArray(); Idempotency = idempotencyKey;
            return Task.FromResult(SessionOperationResult<CaptureRuntimeCatalogPublicationResponse>.Failure("CONFLICT", "Conflict.", 409));
        }
    }
}
