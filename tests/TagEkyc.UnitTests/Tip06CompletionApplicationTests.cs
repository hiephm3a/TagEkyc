using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using TagEkyc.Application;
using TagEkyc.Application.LocalDev;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.BusinessConsumer;
using TagEkyc.Contracts.CaptureAgent;
using TagEkyc.Contracts.Common;
using TagEkyc.Contracts.TrustedAdapter;
using TagEkyc.Domain;

namespace TagEkyc.UnitTests;

public sealed class Tip06CompletionApplicationTests
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    [Fact]
    public async Task Complete_calculates_passed_decision_and_stores_deterministic_package_snapshot()
    {
        var fixture = CreateFixture();
        var session = await CreateSessionAsync(fixture, [RequiredCheckTypeDto.CaptureQuality]);
        var artifact = await fixture.EvidenceService.AppendCaptureArtifactAsync(
            CaptureCaller(),
            session.Value!.VerificationSessionId,
            DeviceMetadataArtifact(),
            CancellationToken.None);
        await fixture.EvidenceService.AppendEvidenceResultAsync(
            TrustedCaller(),
            session.Value!.VerificationSessionId,
            CaptureQualityEvidence([artifact.Value!.CaptureArtifactId]),
            CancellationToken.None);

        var completed = await fixture.CompletionService.CompleteAsync(
            BusinessCaller(),
            session.Value.VerificationSessionId,
            new CompleteVerificationSessionRequestDto(
                RequestId: "req-complete",
                CorrelationId: "corr-complete"),
            CancellationToken.None);
        var second = await fixture.CompletionService.CompleteAsync(
            BusinessCaller(),
            session.Value.VerificationSessionId,
            new CompleteVerificationSessionRequestDto(
                RequestId: "req-ignored",
                CorrelationId: "corr-ignored"),
            CancellationToken.None);

        Assert.True(completed.IsSuccess);
        Assert.Equal(VerificationSessionStateDto.Completed, completed.Value?.State);
        Assert.Equal(VerificationResultDto.Passed, completed.Value?.Result);
        Assert.Equal(AssuranceLevelDto.Medium, completed.Value?.AssuranceLevel);
        Assert.False(string.IsNullOrWhiteSpace(completed.Value?.FinalDecisionId));
        Assert.StartsWith("sha256:", completed.Value?.EvidencePackageHash, StringComparison.Ordinal);
        Assert.StartsWith("sha256:", completed.Value?.ManifestHash, StringComparison.Ordinal);
        Assert.Equal("req-complete", completed.Value?.RequestId);
        Assert.Equal("corr-complete", completed.Value?.CorrelationId);
        Assert.Equal(completed.Value?.EvidencePackageId, second.Value?.EvidencePackageId);
        Assert.Single(fixture.Decisions.Decisions);
        Assert.Single(fixture.Packages.Packages);
        Assert.Single(fixture.Manifests.Manifests);
        Assert.Contains(fixture.Audit.Events, audit => audit.EventType == "VERIFICATION_COMPLETED");

        var summary = await fixture.SessionService.GetSummaryAsync(
            BusinessCaller(),
            session.Value.VerificationSessionId,
            CancellationToken.None);

        Assert.True(summary.IsSuccess);
        Assert.Equal(VerificationSessionStateDto.Completed, summary.Value?.State);
        Assert.Equal(VerificationResultDto.Passed, summary.Value?.Result);
        Assert.Equal(completed.Value?.EvidencePackageId, summary.Value?.EvidencePackageId);
        Assert.Equal(completed.Value?.EvidencePackageHash, summary.Value?.EvidencePackageHash);
        Assert.Equal(completed.Value?.ManifestHash, summary.Value?.ManifestHash);
        Assert.Equal("req-complete", summary.Value?.RequestId);
        Assert.Equal("corr-complete", summary.Value?.CorrelationId);
        Assert.NotNull(summary.Value?.CompletedAt);

        var package = await fixture.CompletionService.GetEvidencePackageAsync(
            BusinessCaller(),
            completed.Value!.EvidencePackageId,
            CancellationToken.None);
        var completedJson = JsonSerializer.Serialize(completed.Value, JsonOptions);
        var packageJson = JsonSerializer.Serialize(package.Value, JsonOptions);
        var manifestJson = JsonSerializer.Serialize(fixture.Manifests.Manifests.Single(), JsonOptions);

        Assert.True(package.IsSuccess);
        Assert.Equal(VerificationResultDto.Passed, package.Value?.Result);
        Assert.Equal("req-complete", package.Value?.RequestId);
        Assert.Equal("corr-complete", package.Value?.CorrelationId);
        Assert.Single(package.Value!.EvidenceRefs);
        Assert.Equal(package.Value.EvidenceRefs[0].EvidenceResultId, package.Value.EvidenceRefs[0].Id);
        Assert.Equal(package.Value.EvidenceRefs[0].ResultType, package.Value.EvidenceRefs[0].Type);
        Assert.DoesNotContain("payloadHash", packageJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PayloadHash", packageJson, StringComparison.Ordinal);
        Assert.DoesNotContain("vaultRef", packageJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("clientApplicationId", packageJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("InternalAudit", packageJson, StringComparison.Ordinal);
        AssertTip11MetadataNotExposed(completedJson);
        AssertTip11MetadataNotExposed(packageJson);
        AssertTip11MetadataNotExposed(manifestJson);
        Assert.DoesNotContain(PolicySnapshotId.LocalDevS1Value, manifestJson, StringComparison.Ordinal);
        Assert.Contains(fixture.Manifests.Manifests.Single().EvidenceRefs, evidenceRef => evidenceRef.PayloadHash == "sha256:payload");
    }

    [Fact]
    public async Task Complete_calculates_failed_identity_from_latest_required_evidence()
    {
        var fixture = CreateFixture();
        var session = await CreateSessionAsync(fixture, [RequiredCheckTypeDto.DocumentOcr]);
        var artifact = await fixture.EvidenceService.AppendCaptureArtifactAsync(
            CaptureCaller(),
            session.Value!.VerificationSessionId,
            DocumentFrontArtifact(),
            CancellationToken.None);
        await fixture.EvidenceService.AppendEvidenceResultAsync(
            TrustedCaller(),
            session.Value.VerificationSessionId,
            DocumentOcrEvidence([artifact.Value!.CaptureArtifactId]) with
            {
                Result = VerificationResultDto.FailedIdentity,
                ReasonCodes = ["DOCUMENT_DATA_MISMATCH"],
            },
            CancellationToken.None);

        var completed = await fixture.CompletionService.CompleteAsync(
            BusinessCaller(),
            session.Value.VerificationSessionId,
            new CompleteVerificationSessionRequestDto(),
            CancellationToken.None);

        Assert.True(completed.IsSuccess);
        Assert.Equal(VerificationResultDto.FailedIdentity, completed.Value?.Result);
        Assert.Equal(AssuranceLevelDto.Low, completed.Value?.AssuranceLevel);
        Assert.Equal(VerificationResult.FailedIdentity, fixture.Decisions.Decisions.Single().Result);
        Assert.Contains("DOCUMENT_DATA_MISMATCH", fixture.Decisions.Decisions.Single().DecisionReasonCodes);
    }

    [Fact]
    public async Task Force_review_overrides_passed_decision_without_exposing_evidence_statuses()
    {
        var fixture = CreateFixture();
        var session = await CreateSessionAsync(fixture, [RequiredCheckTypeDto.CaptureQuality]);
        var artifact = await fixture.EvidenceService.AppendCaptureArtifactAsync(
            CaptureCaller(),
            session.Value!.VerificationSessionId,
            DeviceMetadataArtifact(),
            CancellationToken.None);
        await fixture.EvidenceService.AppendEvidenceResultAsync(
            TrustedCaller(),
            session.Value.VerificationSessionId,
            CaptureQualityEvidence([artifact.Value!.CaptureArtifactId]),
            CancellationToken.None);

        var completed = await fixture.CompletionService.CompleteAsync(
            BusinessCaller(),
            session.Value.VerificationSessionId,
            new CompleteVerificationSessionRequestDto(ForceReview: true),
            CancellationToken.None);
        var package = await fixture.CompletionService.GetEvidencePackageAsync(
            BusinessCaller(),
            completed.Value!.EvidencePackageId,
            CancellationToken.None);
        var json = JsonSerializer.Serialize(package.Value, JsonOptions);

        Assert.Equal(VerificationResultDto.ReviewRequired, completed.Value?.Result);
        Assert.Equal(AssuranceLevelDto.Low, completed.Value?.AssuranceLevel);
        Assert.Contains("FORCED_REVIEW", fixture.Decisions.Decisions.Single().DecisionReasonCodes);
        Assert.DoesNotContain("createdAt", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("result\":", JsonSerializer.Serialize(package.Value!.EvidenceRefs, JsonOptions), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("status", JsonSerializer.Serialize(package.Value.EvidenceRefs, JsonOptions), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Complete_rejects_wrong_category_missing_scope_missing_evidence_and_terminal_sessions()
    {
        var fixture = CreateFixture();
        var session = await CreateSessionAsync(fixture, [RequiredCheckTypeDto.CaptureQuality]);

        var wrongCategory = await fixture.CompletionService.CompleteAsync(
            CaptureCaller(),
            session.Value!.VerificationSessionId,
            new CompleteVerificationSessionRequestDto(),
            CancellationToken.None);
        var missingScope = await fixture.CompletionService.CompleteAsync(
            BusinessReadOnlyCaller(),
            session.Value.VerificationSessionId,
            new CompleteVerificationSessionRequestDto(),
            CancellationToken.None);
        var missingEvidence = await fixture.CompletionService.CompleteAsync(
            BusinessCaller(),
            session.Value.VerificationSessionId,
            new CompleteVerificationSessionRequestDto(),
            CancellationToken.None);
        await fixture.Sessions.SetStateAsync(
            Guid.Parse(session.Value.VerificationSessionId),
            VerificationSessionState.TechnicalTerminal,
            CancellationToken.None);
        var terminal = await fixture.CompletionService.CompleteAsync(
            BusinessCaller(),
            session.Value.VerificationSessionId,
            new CompleteVerificationSessionRequestDto(ForceReview: true),
            CancellationToken.None);

        Assert.Equal("CALLER_CATEGORY_NOT_ALLOWED", wrongCategory.Error?.Code);
        Assert.Equal(StatusCodes.Status403Forbidden, wrongCategory.Error?.StatusCode);
        Assert.Equal("MISSING_SCOPE", missingScope.Error?.Code);
        Assert.Equal(StatusCodes.Status403Forbidden, missingScope.Error?.StatusCode);
        Assert.Equal("REQUIRED_EVIDENCE_MISSING", missingEvidence.Error?.Code);
        Assert.Equal(StatusCodes.Status409Conflict, missingEvidence.Error?.StatusCode);
        Assert.Equal("SESSION_TERMINAL", terminal.Error?.Code);
        Assert.Equal(StatusCodes.Status409Conflict, terminal.Error?.StatusCode);
        Assert.Empty(fixture.Packages.Packages);
    }

    [Fact]
    public async Task Package_read_requires_business_read_scope_and_owner()
    {
        var fixture = CreateFixture();
        var completed = await CompleteCaptureQualitySessionAsync(fixture);

        var completeOnly = await fixture.CompletionService.GetEvidencePackageAsync(
            BusinessCompleteOnlyCaller(),
            completed.EvidencePackageId,
            CancellationToken.None);
        var otherClient = await fixture.CompletionService.GetEvidencePackageAsync(
            OtherBusinessCaller(),
            completed.EvidencePackageId,
            CancellationToken.None);

        Assert.Equal("MISSING_SCOPE", completeOnly.Error?.Code);
        Assert.Equal(StatusCodes.Status403Forbidden, completeOnly.Error?.StatusCode);
        Assert.Equal("FORBIDDEN_CLIENT_APPLICATION", otherClient.Error?.Code);
        Assert.Equal(StatusCodes.Status403Forbidden, otherClient.Error?.StatusCode);
    }

    private static async Task<CompleteVerificationSessionResponseDto> CompleteCaptureQualitySessionAsync(TestFixture fixture)
    {
        var session = await CreateSessionAsync(fixture, [RequiredCheckTypeDto.CaptureQuality]);
        var artifact = await fixture.EvidenceService.AppendCaptureArtifactAsync(
            CaptureCaller(),
            session.Value!.VerificationSessionId,
            DeviceMetadataArtifact(),
            CancellationToken.None);
        await fixture.EvidenceService.AppendEvidenceResultAsync(
            TrustedCaller(),
            session.Value.VerificationSessionId,
            CaptureQualityEvidence([artifact.Value!.CaptureArtifactId]),
            CancellationToken.None);
        var completed = await fixture.CompletionService.CompleteAsync(
            BusinessCaller(),
            session.Value.VerificationSessionId,
            new CompleteVerificationSessionRequestDto(),
            CancellationToken.None);

        Assert.True(completed.IsSuccess);
        return completed.Value!;
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
            RequestId: "req-create",
            CorrelationId: "corr-create");

        return await fixture.SessionService.CreateAsync(BusinessCaller(), request, cancellationToken: CancellationToken.None);
    }

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
        var finalization = new LocalDevInMemoryVerificationFinalizationBoundary(
            sessions,
            decisions,
            packages,
            manifests,
            audit);
        var sessionService = new VerificationSessionApplicationService(sessions, audit, policies);
        var evidenceService = new VerificationEvidenceApplicationService(sessions, artifacts, evidence, audit, policies);
        var completionService = new VerificationCompletionApplicationService(
            sessions,
            artifacts,
            evidence,
            decisions,
            packages,
            manifests,
            audit,
            finalization);

        return new TestFixture(
            sessionService,
            evidenceService,
            completionService,
            sessions,
            decisions,
            packages,
            manifests,
            audit);
    }

    private static CaptureArtifactSubmissionRequestDto DeviceMetadataArtifact() =>
        new(
            CaptureArtifactTypeDto.DeviceCaptureMetadata,
            CaptureSourceDto.MobileSdk,
            "ldev_capture",
            "device-1",
            ArtifactHash: null,
            MetadataHash: "sha256:metadata",
            RequestId: "req-artifact",
            CorrelationId: "corr-artifact");

    private static CaptureArtifactSubmissionRequestDto DocumentFrontArtifact() =>
        new(
            CaptureArtifactTypeDto.DocumentFrontImage,
            CaptureSourceDto.MobileSdk,
            "ldev_capture",
            "device-1",
            "sha256:artifact",
            MetadataHash: null,
            RequestId: "req-artifact",
            CorrelationId: "corr-artifact");

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
            CorrelationId: "corr-evidence");

    private static EvidenceResultSubmissionRequestDto DocumentOcrEvidence(IReadOnlyList<string> artifactIds) =>
        CaptureQualityEvidence(artifactIds) with
        {
            ResultType = EvidenceResultTypeDto.DocumentOcr,
            SanitizedSummaryRef = "summary:document-ocr",
        };

    private static AuthenticatedClientContext BusinessCaller() =>
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000001"),
            LocalDevRuntimePolicySource.BusinessClientId,
            "ldev_biz",
            AuthenticatedCallerCategory.BusinessConsumer,
            new HashSet<string> { "business.session.create", "business.session.read", "session.complete" });

    private static AuthenticatedClientContext BusinessReadOnlyCaller() =>
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000003"),
            LocalDevRuntimePolicySource.BusinessClientId,
            "ldev_read",
            AuthenticatedCallerCategory.BusinessConsumer,
            new HashSet<string> { "business.session.read" });

    private static AuthenticatedClientContext BusinessCompleteOnlyCaller() =>
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000009"),
            LocalDevRuntimePolicySource.BusinessClientId,
            "ldev_complete",
            AuthenticatedCallerCategory.BusinessConsumer,
            new HashSet<string> { "business.session.create", "session.complete" });

    private static AuthenticatedClientContext OtherBusinessCaller() =>
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000002"),
            LocalDevRuntimePolicySource.OtherBusinessClientId,
            "ldev_other",
            AuthenticatedCallerCategory.BusinessConsumer,
            new HashSet<string> { "business.session.create", "business.session.read", "session.complete" });

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

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private static void AssertTip11MetadataNotExposed(string json)
    {
        Assert.DoesNotContain("policySnapshot", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("retention", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("legalHold", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("purge", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("deletionEligibility", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("accessAudit", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(PolicySnapshotId.LocalDevS1Value, json, StringComparison.Ordinal);
    }

    private sealed record TestFixture(
        VerificationSessionApplicationService SessionService,
        VerificationEvidenceApplicationService EvidenceService,
        VerificationCompletionApplicationService CompletionService,
        LocalDevInMemoryVerificationSessionRepository Sessions,
        LocalDevInMemoryVerificationDecisionRepository Decisions,
        LocalDevInMemoryEvidencePackageRepository Packages,
        LocalDevInMemoryEvidenceManifestRepository Manifests,
        LocalDevInMemoryAuditEventRepository Audit);
}
