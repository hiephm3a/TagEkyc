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

public sealed class Tip07CompletionNotificationApplicationTests
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    [Fact]
    public async Task Completion_notification_query_maps_completed_session_without_public_dto_leakage()
    {
        var fixture = CreateFixture();
        var session = await CreateSessionAsync(fixture);
        var completed = await CompleteCaptureQualitySessionAsync(fixture, session.Value!.VerificationSessionId);

        var notification = await fixture.CompletionService.GetCompletionNotificationAsync(
            BusinessCaller(),
            session.Value.VerificationSessionId,
            CancellationToken.None);
        var summary = await fixture.SessionService.GetSummaryAsync(
            BusinessCaller(),
            session.Value.VerificationSessionId,
            CancellationToken.None);
        var package = await fixture.CompletionService.GetEvidencePackageAsync(
            BusinessCaller(),
            completed.EvidencePackageId,
            CancellationToken.None);

        Assert.True(notification.IsSuccess);
        Assert.Equal("VERIFICATION_COMPLETED", notification.Value?.EventType);
        Assert.Equal("localdev-not-dispatched", notification.Value?.DeliveryId);
        Assert.Equal(completed.CompletedAt, notification.Value?.SentAt);
        Assert.Equal(completed.CompletedAt, notification.Value?.CompletedAt);
        Assert.Equal(session.Value.VerificationSessionId, notification.Value?.VerificationSessionId);
        Assert.Equal(LocalDevRuntimePolicySource.BusinessClientId.ToString("N"), notification.Value?.ClientApplicationId);
        Assert.Equal(VerificationProfileDto.StandardEkycProfile, notification.Value?.Profile);
        Assert.Equal(session.Value.VerificationSessionId, notification.Value?.VerificationSessionId);
        Assert.Equal(VerificationResultDto.Passed, notification.Value?.Result);
        Assert.Equal(AssuranceLevelDto.Medium, notification.Value?.AssuranceLevel);
        Assert.Equal(completed.EvidencePackageId, notification.Value?.EvidencePackageId);
        Assert.Equal(completed.EvidencePackageHash, notification.Value?.EvidencePackageHash);
        Assert.Equal(completed.ManifestHash, notification.Value?.ManifestHash);
        Assert.Equal("req-complete", notification.Value?.RequestId);
        Assert.Equal("corr-complete", notification.Value?.CorrelationId);
        Assert.Equal(SignaturePlaceholderStatusDto.PlaceholderUnverified, notification.Value?.WebhookSignatureStatus);
        Assert.Equal(SignaturePlaceholderStatusDto.Signed, notification.Value?.EvidencePackageSignatureStatus);

        var notificationJson = JsonSerializer.Serialize(notification.Value, JsonOptions);
        Assert.Contains("clientApplicationId", notificationJson, StringComparison.Ordinal);
        Assert.DoesNotContain("payloadHash", notificationJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("vaultRef", notificationJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("InternalAudit", notificationJson, StringComparison.Ordinal);
        Assert.DoesNotContain("TrustedAdapter", notificationJson, StringComparison.Ordinal);
        Assert.DoesNotContain("CaptureAgent", notificationJson, StringComparison.Ordinal);
        Assert.DoesNotContain("SignFlow", notificationJson, StringComparison.Ordinal);
        AssertTip11MetadataNotExposed(notificationJson);

        Assert.DoesNotContain("clientApplicationId", JsonSerializer.Serialize(completed, JsonOptions), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("clientApplicationId", JsonSerializer.Serialize(summary.Value, JsonOptions), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("clientApplicationId", JsonSerializer.Serialize(package.Value, JsonOptions), StringComparison.OrdinalIgnoreCase);
        AssertTip11MetadataNotExposed(JsonSerializer.Serialize(completed, JsonOptions));
        AssertTip11MetadataNotExposed(JsonSerializer.Serialize(summary.Value, JsonOptions));
        AssertTip11MetadataNotExposed(JsonSerializer.Serialize(package.Value, JsonOptions));
    }

    [Fact]
    public async Task Completion_notification_query_requires_authorized_completed_owner_and_is_idempotent()
    {
        var fixture = CreateFixture();
        var incompleteSession = await CreateSessionAsync(fixture);

        var notCompleted = await fixture.CompletionService.GetCompletionNotificationAsync(
            BusinessCaller(),
            incompleteSession.Value!.VerificationSessionId,
            CancellationToken.None);
        var missingScope = await fixture.CompletionService.GetCompletionNotificationAsync(
            BusinessCompleteOnlyCaller(),
            incompleteSession.Value.VerificationSessionId,
            CancellationToken.None);

        Assert.Equal("SESSION_NOT_COMPLETED", notCompleted.Error?.Code);
        Assert.Equal(StatusCodes.Status409Conflict, notCompleted.Error?.StatusCode);
        Assert.Equal("MISSING_SCOPE", missingScope.Error?.Code);
        Assert.Equal(StatusCodes.Status403Forbidden, missingScope.Error?.StatusCode);

        var completed = await CompleteCaptureQualitySessionAsync(fixture, incompleteSession.Value.VerificationSessionId);
        var decisionCount = fixture.Decisions.Decisions.Count;
        var packageCount = fixture.Packages.Packages.Count;
        var manifestCount = fixture.Manifests.Manifests.Count;
        var auditCount = fixture.Audit.Events.Count;

        var first = await fixture.CompletionService.GetCompletionNotificationAsync(
            BusinessCaller(),
            incompleteSession.Value.VerificationSessionId,
            CancellationToken.None);
        var second = await fixture.CompletionService.GetCompletionNotificationAsync(
            BusinessCaller(),
            incompleteSession.Value.VerificationSessionId,
            CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(completed.EvidencePackageId, first.Value?.EvidencePackageId);
        Assert.Equal(first.Value, second.Value);
        Assert.Equal(decisionCount, fixture.Decisions.Decisions.Count);
        Assert.Equal(packageCount, fixture.Packages.Packages.Count);
        Assert.Equal(manifestCount, fixture.Manifests.Manifests.Count);
        Assert.Equal(auditCount, fixture.Audit.Events.Count);

        var otherClient = await fixture.CompletionService.GetCompletionNotificationAsync(
            OtherBusinessCaller(),
            incompleteSession.Value.VerificationSessionId,
            CancellationToken.None);

        Assert.Equal("FORBIDDEN_CLIENT_APPLICATION", otherClient.Error?.Code);
        Assert.Equal(StatusCodes.Status403Forbidden, otherClient.Error?.StatusCode);
        Assert.Equal(auditCount, fixture.Audit.Events.Count);
    }

    private static async Task<SessionOperationResult<CreateVerificationSessionResponseDto>> CreateSessionAsync(TestFixture fixture)
    {
        var request = new CreateVerificationSessionRequestDto(
            $"external-{Guid.NewGuid():N}",
            "subject-ref",
            "PATIENT_REGISTRATION",
            VerificationProfileDto.StandardEkycProfile,
            [new RequiredCheckRequestDto(RequiredCheckTypeDto.CaptureQuality, Required: true, MinimumConfidence: null)],
            DateTimeOffset.UtcNow.AddMinutes(30),
            RequestId: "req-create",
            CorrelationId: "corr-create");

        return await fixture.SessionService.CreateAsync(BusinessCaller(), request, cancellationToken: CancellationToken.None);
    }

    private static async Task<CompleteVerificationSessionResponseDto> CompleteCaptureQualitySessionAsync(
        TestFixture fixture,
        string verificationSessionId)
    {
        var artifact = await fixture.EvidenceService.AppendCaptureArtifactAsync(
            CaptureCaller(),
            verificationSessionId,
            new CaptureArtifactSubmissionRequestDto(
                CaptureArtifactTypeDto.DeviceCaptureMetadata,
                CaptureSourceDto.MobileSdk,
                "ldev_capture",
                "device-1",
                ArtifactHash: null,
                MetadataHash: "sha256:metadata",
                RequestId: "req-artifact",
                CorrelationId: "corr-artifact"),
            CancellationToken.None);
        await fixture.EvidenceService.AppendEvidenceResultAsync(
            TrustedCaller(),
            verificationSessionId,
            new EvidenceResultSubmissionRequestDto(
                EvidenceResultTypeDto.CaptureQuality,
                [artifact.Value!.CaptureArtifactId],
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
                CorrelationId: "corr-evidence"),
            CancellationToken.None);
        var completed = await fixture.CompletionService.CompleteAsync(
            BusinessCaller(),
            verificationSessionId,
            new CompleteVerificationSessionRequestDto(
                RequestId: "req-complete",
                CorrelationId: "corr-complete"),
            CancellationToken.None);

        Assert.True(completed.IsSuccess);
        return completed.Value!;
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
        var sessionService = new VerificationSessionApplicationService(sessions, artifacts, evidence, audit, policies);
        var evidenceService = new VerificationEvidenceApplicationService(sessions, artifacts, evidence, audit, policies);
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

        return new TestFixture(
            sessionService,
            evidenceService,
            completionService,
            decisions,
            packages,
            manifests,
            audit);
    }

    private static AuthenticatedClientContext BusinessCaller() =>
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000001"),
            LocalDevRuntimePolicySource.BusinessClientId,
            "ldev_biz",
            AuthenticatedCallerCategory.BusinessConsumer,
            new HashSet<string> { "business.session.create", "business.session.read", "session.complete" });

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
        options.Converters.Add(new VerificationProfileDtoJsonConverter());
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
        LocalDevInMemoryVerificationDecisionRepository Decisions,
        LocalDevInMemoryEvidencePackageRepository Packages,
        LocalDevInMemoryEvidenceManifestRepository Manifests,
        LocalDevInMemoryAuditEventRepository Audit);
}
