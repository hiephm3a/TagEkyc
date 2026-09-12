using System.Text.Json;
using TagEkyc.Application;
using TagEkyc.Application.CaptureRuntime;
using TagEkyc.Application.LocalDev;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.BusinessConsumer;
using TagEkyc.Contracts.CaptureAgent;
using TagEkyc.Contracts.CaptureRuntime;
using TagEkyc.Contracts.Common;
using TagEkyc.Contracts.TrustedAdapter;
using TagEkyc.Domain;

namespace TagEkyc.UnitTests;

public sealed class Tip88C1C6BA1RuntimeAppendTests
{
    private static readonly AuthenticatedCaptureRuntimeContext Actor = new(Guid.NewGuid(), Guid.NewGuid(),
        Guid.NewGuid(), 1, new byte[32], Guid.NewGuid(), 1, 1, 1, 1,
        DateTimeOffset.UtcNow, new byte[32], new byte[32]);

    [Fact]
    public async Task R24_UsesOnlyBindingDerivedSession_AndSameBusinessScope()
    {
        var fixture = await Fixture.Create();
        var binding = Guid.NewGuid(); fixture.Authority.Sessions[binding] = fixture.Session;
        var key = Guid.NewGuid();
        var result = await fixture.Service.AppendCaptureArtifactAsync(Actor, binding, Capture(binding), key, default);
        Assert.True(result.IsSuccess, result.Error?.Code);
        var artifact = Assert.Single(fixture.Artifacts.Artifacts);
        Assert.Equal(fixture.Session, artifact.VerificationSessionId);
        Assert.Equal(Actor.CaptureAgentId.ToString("N"), artifact.CaptureAgentId);
        Assert.Equal(Actor.DeviceInstallationId.ToString("N"), artifact.DeviceId);
        Assert.Equal(key.ToString("N"), Assert.Single(fixture.Store.Records).IdempotencyKey);
        Assert.Equal(1, fixture.Transaction.Calls);
        Assert.Equal(1, fixture.Proof.PlannerCalls); Assert.Equal(1, fixture.Proof.WriterCalls);
        Assert.False(fixture.Transaction.Active);
    }

    [Fact]
    public async Task R24_DifferentBindingSelectsDifferentDerivedSession_NotPreviousSession()
    {
        var fixture = await Fixture.Create();
        var second = await fixture.CreateSession();
        var firstBinding = Guid.NewGuid(); var secondBinding = Guid.NewGuid();
        fixture.Authority.Sessions[firstBinding] = fixture.Session;
        fixture.Authority.Sessions[secondBinding] = second;
        var result = await fixture.Service.AppendCaptureArtifactAsync(Actor, secondBinding, Capture(secondBinding),
            Guid.NewGuid(), default);
        Assert.True(result.IsSuccess, result.Error?.Code);
        Assert.Equal(second, Assert.Single(fixture.Artifacts.Artifacts).VerificationSessionId);
        Assert.DoesNotContain(fixture.Artifacts.Artifacts, x => x.VerificationSessionId == fixture.Session);
    }

    [Fact]
    public async Task R24_RouteBodyMismatch_IsBeforeBusinessTransaction()
    {
        var fixture = await Fixture.Create();
        var result = await fixture.Service.AppendCaptureArtifactAsync(Actor, Guid.NewGuid(), Capture(Guid.NewGuid()), Guid.NewGuid(), default);
        Assert.Equal(400, result.Error!.StatusCode);
        Assert.Equal(0, fixture.Transaction.Calls); Assert.Equal(0, fixture.Authority.Calls);
        Assert.Equal(0, fixture.Proof.PlannerCalls); Assert.Equal(0, fixture.Proof.WriterCalls);
    }

    [Fact]
    public async Task R25_EqualSession_UsesProductionPlannerAndWriteCore()
    {
        var fixture = await Fixture.Create();
        var binding = Guid.NewGuid(); fixture.Authority.Sessions[binding] = fixture.Session;
        var capture = await fixture.Service.AppendCaptureArtifactAsync(Actor, binding, Capture(binding), Guid.NewGuid(), default);
        Assert.True(capture.IsSuccess, capture.Error?.Code);
        var result = await fixture.Service.AppendEvidenceResultAsync(Actor, fixture.Session,
            Evidence(binding, capture.Value!.CaptureArtifactId), Guid.NewGuid(), default);
        Assert.True(result.IsSuccess, result.Error?.Code);
        Assert.Equal(fixture.Session, Assert.Single(fixture.Evidence.EvidenceResults).VerificationSessionId);
        Assert.Equal(2, fixture.Proof.PlannerCalls); Assert.Equal(2, fixture.Proof.WriterCalls);
        Assert.Equal(2, fixture.Transaction.Calls);
    }

    [Fact]
    public async Task R25_MismatchingSession_NoPlannerWriteOrAuthorityDetail()
    {
        var fixture = await Fixture.Create();
        var auditBefore = (await fixture.Audit.ListBySessionAsync(fixture.Session)).Count;
        var binding = Guid.NewGuid(); fixture.Authority.Sessions[binding] = fixture.Session;
        var result = await fixture.Service.AppendEvidenceResultAsync(Actor, Guid.NewGuid(),
            Evidence(binding, Guid.NewGuid().ToString("N")), Guid.NewGuid(), default);
        Assert.Equal(403, result.Error!.StatusCode);
        Assert.Equal("ACCESS_DENIED", result.Error.Code);
        Assert.Equal(0, fixture.Proof.PlannerCalls); Assert.Equal(0, fixture.Proof.WriterCalls);
        Assert.Empty(fixture.Store.Records); Assert.Empty(fixture.Artifacts.Artifacts);
        Assert.Empty(fixture.Evidence.EvidenceResults);
        Assert.Equal(auditBefore, (await fixture.Audit.ListBySessionAsync(fixture.Session)).Count);
        Assert.DoesNotContain(fixture.Session.ToString("N"), JsonSerializer.Serialize(result));
    }

    [Fact]
    public async Task R24_ExactKeyReplayDeduplicates_ChangedPayloadConflicts()
    {
        var fixture = await Fixture.Create();
        var binding = Guid.NewGuid(); fixture.Authority.Sessions[binding] = fixture.Session;
        var key = Guid.NewGuid(); var request = Capture(binding);
        var first = await fixture.Service.AppendCaptureArtifactAsync(Actor, binding, request, key, default);
        var replay = await fixture.Service.AppendCaptureArtifactAsync(Actor, binding, request, key, default);
        var conflict = await fixture.Service.AppendCaptureArtifactAsync(Actor, binding,
            request with { Payload = request.Payload with { MetadataHash = "sha256:changed" } }, key, default);
        Assert.True(first.IsSuccess); Assert.True(replay.IsSuccess);
        Assert.True(replay.Value!.Deduplicated); Assert.Equal(first.Value!.CaptureArtifactId, replay.Value.CaptureArtifactId);
        Assert.Equal(409, conflict.Error!.StatusCode); Assert.Single(fixture.Artifacts.Artifacts);
    }

    private static CaptureRuntimeCaptureArtifactRequest Capture(Guid binding) => new(binding,
        new(CaptureArtifactTypeDto.DeviceCaptureMetadata, CaptureSourceDto.PcAgent, null, "sha256:metadata", "request", "correlation"));
    private static CaptureRuntimeEvidenceResultRequest Evidence(Guid binding, string artifactId) => new(binding,
        new(EvidenceResultTypeDto.CaptureQuality, [artifactId], VerificationResultDto.Passed, 0.9m, [],
            null, "summary:capture-quality", "sha256:payload", SignaturePlaceholderStatusDto.PlaceholderUnverified,
            "quality", "1", "request", "correlation"));

    private sealed class Fixture
    {
        public readonly LocalDevInMemoryVerificationSessionRepository Sessions = new();
        public readonly LocalDevInMemoryCaptureArtifactRepository Artifacts = new();
        public readonly LocalDevInMemoryEvidenceResultRepository Evidence = new();
        public readonly LocalDevInMemoryAuditEventRepository Audit = new();
        public readonly LocalDevRuntimePolicySource Policies = new();
        public readonly Transaction Transaction = new();
        public LocalDevInMemoryAppendIdempotencyStore Store = null!;
        public Authority Authority = null!;
        public Probe Proof = null!;
        public CaptureRuntimeAppendApplicationService Service = null!;
        public Guid Session;
        public static async Task<Fixture> Create()
        {
            var f = new Fixture();
            f.Store = new(f.Sessions, f.Artifacts, f.Evidence, f.Audit);
            var core = new VerificationEvidenceApplicationService(f.Sessions, f.Artifacts, f.Evidence,
                f.Audit, f.Policies, f.Store, f.Store);
            f.Proof = new(core, f.Transaction); f.Authority = new(f.Transaction);
            f.Service = new(f.Transaction, f.Authority, f.Sessions, f.Policies, f.Artifacts, f.Proof, f.Proof);
            f.Session = await f.CreateSession();
            return f;
        }
        public async Task<Guid> CreateSession()
        {
            var result = await new VerificationSessionApplicationService(Sessions, Artifacts, Evidence, Audit, Policies)
                .CreateAsync(new(Guid.NewGuid(), LocalDevRuntimePolicySource.BusinessClientId, "ldev_biz",
                    AuthenticatedCallerCategory.BusinessConsumer, new HashSet<string> { "business.session.create" }),
                    new(null, "subject-ref", "PATIENT_REGISTRATION", VerificationProfileDto.StandardEkycProfile,
                        [new(RequiredCheckTypeDto.CaptureQuality, true, null)], DateTimeOffset.UtcNow.AddMinutes(30), "request", "correlation"));
            Assert.True(result.IsSuccess, result.Error?.Code);
            return Guid.Parse(result.Value!.VerificationSessionId);
        }
    }
    private sealed class Transaction : IAppendBusinessTransaction
    {
        public bool Active; public int Calls;
        public async Task<SessionOperationResult<T>> ExecuteAsync<T>(Func<CancellationToken, Task<SessionOperationResult<T>>> operation, CancellationToken ct)
        {
            Assert.False(Active); Calls++; Active = true;
            try { return await operation(ct); } finally { Active = false; }
        }
    }
    private sealed class Authority(Transaction transaction) : ICaptureRuntimeAppendAuthority
    {
        public Dictionary<Guid, Guid> Sessions = new(); public int Calls;
        public Task<SessionOperationResult<VerifiedRuntimeAppendAuthority>> ValidateCaptureAsync(AuthenticatedCaptureRuntimeContext actor, Guid binding, DateTimeOffset now, CancellationToken ct) => Validate(binding);
        public Task<SessionOperationResult<VerifiedRuntimeAppendAuthority>> ValidateEvidenceAsync(AuthenticatedCaptureRuntimeContext actor, Guid binding, DateTimeOffset now, CancellationToken ct) => Validate(binding);
        private Task<SessionOperationResult<VerifiedRuntimeAppendAuthority>> Validate(Guid binding)
        {
            Assert.True(transaction.Active); Calls++;
            return Task.FromResult(SessionOperationResult<VerifiedRuntimeAppendAuthority>.Success(new(
                Sessions[binding], Actor.RolePolicyId, 1, 1, 1, 1, Guid.NewGuid(), 1, binding)));
        }
    }
    private sealed class Probe(VerificationEvidenceApplicationService core, Transaction transaction) :
        IAuthorityNeutralVerificationEvidencePlanner, IAuthorityNeutralVerificationEvidenceWriter
    {
        public int PlannerCalls; public int WriterCalls;
        public Task<SessionOperationResult<AppendCaptureArtifactWrite>> PlanCaptureArtifactAsync(VerifiedAppendPrincipal principal, VerificationSession session, LocalDevClientPolicy policy, NeutralCaptureArtifactPayload payload, string key, DateTimeOffset now, CancellationToken ct)
        { Assert.True(transaction.Active); PlannerCalls++; return core.PlanCaptureArtifactAsync(principal, session, policy, payload, key, now, ct); }
        public Task<SessionOperationResult<AppendEvidenceResultWrite>> PlanEvidenceResultAsync(VerifiedAppendPrincipal principal, VerificationSession session, LocalDevClientPolicy policy, IReadOnlyList<CaptureArtifact> artifacts, NeutralEvidenceResultPayload payload, string key, DateTimeOffset now, CancellationToken ct)
        { Assert.True(transaction.Active); PlannerCalls++; return core.PlanEvidenceResultAsync(principal, session, policy, artifacts, payload, key, now, ct); }
        public Task<SessionOperationResult<CaptureArtifactSubmissionResponseDto>> ApplyCaptureArtifactAsync(VerifiedAppendPrincipal principal, AppendCaptureArtifactWrite write, CancellationToken ct)
        { Assert.True(transaction.Active); WriterCalls++; return core.ApplyCaptureArtifactAsync(principal, write, ct); }
        public Task<SessionOperationResult<EvidenceResultSubmissionResponseDto>> ApplyEvidenceResultAsync(VerifiedAppendPrincipal principal, AppendEvidenceResultWrite write, CancellationToken ct)
        { Assert.True(transaction.Active); WriterCalls++; return core.ApplyEvidenceResultAsync(principal, write, ct); }
    }
}
