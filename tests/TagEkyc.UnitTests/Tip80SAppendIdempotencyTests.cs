using Microsoft.EntityFrameworkCore;
using TagEkyc.Application;
using TagEkyc.Application.LocalDev;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.BusinessConsumer;
using TagEkyc.Contracts.CaptureAgent;
using TagEkyc.Contracts.Common;
using TagEkyc.Contracts.TrustedAdapter;
using TagEkyc.Domain;
using TagEkyc.Infrastructure.Persistence;
using TagEkyc.Infrastructure.Persistence.Entities;

namespace TagEkyc.UnitTests;

public sealed class Tip80SAppendIdempotencyTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Service_layer_requires_idempotency_key_for_capture_append(string? idempotencyKey)
    {
        var fixture = CreateFixture();
        var session = await CreateSessionAsync(fixture);

        var result = await fixture.EvidenceService.AppendCaptureArtifactAsync(
            CaptureCaller(),
            session.VerificationSessionId,
            DeviceMetadataArtifact("sha256:metadata"),
            CancellationToken.None,
            idempotencyKey);

        Assert.False(result.IsSuccess);
        Assert.Equal("IDEMPOTENCY_KEY_REQUIRED", result.Error!.Code);
        Assert.Empty(fixture.Artifacts.Artifacts);
        Assert.Empty(fixture.Idempotency.Records);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Service_layer_requires_idempotency_key_for_evidence_append(string? idempotencyKey)
    {
        var fixture = CreateFixture();
        var session = await CreateSessionAsync(fixture);
        var artifact = await AppendCaptureArtifactAsync(fixture, session.VerificationSessionId, "run-required-key|artifact");

        var result = await fixture.EvidenceService.AppendEvidenceResultAsync(
            TrustedCaller(),
            session.VerificationSessionId,
            CaptureQualityEvidence([artifact.Value!.CaptureArtifactId], VerificationResultDto.Passed, "sha256:payload"),
            CancellationToken.None,
            idempotencyKey);

        Assert.False(result.IsSuccess);
        Assert.Equal("IDEMPOTENCY_KEY_REQUIRED", result.Error!.Code);
        Assert.Empty(fixture.Evidence.EvidenceResults);
        Assert.Single(fixture.Idempotency.Records);
    }

    [Fact]
    public async Task Same_key_and_same_normalized_payload_deduplicates_without_second_write()
    {
        var fixture = CreateFixture();
        var session = await CreateSessionAsync(fixture);
        var request = DeviceMetadataArtifact("sha256:metadata");

        var first = await fixture.EvidenceService.AppendCaptureArtifactAsync(
            CaptureCaller(),
            session.VerificationSessionId,
            request,
            CancellationToken.None,
            "run-1|DeviceCaptureMetadata");
        var replay = await fixture.EvidenceService.AppendCaptureArtifactAsync(
            CaptureCaller(),
            session.VerificationSessionId,
            request,
            CancellationToken.None,
            "run-1|DeviceCaptureMetadata");

        Assert.True(first.IsSuccess, first.Error?.Code);
        Assert.True(replay.IsSuccess, replay.Error?.Code);
        Assert.False(first.Value!.Deduplicated);
        Assert.True(replay.Value!.Deduplicated);
        Assert.Equal(first.Value.CaptureArtifactId, replay.Value.CaptureArtifactId);
        Assert.Single(fixture.Artifacts.Artifacts);
        Assert.Contains(fixture.Audit.Events, audit => audit.EventType == "CAPTURE_ARTIFACT_DEDUPLICATED");
    }

    [Fact]
    public async Task Same_key_with_materially_different_payload_returns_409_mismatch()
    {
        var fixture = CreateFixture();
        var session = await CreateSessionAsync(fixture);
        await fixture.EvidenceService.AppendCaptureArtifactAsync(
            CaptureCaller(),
            session.VerificationSessionId,
            DeviceMetadataArtifact("sha256:metadata-a"),
            CancellationToken.None,
            "run-2|DeviceCaptureMetadata");

        var mismatch = await fixture.EvidenceService.AppendCaptureArtifactAsync(
            CaptureCaller(),
            session.VerificationSessionId,
            DeviceMetadataArtifact("sha256:metadata-b"),
            CancellationToken.None,
            "run-2|DeviceCaptureMetadata");

        Assert.False(mismatch.IsSuccess);
        Assert.Equal("IDEMPOTENCY_KEY_PAYLOAD_MISMATCH", mismatch.Error!.Code);
        Assert.Equal(409, mismatch.Error.StatusCode);
        Assert.Single(fixture.Artifacts.Artifacts);
    }

    [Fact]
    public async Task Same_key_with_different_append_slot_returns_409_slot_mismatch()
    {
        var fixture = CreateFixture();
        var session = await CreateSessionAsync(fixture);
        var artifact = await AppendCaptureArtifactAsync(fixture, session.VerificationSessionId, "run-3|shared");

        var mismatch = await fixture.EvidenceService.AppendEvidenceResultAsync(
            TrustedCaller(),
            session.VerificationSessionId,
            CaptureQualityEvidence([artifact.Value!.CaptureArtifactId], VerificationResultDto.Passed, "sha256:payload"),
            CancellationToken.None,
            "run-3|shared");

        Assert.False(mismatch.IsSuccess);
        Assert.Equal("IDEMPOTENCY_KEY_SLOT_MISMATCH", mismatch.Error!.Code);
        Assert.Equal(409, mismatch.Error.StatusCode);
        Assert.Empty(fixture.Evidence.EvidenceResults);
    }

    [Fact]
    public async Task Replay_after_ready_and_terminal_is_recovery_read_but_new_key_is_rejected()
    {
        var fixture = CreateFixture();
        var session = await CreateSessionAsync(fixture);
        var artifact = await AppendCaptureArtifactAsync(fixture, session.VerificationSessionId, "run-4|artifact");
        var evidence = await AppendEvidenceAsync(
            fixture,
            session.VerificationSessionId,
            artifact.Value!.CaptureArtifactId,
            "run-4|evidence");
        Assert.Equal("ReadyToComplete", evidence.Value!.SessionState);

        var replayAfterReady = await fixture.EvidenceService.AppendCaptureArtifactAsync(
            CaptureCaller(),
            session.VerificationSessionId,
            DeviceMetadataArtifact("sha256:metadata"),
            CancellationToken.None,
            "run-4|artifact");
        Assert.True(replayAfterReady.IsSuccess, replayAfterReady.Error?.Code);
        Assert.True(replayAfterReady.Value!.Deduplicated);

        await fixture.Sessions.SetStateAsync(Guid.Parse(session.VerificationSessionId), VerificationSessionState.Completed);
        var replayAfterTerminal = await fixture.EvidenceService.AppendEvidenceResultAsync(
            TrustedCaller(),
            session.VerificationSessionId,
            CaptureQualityEvidence([artifact.Value.CaptureArtifactId], VerificationResultDto.Passed, "sha256:payload"),
            CancellationToken.None,
            "run-4|evidence");
        var newKeyAfterTerminal = await fixture.EvidenceService.AppendCaptureArtifactAsync(
            CaptureCaller(),
            session.VerificationSessionId,
            DeviceMetadataArtifact("sha256:other-metadata"),
            CancellationToken.None,
            "run-4|new-artifact");

        Assert.True(replayAfterTerminal.IsSuccess, replayAfterTerminal.Error?.Code);
        Assert.True(replayAfterTerminal.Value!.Deduplicated);
        Assert.False(newKeyAfterTerminal.IsSuccess);
        Assert.Equal("SESSION_TERMINAL", newKeyAfterTerminal.Error!.Code);
        Assert.Single(fixture.Artifacts.Artifacts);
        Assert.Single(fixture.Evidence.EvidenceResults);
    }

    [Fact]
    public async Task New_key_on_expired_session_is_rejected_after_dedup_lookup()
    {
        var fixture = CreateFixture();
        var expired = VerificationSession.Create(
            LocalDevRuntimePolicySource.BusinessClientId,
            "subject-ref",
            VerificationProfile.StandardEkycProfile,
            "PATIENT_REGISTRATION",
            [RequiredCheckType.CaptureQuality],
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow.AddMinutes(-10),
            requestId: "req-expired",
            correlationId: "corr-expired");
        await fixture.Sessions.AddAsync(expired);

        var result = await fixture.EvidenceService.AppendCaptureArtifactAsync(
            CaptureCaller(),
            expired.Id.ToString("N"),
            DeviceMetadataArtifact("sha256:metadata"),
            CancellationToken.None,
            "run-5|expired-new");

        Assert.False(result.IsSuccess);
        Assert.Equal("SESSION_EXPIRED", result.Error!.Code);
        Assert.Empty(fixture.Artifacts.Artifacts);
        Assert.Empty(fixture.Idempotency.Records);
    }

    [Fact]
    public async Task Concurrent_same_key_has_one_winner_and_one_deduplicated_replay()
    {
        var fixture = CreateFixture();
        var session = await CreateSessionAsync(fixture);
        var request = DeviceMetadataArtifact("sha256:metadata");

        var attempts = await Task.WhenAll(
            fixture.EvidenceService.AppendCaptureArtifactAsync(CaptureCaller(), session.VerificationSessionId, request, CancellationToken.None, "run-6|artifact"),
            fixture.EvidenceService.AppendCaptureArtifactAsync(CaptureCaller(), session.VerificationSessionId, request, CancellationToken.None, "run-6|artifact"));

        Assert.All(attempts, attempt => Assert.True(attempt.IsSuccess, attempt.Error?.Code));
        Assert.Equal(1, attempts.Count(attempt => !attempt.Value!.Deduplicated));
        Assert.Equal(1, attempts.Count(attempt => attempt.Value!.Deduplicated));
        Assert.Single(fixture.Artifacts.Artifacts);
        Assert.Single(fixture.Idempotency.Records);
    }

    [Fact]
    public void Ef_model_defines_unique_session_idempotency_key_constraint()
    {
        var options = new DbContextOptionsBuilder<TagEkycDbContext>()
            .UseNpgsql("Host=localhost;Database=tagekyc_model_test;Username=test;Password=test")
            .Options;
        using var db = new TagEkycDbContext(options);

        var entity = db.Model.FindEntityType(typeof(AppendIdempotencyRecordRow));
        Assert.NotNull(entity);
        Assert.Contains(
            entity!.GetKeys(),
            key => key.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(AppendIdempotencyRecordRow.VerificationSessionId), nameof(AppendIdempotencyRecordRow.IdempotencyKey)]));
        Assert.Contains(
            entity.GetIndexes(),
            index => index.IsUnique &&
                     index.Properties.Select(property => property.Name)
                         .SequenceEqual([nameof(AppendIdempotencyRecordRow.VerificationSessionId), nameof(AppendIdempotencyRecordRow.IdempotencyKey)]));
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
                RequestId: "req-tip80si",
                CorrelationId: "corr-tip80si"),
            cancellationToken: CancellationToken.None);

        Assert.True(created.IsSuccess, created.Error?.Code);
        return created.Value!;
    }

    private static async Task<SessionOperationResult<CaptureArtifactSubmissionResponseDto>> AppendCaptureArtifactAsync(
        TestFixture fixture,
        string sessionId,
        string idempotencyKey) =>
        await fixture.EvidenceService.AppendCaptureArtifactAsync(
            CaptureCaller(),
            sessionId,
            DeviceMetadataArtifact("sha256:metadata"),
            CancellationToken.None,
            idempotencyKey);

    private static async Task<SessionOperationResult<EvidenceResultSubmissionResponseDto>> AppendEvidenceAsync(
        TestFixture fixture,
        string sessionId,
        string artifactId,
        string idempotencyKey) =>
        await fixture.EvidenceService.AppendEvidenceResultAsync(
            TrustedCaller(),
            sessionId,
            CaptureQualityEvidence([artifactId], VerificationResultDto.Passed, "sha256:payload"),
            CancellationToken.None,
            idempotencyKey);

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

    private static EvidenceResultSubmissionRequestDto CaptureQualityEvidence(
        IReadOnlyList<string> artifactIds,
        VerificationResultDto result,
        string payloadHash) =>
        new(
            EvidenceResultTypeDto.CaptureQuality,
            artifactIds,
            result,
            Confidence: 0.9m,
            ReasonCodes: [],
            RetryReasonCode: null,
            SanitizedSummaryRef: "summary:capture-quality",
            payloadHash,
            SignaturePlaceholderStatusDto.PlaceholderUnverified,
            "localdev-quality",
            "s1",
            RequestId: "req-evidence",
            CorrelationId: "corr-evidence");

    private static TestFixture CreateFixture()
    {
        var sessions = new LocalDevInMemoryVerificationSessionRepository();
        var artifacts = new LocalDevInMemoryCaptureArtifactRepository();
        var evidence = new LocalDevInMemoryEvidenceResultRepository();
        var audit = new LocalDevInMemoryAuditEventRepository();
        var policies = new LocalDevRuntimePolicySource();
        var idempotency = new LocalDevInMemoryAppendIdempotencyStore(sessions, artifacts, evidence, audit);
        var sessionService = new VerificationSessionApplicationService(sessions, artifacts, evidence, audit, policies);
        var evidenceService = new VerificationEvidenceApplicationService(sessions, artifacts, evidence, audit, policies, idempotency, idempotency);

        return new TestFixture(sessionService, evidenceService, sessions, artifacts, evidence, audit, idempotency);
    }

    private static AuthenticatedClientContext BusinessCaller() =>
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000001"),
            LocalDevRuntimePolicySource.BusinessClientId,
            "ldev_biz",
            AuthenticatedCallerCategory.BusinessConsumer,
            new HashSet<string> { "business.session.create", "business.session.read", "session.complete" });

    private static AuthenticatedClientContext CaptureCaller() =>
        new(
            Guid.Parse("30000000-0000-0000-0000-000000000001"),
            LocalDevRuntimePolicySource.BusinessClientId,
            "ldev_capture",
            AuthenticatedCallerCategory.CaptureAgent,
            new HashSet<string> { "capture.artifact.append" },
            AllowedCaptureAgentIds: new HashSet<string> { "ldev_capture" });

    private static AuthenticatedClientContext TrustedCaller() =>
        new(
            Guid.Parse("40000000-0000-0000-0000-000000000001"),
            LocalDevRuntimePolicySource.BusinessClientId,
            "ldev_adapter",
            AuthenticatedCallerCategory.TrustedAdapter,
            new HashSet<string> { "trusted.evidence.append" });

    private sealed record TestFixture(
        VerificationSessionApplicationService SessionService,
        VerificationEvidenceApplicationService EvidenceService,
        LocalDevInMemoryVerificationSessionRepository Sessions,
        LocalDevInMemoryCaptureArtifactRepository Artifacts,
        LocalDevInMemoryEvidenceResultRepository Evidence,
        LocalDevInMemoryAuditEventRepository Audit,
        LocalDevInMemoryAppendIdempotencyStore Idempotency);
}
