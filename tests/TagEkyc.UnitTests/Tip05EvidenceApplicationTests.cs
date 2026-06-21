using Microsoft.AspNetCore.Http;
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

public sealed class Tip05EvidenceApplicationTests
{
    [Fact]
    public async Task Business_consumer_is_wrong_category_before_missing_tip05_scope()
    {
        var fixture = CreateFixture();
        var session = await CreateSessionAsync(fixture, [RequiredCheckTypeDto.CaptureQuality]);

        var result = await fixture.Service.AppendCaptureArtifactAsync(
            BusinessCaller(),
            session.Value!.VerificationSessionId,
            DocumentFrontArtifact(),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("CALLER_CATEGORY_NOT_ALLOWED", result.Error?.Code);
        Assert.Equal(StatusCodes.Status403Forbidden, result.Error?.StatusCode);
    }

    [Fact]
    public async Task Capture_artifact_requires_stable_hash_and_policy_mapping()
    {
        var fixture = CreateFixture();
        var session = await CreateSessionAsync(fixture, [RequiredCheckTypeDto.CaptureQuality]);

        var missingHash = await fixture.Service.AppendCaptureArtifactAsync(
            CaptureCaller(),
            session.Value!.VerificationSessionId,
            DocumentFrontArtifact() with { ArtifactHash = null, MetadataHash = null },
            CancellationToken.None);

        var fingerprint = await fixture.Service.AppendCaptureArtifactAsync(
            CaptureCaller(),
            session.Value!.VerificationSessionId,
            DocumentFrontArtifact() with { ArtifactType = CaptureArtifactTypeDto.FingerprintCapture },
            CancellationToken.None);

        var accepted = await fixture.Service.AppendCaptureArtifactAsync(
            CaptureCaller(),
            session.Value!.VerificationSessionId,
            DeviceMetadataArtifact(),
            CancellationToken.None);

        Assert.Equal("INVALID_CAPTURE_ARTIFACT", missingHash.Error?.Code);
        Assert.Equal("FINGERPRINT_NOT_ENABLED", fingerprint.Error?.Code);
        Assert.True(accepted.IsSuccess);
        Assert.Equal("InProgress", accepted.Value?.SessionState);
        Assert.Single(fixture.Artifacts.Artifacts);
    }

    [Fact]
    public async Task Accepted_capture_artifact_registers_internal_metadata_reference_without_proof_claims()
    {
        var fixture = CreateFixture();
        var session = await CreateSessionAsync(fixture, [RequiredCheckTypeDto.CaptureQuality]);

        var accepted = await fixture.Service.AppendCaptureArtifactAsync(
            CaptureCaller(),
            session.Value!.VerificationSessionId,
            DeviceMetadataArtifact(),
            CancellationToken.None);

        Assert.True(accepted.IsSuccess);
        var referenceId = new MetadataReferenceId($"capture-artifact-metadata:{accepted.Value!.CaptureArtifactId}");
        var registered = await fixture.MetadataReferences.QueryAsync(referenceId);
        var unknown = await fixture.MetadataReferences.QueryAsync(
            new MetadataReferenceId("capture-artifact-metadata:unknown-tip62"));

        Assert.True(registered.HasRegisteredMetadata());
        Assert.False(registered.IsNonSuccess());
        Assert.NotNull(registered.Reference);
        Assert.Equal("capture-artifact-metadata", registered.Reference.ReferenceKind);
        Assert.Equal(MetadataReferenceState.RegisteredMetadata, registered.Reference.State);
        Assert.Equal(new HashRef("sha256:metadata"), registered.Reference.MetadataHash);
        Assert.True(registered.RequiresPacketBeforeReliance());
        Assert.True(registered.IsNotReliableForEvidenceReliance());
        Assert.True(registered.DeniesEvidenceAvailabilityProof());
        Assert.True(registered.DeniesArtifactAccessProof());
        Assert.True(registered.DeniesCompletePackageProof());
        Assert.True(registered.DeniesProviderEvidenceAvailabilityProof());
        Assert.True(registered.DeniesReadinessProof());
        Assert.Null(unknown.Reference);
        Assert.True(unknown.IsNonSuccess());
        Assert.True(unknown.IsNotReliableForEvidenceReliance());
    }

    [Fact]
    public async Task Evidence_requires_payload_hash_input_artifact_and_compatible_artifact_type()
    {
        var fixture = CreateFixture();
        var session = await CreateSessionAsync(fixture, [RequiredCheckTypeDto.CaptureQuality]);
        var artifact = await fixture.Service.AppendCaptureArtifactAsync(
            CaptureCaller(),
            session.Value!.VerificationSessionId,
            DeviceMetadataArtifact(),
            CancellationToken.None);

        var missingPayloadHash = await fixture.Service.AppendEvidenceResultAsync(
            TrustedCaller(),
            session.Value!.VerificationSessionId,
            CaptureQualityEvidence([artifact.Value!.CaptureArtifactId]) with { PayloadHash = null },
            CancellationToken.None);

        var missingInput = await fixture.Service.AppendEvidenceResultAsync(
            TrustedCaller(),
            session.Value!.VerificationSessionId,
            CaptureQualityEvidence([]),
            CancellationToken.None);

        var nfcSession = await CreateSessionAsync(fixture, [RequiredCheckTypeDto.DocumentNfc]);
        var metadataArtifact = await fixture.Service.AppendCaptureArtifactAsync(
            CaptureCaller(),
            nfcSession.Value!.VerificationSessionId,
            DeviceMetadataArtifact(),
            CancellationToken.None);

        var incompatible = await fixture.Service.AppendEvidenceResultAsync(
            TrustedCaller(),
            nfcSession.Value!.VerificationSessionId,
            CaptureQualityEvidence([metadataArtifact.Value!.CaptureArtifactId]) with { ResultType = EvidenceResultTypeDto.NfcValidation },
            CancellationToken.None);

        Assert.Equal("INVALID_HASH_REF", missingPayloadHash.Error?.Code);
        Assert.Equal("INPUT_CAPTURE_ARTIFACTS_REQUIRED", missingInput.Error?.Code);
        Assert.Equal("INVALID_EVIDENCE_RESULT", incompatible.Error?.Code);
    }

    [Fact]
    public async Task Latest_evidence_result_controls_readiness_and_ready_rejects_more_writes()
    {
        var fixture = CreateFixture();
        var session = await CreateSessionAsync(fixture, [RequiredCheckTypeDto.CaptureQuality]);
        var artifact = await fixture.Service.AppendCaptureArtifactAsync(
            CaptureCaller(),
            session.Value!.VerificationSessionId,
            DeviceMetadataArtifact(),
            CancellationToken.None);

        var review = await fixture.Service.AppendEvidenceResultAsync(
            TrustedCaller(),
            session.Value!.VerificationSessionId,
            CaptureQualityEvidence([artifact.Value!.CaptureArtifactId]) with { Result = VerificationResultDto.ReviewRequired },
            CancellationToken.None);

        var passed = await fixture.Service.AppendEvidenceResultAsync(
            TrustedCaller(),
            session.Value!.VerificationSessionId,
            CaptureQualityEvidence([artifact.Value!.CaptureArtifactId]),
            CancellationToken.None);

        var afterReady = await fixture.Service.AppendCaptureArtifactAsync(
            CaptureCaller(),
            session.Value!.VerificationSessionId,
            DeviceMetadataArtifact(),
            CancellationToken.None);

        var summary = await fixture.Sessions.GetAsync(Guid.Parse(session.Value!.VerificationSessionId), CancellationToken.None);

        Assert.True(review.IsSuccess);
        Assert.Equal("InProgress", review.Value?.SessionState);
        Assert.True(passed.IsSuccess);
        Assert.Equal("ReadyToComplete", passed.Value?.SessionState);
        Assert.Equal(VerificationSessionState.ReadyToComplete, summary?.State);
        Assert.Equal("SESSION_READY_TO_COMPLETE", afterReady.Error?.Code);
        Assert.Equal(StatusCodes.Status409Conflict, afterReady.Error?.StatusCode);
        Assert.Contains(fixture.Audit.Events, audit => audit.EventType == "EVIDENCE_RESULT_RECORDED");
    }

    [Fact]
    public async Task Business_summary_after_tip05_ready_keeps_non_final_defaults()
    {
        var fixture = CreateFixture();
        var session = await CreateSessionAsync(fixture, [RequiredCheckTypeDto.CaptureQuality]);
        var artifact = await fixture.Service.AppendCaptureArtifactAsync(
            CaptureCaller(),
            session.Value!.VerificationSessionId,
            DeviceMetadataArtifact(),
            CancellationToken.None);

        await fixture.Service.AppendEvidenceResultAsync(
            TrustedCaller(),
            session.Value!.VerificationSessionId,
            CaptureQualityEvidence([artifact.Value!.CaptureArtifactId]),
            CancellationToken.None);

        var summary = await fixture.SessionService.GetSummaryAsync(
            BusinessCaller(),
            session.Value!.VerificationSessionId,
            CancellationToken.None);

        Assert.True(summary.IsSuccess);
        Assert.Equal(VerificationSessionStateDto.ReadyToComplete, summary.Value?.State);
        Assert.Equal(VerificationResultDto.NotAvailable, summary.Value?.Result);
        Assert.Equal(AssuranceLevelDto.None, summary.Value?.AssuranceLevel);
        Assert.Null(summary.Value?.EvidencePackageId);
        Assert.Null(summary.Value?.EvidencePackageHash);
        Assert.Null(summary.Value?.CompletedAt);
    }

    private static async Task<SessionOperationResult<CreateVerificationSessionResponseDto>> CreateSessionAsync(
        TestFixture fixture,
        IReadOnlyList<RequiredCheckTypeDto> checks)
    {
        var request = new CreateVerificationSessionRequestDto(
            $"external-{Guid.NewGuid():N}",
            "subject-ref",
            "PATIENT_REGISTRATION",
            VerificationProfileDto.StandardEkycProfile,
            checks.Select(check => new RequiredCheckRequestDto(check, Required: true, MinimumConfidence: null)).ToArray(),
            DateTimeOffset.UtcNow.AddMinutes(30),
            RequestId: "req-tip05",
            CorrelationId: "corr-tip05");

        return await fixture.SessionService.CreateAsync(BusinessCaller(), request, cancellationToken: CancellationToken.None);
    }

    private static TestFixture CreateFixture()
    {
        var sessions = new LocalDevInMemoryVerificationSessionRepository();
        var artifacts = new LocalDevInMemoryCaptureArtifactRepository();
        var evidence = new LocalDevInMemoryEvidenceResultRepository();
        var audit = new LocalDevInMemoryAuditEventRepository();
        var policies = new LocalDevRuntimePolicySource();
        var metadataReferences = new LocalDevInMemoryMetadataReferenceRegistry();
        var sessionService = new VerificationSessionApplicationService(sessions, audit, policies);
        var service = new VerificationEvidenceApplicationService(
            sessions,
            artifacts,
            evidence,
            audit,
            policies,
            metadataReferences);

        return new TestFixture(service, sessionService, sessions, artifacts, evidence, audit, metadataReferences);
    }

    private static CaptureArtifactSubmissionRequestDto DocumentFrontArtifact() =>
        new(
            CaptureArtifactTypeDto.DocumentFrontImage,
            CaptureSourceDto.MobileSdk,
            "ldev_capture",
            "device-1",
            "sha256:artifact",
            MetadataHash: null,
            RequestId: "req-artifact",
            CorrelationId: "corr-tip05");

    private static CaptureArtifactSubmissionRequestDto DeviceMetadataArtifact() =>
        new(
            CaptureArtifactTypeDto.DeviceCaptureMetadata,
            CaptureSourceDto.MobileSdk,
            "ldev_capture",
            "device-1",
            ArtifactHash: null,
            MetadataHash: "sha256:metadata",
            RequestId: "req-artifact",
            CorrelationId: "corr-tip05");

    private static EvidenceResultSubmissionRequestDto CaptureQualityEvidence(IReadOnlyList<string> artifactIds) =>
        new(
            EvidenceResultTypeDto.CaptureQuality,
            artifactIds,
            VerificationResultDto.Passed,
            Confidence: 0.9m,
            ReasonCodes: [],
            RetryReasonCode: null,
            SanitizedSummaryRef: "summary:capture-quality",
            PayloadHash: "sha256:payload",
            SignaturePlaceholderStatusDto.PlaceholderUnverified,
            "localdev-quality",
            "s1",
            RequestId: "req-evidence",
            CorrelationId: "corr-tip05");

    private static AuthenticatedClientContext BusinessCaller() =>
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000001"),
            LocalDevRuntimePolicySource.BusinessClientId,
            "ldev_biz",
            AuthenticatedCallerCategory.BusinessConsumer,
            new HashSet<string> { "business.session.create", "business.session.read" });

    private static AuthenticatedClientContext CaptureCaller() =>
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000007"),
            LocalDevRuntimePolicySource.BusinessClientId,
            "ldev_capture",
            AuthenticatedCallerCategory.CaptureAgent,
            new HashSet<string> { "capture.artifact.append" },
            new HashSet<Guid> { LocalDevRuntimePolicySource.BusinessClientId },
            new HashSet<string> { "ldev_capture" });

    private static AuthenticatedClientContext TrustedCaller() =>
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000008"),
            LocalDevRuntimePolicySource.BusinessClientId,
            "ldev_adapter",
            AuthenticatedCallerCategory.TrustedAdapter,
            new HashSet<string> { "trusted.evidence.append" },
            new HashSet<Guid> { LocalDevRuntimePolicySource.BusinessClientId });

    private sealed record TestFixture(
        VerificationEvidenceApplicationService Service,
        VerificationSessionApplicationService SessionService,
        LocalDevInMemoryVerificationSessionRepository Sessions,
        LocalDevInMemoryCaptureArtifactRepository Artifacts,
        LocalDevInMemoryEvidenceResultRepository Evidence,
        LocalDevInMemoryAuditEventRepository Audit,
        LocalDevInMemoryMetadataReferenceRegistry MetadataReferences);
}
