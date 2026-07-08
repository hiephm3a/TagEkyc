using TagEkyc.Application;
using TagEkyc.Application.LocalDev;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.BusinessConsumer;
using TagEkyc.Contracts.CaptureAgent;
using TagEkyc.Contracts.Common;
using TagEkyc.Contracts.TrustedAdapter;
using TagEkyc.Domain;

namespace TagEkyc.UnitTests;

public sealed class Tip82SSessionCancelTests
{
    [Fact]
    public async Task Cancel_requires_business_scope_and_same_business_client()
    {
        var fixture = CreateFixture();
        var session = await CreateSessionAsync(fixture);

        var captureCaller = await fixture.CompletionService.CancelAsync(
            CaptureCallerWithCancelScope(),
            session.VerificationSessionId,
            new CancelVerificationSessionRequestDto("caller_void", "req-cancel", "corr-cancel"),
            CancellationToken.None);
        var trustedCaller = await fixture.CompletionService.CancelAsync(
            TrustedCallerWithCancelScope(),
            session.VerificationSessionId,
            new CancelVerificationSessionRequestDto("caller_void", "req-cancel", "corr-cancel"),
            CancellationToken.None);
        var businessMissingScope = await fixture.CompletionService.CancelAsync(
            BusinessCaller(scopes: new HashSet<string> { "business.session.read" }),
            session.VerificationSessionId,
            new CancelVerificationSessionRequestDto("caller_void", "req-cancel", "corr-cancel"),
            CancellationToken.None);
        var crossTenant = await fixture.CompletionService.CancelAsync(
            BusinessCaller(
                clientId: LocalDevRuntimePolicySource.OtherBusinessClientId,
                apiKeyId: Guid.Parse("20000000-0000-0000-0000-000000000902")),
            session.VerificationSessionId,
            new CancelVerificationSessionRequestDto("caller_void", "req-cancel", "corr-cancel"),
            CancellationToken.None);

        AssertFailure(captureCaller, "CALLER_CATEGORY_NOT_ALLOWED", 403);
        AssertFailure(trustedCaller, "CALLER_CATEGORY_NOT_ALLOWED", 403);
        AssertFailure(businessMissingScope, "MISSING_SCOPE", 403);
        AssertFailure(crossTenant, "FORBIDDEN_CLIENT_APPLICATION", 403);
        Assert.Contains(fixture.Audit.Events, audit => audit.EventType == "SESSION_ACCESS_DENIED");
        Assert.DoesNotContain(fixture.Audit.Events, audit => audit.EventType == "SESSION_CANCELLED");
    }

    [Theory]
    [InlineData("free text")]
    [InlineData("bad/token")]
    [InlineData("bad|token")]
    public async Task Cancel_rejects_invalid_reason_tokens_without_state_change(string reason)
    {
        var fixture = CreateFixture();
        var session = await CreateSessionAsync(fixture);

        var result = await fixture.CompletionService.CancelAsync(
            BusinessCaller(),
            session.VerificationSessionId,
            new CancelVerificationSessionRequestDto(reason, "req-cancel", "corr-cancel"),
            CancellationToken.None);
        var current = await fixture.Sessions.GetAsync(Guid.Parse(session.VerificationSessionId));

        AssertFailure(result, "INVALID_CANCEL_REASON", 400);
        Assert.Equal(VerificationSessionState.Created, current!.State);
        Assert.DoesNotContain(fixture.Audit.Events, audit => audit.EventType == "SESSION_CANCELLED");
    }

    [Fact]
    public async Task Cancel_rejects_overlong_reason_token()
    {
        var fixture = CreateFixture();
        var session = await CreateSessionAsync(fixture);

        var result = await fixture.CompletionService.CancelAsync(
            BusinessCaller(),
            session.VerificationSessionId,
            new CancelVerificationSessionRequestDto(new string('a', 65), "req-cancel", "corr-cancel"),
            CancellationToken.None);

        AssertFailure(result, "INVALID_CANCEL_REASON", 400);
        Assert.DoesNotContain(fixture.Audit.Events, audit => audit.EventType == "SESSION_CANCELLED");
    }

    [Fact]
    public async Task Cancel_sets_cancelled_state_and_audit_atomically_and_replays_as_success()
    {
        var fixture = CreateFixture();
        var session = await CreateSessionAsync(fixture);

        var first = await fixture.CompletionService.CancelAsync(
            BusinessCaller(),
            session.VerificationSessionId,
            new CancelVerificationSessionRequestDto("caller_void", "req-cancel-1", "corr-cancel-1"),
            CancellationToken.None);
        var second = await fixture.CompletionService.CancelAsync(
            BusinessCaller(),
            session.VerificationSessionId,
            new CancelVerificationSessionRequestDto("business_abort", "req-cancel-2", "corr-cancel-2"),
            CancellationToken.None);
        var current = await fixture.Sessions.GetAsync(Guid.Parse(session.VerificationSessionId));
        var cancellationAudits = fixture.Audit.Events
            .Where(audit => audit.EventType == "SESSION_CANCELLED")
            .ToArray();

        Assert.True(first.IsSuccess, first.Error?.Code);
        Assert.True(second.IsSuccess, second.Error?.Code);
        Assert.Equal(VerificationSessionStateDto.Cancelled, first.Value!.State);
        Assert.Equal(VerificationSessionStateDto.Cancelled, second.Value!.State);
        Assert.Equal(VerificationSessionState.Cancelled, current!.State);
        Assert.Single(cancellationAudits);
        Assert.Equal("caller_void", cancellationAudits[0].EventPayloadRef);
    }

    [Theory]
    [InlineData(VerificationSessionState.Completed)]
    [InlineData(VerificationSessionState.Expired)]
    [InlineData(VerificationSessionState.TechnicalTerminal)]
    public async Task Cancel_rejects_non_cancelled_terminal_states(VerificationSessionState state)
    {
        var fixture = CreateFixture();
        var session = await CreateSessionAsync(fixture);
        await fixture.Sessions.SetStateAsync(Guid.Parse(session.VerificationSessionId), state);

        var result = await fixture.CompletionService.CancelAsync(
            BusinessCaller(),
            session.VerificationSessionId,
            new CancelVerificationSessionRequestDto("caller_void", "req-cancel", "corr-cancel"),
            CancellationToken.None);

        AssertFailure(result, "SESSION_TERMINAL", 409);
        Assert.DoesNotContain(fixture.Audit.Events, audit => audit.EventType == "SESSION_CANCELLED");
    }

    [Fact]
    public async Task Cancelled_session_rejects_complete_and_new_append_but_allows_existing_key_replay()
    {
        var fixture = CreateFixture();
        var session = await CreateSessionAsync(fixture);
        var artifact = await fixture.EvidenceService.AppendCaptureArtifactAsync(
            CaptureCaller(),
            session.VerificationSessionId,
            DeviceMetadataArtifact("sha256:metadata"),
            CancellationToken.None,
            "run-cancel-replay|artifact");
        Assert.True(artifact.IsSuccess, artifact.Error?.Code);

        var cancelled = await fixture.CompletionService.CancelAsync(
            BusinessCaller(),
            session.VerificationSessionId,
            new CancelVerificationSessionRequestDto("caller_void", "req-cancel", "corr-cancel"),
            CancellationToken.None);
        var complete = await fixture.CompletionService.CompleteAsync(
            BusinessCaller(),
            session.VerificationSessionId,
            new CompleteVerificationSessionRequestDto(
                ForceReview: false,
                RequestId: "req-complete",
                CorrelationId: "corr-complete"),
            CancellationToken.None);
        var replay = await fixture.EvidenceService.AppendCaptureArtifactAsync(
            CaptureCaller(),
            session.VerificationSessionId,
            DeviceMetadataArtifact("sha256:metadata"),
            CancellationToken.None,
            "run-cancel-replay|artifact");
        var newAppend = await fixture.EvidenceService.AppendCaptureArtifactAsync(
            CaptureCaller(),
            session.VerificationSessionId,
            DeviceMetadataArtifact("sha256:other-metadata"),
            CancellationToken.None,
            "run-cancel-replay|new-artifact");

        Assert.True(cancelled.IsSuccess, cancelled.Error?.Code);
        AssertFailure(complete, "SESSION_TERMINAL", 409);
        Assert.True(replay.IsSuccess, replay.Error?.Code);
        Assert.True(replay.Value!.Deduplicated);
        AssertFailure(newAppend, "SESSION_TERMINAL", 409);
        Assert.Single(fixture.Artifacts.Artifacts);
    }

    [Fact]
    public async Task Append_boundary_rechecks_cancelled_state_before_inprogress_write_from_stale_created_snapshot()
    {
        var fixture = CreateFixture();
        var session = await CreateSessionAsync(fixture);
        var stale = await fixture.Sessions.GetAsync(Guid.Parse(session.VerificationSessionId));
        Assert.NotNull(stale);
        await CancelAsync(fixture, session.VerificationSessionId);

        var result = await fixture.Idempotency.TryApplyCaptureArtifactAsync(
            new AppendCaptureArtifactWrite(
                IdempotencyRecord(stale!.Id, "run-stale-created|artifact", "captureArtifact", "DeviceCaptureMetadata"),
                DomainArtifact(stale.Id, "sha256:metadata-stale"),
                stale,
                VerificationSessionState.InProgress,
                []),
            CancellationToken.None);
        var current = await fixture.Sessions.GetAsync(stale.Id);

        Assert.Equal(AppendIdempotencyApplyStatus.SessionTerminal, result.Status);
        Assert.Equal(VerificationSessionState.Cancelled, current!.State);
        Assert.Empty(fixture.Artifacts.Artifacts);
        Assert.Empty(fixture.Idempotency.Records);
    }

    [Fact]
    public async Task Append_boundary_rechecks_cancelled_state_before_ready_write_from_stale_inprogress_snapshot()
    {
        var fixture = CreateFixture();
        var session = await CreateSessionAsync(fixture);
        await fixture.Sessions.SetStateAsync(Guid.Parse(session.VerificationSessionId), VerificationSessionState.InProgress);
        var stale = await fixture.Sessions.GetAsync(Guid.Parse(session.VerificationSessionId));
        Assert.NotNull(stale);
        await CancelAsync(fixture, session.VerificationSessionId);

        var result = await fixture.Idempotency.TryApplyEvidenceResultAsync(
            new AppendEvidenceResultWrite(
                IdempotencyRecord(stale!.Id, "run-stale-inprogress|evidence", "evidenceResult", "CaptureQuality"),
                DomainEvidence(stale.Id, "sha256:payload-stale"),
                stale,
                VerificationSessionState.ReadyToComplete,
                []),
            CancellationToken.None);
        var current = await fixture.Sessions.GetAsync(stale.Id);

        Assert.Equal(AppendIdempotencyApplyStatus.SessionTerminal, result.Status);
        Assert.Equal(VerificationSessionState.Cancelled, current!.State);
        Assert.Empty(fixture.Evidence.EvidenceResults);
        Assert.Empty(fixture.Idempotency.Records);
    }

    private static async Task<CreateVerificationSessionResponseDto> CreateSessionAsync(TestFixture fixture)
    {
        var created = await fixture.SessionService.CreateAsync(
            BusinessCaller(),
            new CreateVerificationSessionRequestDto(
                ExternalSessionId: null,
                "subject-ref",
                "PATIENT_REGISTRATION",
                VerificationProfileDto.StandardEkycProfile,
                [new RequiredCheckRequestDto(RequiredCheckTypeDto.CaptureQuality, Required: true, MinimumConfidence: null)],
                DateTimeOffset.UtcNow.AddMinutes(30),
                RequestId: "req-tip82s",
                CorrelationId: "corr-tip82s"),
            cancellationToken: CancellationToken.None);

        Assert.True(created.IsSuccess, created.Error?.Code);
        return created.Value!;
    }

    private static async Task CancelAsync(TestFixture fixture, string sessionId)
    {
        var cancelled = await fixture.CompletionService.CancelAsync(
            BusinessCaller(),
            sessionId,
            new CancelVerificationSessionRequestDto("caller_void", "req-cancel", "corr-cancel"),
            CancellationToken.None);

        Assert.True(cancelled.IsSuccess, cancelled.Error?.Code);
    }

    private static CaptureArtifactSubmissionRequestDto DeviceMetadataArtifact(string metadataHash) =>
        new(
            CaptureArtifactTypeDto.DeviceCaptureMetadata,
            CaptureSourceDto.PcAgent,
            "ldev_capture",
            "device-1",
            ArtifactHash: null,
            metadataHash,
            RequestId: "req-artifact",
            CorrelationId: "corr-artifact");

    private static CaptureArtifact DomainArtifact(Guid sessionId, string metadataHash) =>
        new(
            Guid.NewGuid(),
            sessionId,
            CaptureArtifactType.DeviceCaptureMetadata,
            CaptureSource.PcAgent,
            "ldev_capture",
            "device-1",
            VaultRef: null,
            ArtifactHash: null,
            MetadataHash: new HashRef(metadataHash),
            CaptureArtifactQualityState.Pending,
            RetryReasonCode: null,
            "req-artifact",
            "corr-artifact",
            DateTimeOffset.UtcNow,
            ExpiresAt: null);

    private static EvidenceResult DomainEvidence(Guid sessionId, string payloadHash) =>
        new(
            Guid.NewGuid(),
            sessionId,
            VerificationCheckId: null,
            EvidenceResultType.CaptureQuality,
            InputCaptureArtifactIds: [],
            VerificationResult.Passed,
            Confidence: 0.9m,
            ReasonCodes: [],
            RetryReasonCode: null,
            SanitizedSummaryRef: "summary:capture-quality",
            PayloadHash: new HashRef(payloadHash),
            SignaturePlaceholderStatus.PlaceholderUnverified,
            "localdev-quality",
            "s1",
            "req-evidence",
            "corr-evidence",
            DateTimeOffset.UtcNow);

    private static AppendIdempotencyRecord IdempotencyRecord(
        Guid sessionId,
        string key,
        string endpointKind,
        string slot) =>
        new(
            sessionId,
            key,
            endpointKind,
            slot,
            Guid.NewGuid(),
            $"sha256:{Guid.NewGuid():N}",
            DateTimeOffset.UtcNow);

    private static TestFixture CreateFixture()
    {
        var sessions = new LocalDevInMemoryVerificationSessionRepository();
        var artifacts = new LocalDevInMemoryCaptureArtifactRepository();
        var evidence = new LocalDevInMemoryEvidenceResultRepository();
        var decisions = new LocalDevInMemoryVerificationDecisionRepository();
        var packages = new LocalDevInMemoryEvidencePackageRepository();
        var manifests = new LocalDevInMemoryEvidenceManifestRepository();
        var audit = new LocalDevInMemoryAuditEventRepository();
        var policies = new LocalDevRuntimePolicySource();
        var idempotency = new LocalDevInMemoryAppendIdempotencyStore(sessions, artifacts, evidence, audit);
        var finalization = new LocalDevInMemoryVerificationFinalizationBoundary(
            sessions,
            decisions,
            packages,
            manifests,
            audit);
        var sessionService = new VerificationSessionApplicationService(sessions, artifacts, evidence, audit, policies);
        var evidenceService = new VerificationEvidenceApplicationService(sessions, artifacts, evidence, audit, policies, idempotency, idempotency);
        var completionService = new VerificationCompletionApplicationService(
            sessions,
            artifacts,
            evidence,
            decisions,
            packages,
            manifests,
            audit,
            new TestEvidenceSigner(),
            finalization);

        return new TestFixture(sessionService, evidenceService, completionService, sessions, artifacts, evidence, audit, idempotency);
    }

    private static AuthenticatedClientContext BusinessCaller(
        IReadOnlySet<string>? scopes = null,
        Guid? clientId = null,
        Guid? apiKeyId = null) =>
        new(
            apiKeyId ?? Guid.Parse("20000000-0000-0000-0000-000000000001"),
            clientId ?? LocalDevRuntimePolicySource.BusinessClientId,
            "ldev_biz",
            AuthenticatedCallerCategory.BusinessConsumer,
            scopes ?? new HashSet<string> { "business.session.create", "business.session.read", "session.complete", "session.cancel" });

    private static AuthenticatedClientContext CaptureCaller() =>
        new(
            Guid.Parse("30000000-0000-0000-0000-000000000001"),
            LocalDevRuntimePolicySource.BusinessClientId,
            "ldev_capture",
            AuthenticatedCallerCategory.CaptureAgent,
            new HashSet<string> { "capture.artifact.append" },
            AllowedCaptureAgentIds: new HashSet<string> { "ldev_capture" });

    private static AuthenticatedClientContext CaptureCallerWithCancelScope() =>
        new(
            Guid.Parse("30000000-0000-0000-0000-000000000002"),
            LocalDevRuntimePolicySource.BusinessClientId,
            "ldev_capture",
            AuthenticatedCallerCategory.CaptureAgent,
            new HashSet<string> { "session.cancel" },
            AllowedCaptureAgentIds: new HashSet<string> { "ldev_capture" });

    private static AuthenticatedClientContext TrustedCallerWithCancelScope() =>
        new(
            Guid.Parse("40000000-0000-0000-0000-000000000002"),
            LocalDevRuntimePolicySource.BusinessClientId,
            "ldev_adapter",
            AuthenticatedCallerCategory.TrustedAdapter,
            new HashSet<string> { "session.cancel" });

    private static void AssertFailure<T>(SessionOperationResult<T> result, string code, int status)
    {
        Assert.False(result.IsSuccess);
        Assert.Equal(code, result.Error!.Code);
        Assert.Equal(status, result.Error.StatusCode);
    }

    private sealed record TestFixture(
        VerificationSessionApplicationService SessionService,
        VerificationEvidenceApplicationService EvidenceService,
        VerificationCompletionApplicationService CompletionService,
        LocalDevInMemoryVerificationSessionRepository Sessions,
        LocalDevInMemoryCaptureArtifactRepository Artifacts,
        LocalDevInMemoryEvidenceResultRepository Evidence,
        LocalDevInMemoryAuditEventRepository Audit,
        LocalDevInMemoryAppendIdempotencyStore Idempotency);
}
