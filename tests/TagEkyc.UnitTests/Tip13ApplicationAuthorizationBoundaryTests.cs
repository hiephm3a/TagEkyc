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

public sealed class Tip13ApplicationAuthorizationBoundaryTests
{
    [Fact]
    public async Task Business_consumer_can_use_current_owned_business_flow()
    {
        var fixture = CreateFixture();
        var session = await CreateSessionAsync(fixture);

        var summary = await fixture.SessionService.GetSummaryAsync(
            BusinessCaller(),
            session.Value!.VerificationSessionId,
            CancellationToken.None);
        var completed = await CompleteCaptureQualitySessionAsync(fixture, session.Value.VerificationSessionId);
        var package = await fixture.CompletionService.GetEvidencePackageAsync(
            BusinessCaller(),
            completed.EvidencePackageId,
            CancellationToken.None);

        Assert.True(session.IsSuccess);
        Assert.True(summary.IsSuccess);
        Assert.True(package.IsSuccess);
        Assert.Equal(VerificationSessionStateDto.Completed, completed.State);
        Assert.Equal(VerificationResultDto.Passed, completed.Result);
    }

    [Fact]
    public async Task Business_consumer_cannot_append_capture_or_trusted_evidence()
    {
        var fixture = CreateFixture();
        var session = await CreateSessionAsync(fixture);

        var capture = await fixture.EvidenceService.AppendCaptureArtifactAsync(
            BusinessCaller(),
            session.Value!.VerificationSessionId,
            DeviceMetadataArtifact(),
            CancellationToken.None,
            $"test-idempotency-{Guid.NewGuid():N}");
        var evidence = await fixture.EvidenceService.AppendEvidenceResultAsync(
            BusinessCaller(),
            session.Value.VerificationSessionId,
            CaptureQualityEvidence(["00000000000000000000000000000000"]),
            CancellationToken.None,
            $"test-idempotency-{Guid.NewGuid():N}");

        AssertForbidden(capture, "CALLER_CATEGORY_NOT_ALLOWED");
        AssertForbidden(evidence, "CALLER_CATEGORY_NOT_ALLOWED");
    }

    [Fact]
    public async Task Capture_agent_keeps_capture_write_only_boundary()
    {
        var fixture = CreateFixture();
        var session = await CreateSessionAsync(fixture);
        var completed = await CompleteCaptureQualitySessionAsync(fixture, session.Value!.VerificationSessionId);

        var create = await fixture.SessionService.CreateAsync(
            CaptureCaller(),
            StandardRequest(),
            cancellationToken: CancellationToken.None);
        var read = await fixture.SessionService.GetSummaryAsync(
            CaptureCaller(),
            session.Value.VerificationSessionId,
            CancellationToken.None);
        var complete = await fixture.CompletionService.CompleteAsync(
            CaptureCaller(),
            session.Value.VerificationSessionId,
            new CompleteVerificationSessionRequestDto(),
            CancellationToken.None);
        var package = await fixture.CompletionService.GetEvidencePackageAsync(
            CaptureCaller(),
            completed.EvidencePackageId,
            CancellationToken.None);
        var trustedEvidence = await fixture.EvidenceService.AppendEvidenceResultAsync(
            CaptureCaller(),
            session.Value.VerificationSessionId,
            CaptureQualityEvidence(["00000000000000000000000000000000"]),
            CancellationToken.None,
            $"test-idempotency-{Guid.NewGuid():N}");

        AssertForbidden(create, "MISSING_SCOPE");
        AssertForbidden(read, "MISSING_SCOPE");
        AssertForbidden(complete, "CALLER_CATEGORY_NOT_ALLOWED");
        AssertForbidden(package, "CALLER_CATEGORY_NOT_ALLOWED");
        AssertForbidden(trustedEvidence, "CALLER_CATEGORY_NOT_ALLOWED");
    }

    [Fact]
    public async Task Trusted_adapter_keeps_trusted_evidence_write_only_boundary()
    {
        var fixture = CreateFixture();
        var session = await CreateSessionAsync(fixture);
        var artifact = await fixture.EvidenceService.AppendCaptureArtifactAsync(
            CaptureCaller(),
            session.Value!.VerificationSessionId,
            DeviceMetadataArtifact(),
            CancellationToken.None,
            $"test-idempotency-{Guid.NewGuid():N}");
        Assert.True(artifact.IsSuccess);

        var trustedEvidence = await fixture.EvidenceService.AppendEvidenceResultAsync(
            TrustedCaller(),
            session.Value.VerificationSessionId,
            CaptureQualityEvidence([artifact.Value!.CaptureArtifactId]) with { RetryReasonCode = "TRUSTED-BOUNDARY-RETRY" },
            CancellationToken.None,
            $"test-idempotency-{Guid.NewGuid():N}");
        var completed = await fixture.CompletionService.CompleteAsync(
            BusinessCaller(),
            session.Value.VerificationSessionId,
            new CompleteVerificationSessionRequestDto(
                RequestId: "req-complete",
                CorrelationId: "corr-complete"),
            CancellationToken.None);
        var create = await fixture.SessionService.CreateAsync(
            TrustedCaller(),
            StandardRequest(),
            cancellationToken: CancellationToken.None);
        var read = await fixture.SessionService.GetSummaryAsync(
            TrustedCaller(),
            session.Value.VerificationSessionId,
            CancellationToken.None);
        var complete = await fixture.CompletionService.CompleteAsync(
            TrustedCaller(),
            session.Value.VerificationSessionId,
            new CompleteVerificationSessionRequestDto(),
            CancellationToken.None);
        var package = await fixture.CompletionService.GetEvidencePackageAsync(
            TrustedCaller(),
            completed.Value!.EvidencePackageId,
            CancellationToken.None);
        var capture = await fixture.EvidenceService.AppendCaptureArtifactAsync(
            TrustedCaller(),
            session.Value.VerificationSessionId,
            DeviceMetadataArtifact(),
            CancellationToken.None,
            $"test-idempotency-{Guid.NewGuid():N}");

        Assert.True(trustedEvidence.IsSuccess);
        Assert.True(completed.IsSuccess);
        AssertForbidden(create, "MISSING_SCOPE");
        AssertForbidden(read, "MISSING_SCOPE");
        AssertForbidden(complete, "CALLER_CATEGORY_NOT_ALLOWED");
        AssertForbidden(package, "CALLER_CATEGORY_NOT_ALLOWED");
        AssertForbidden(capture, "CALLER_CATEGORY_NOT_ALLOWED");
    }

    [Fact]
    public async Task Cross_client_access_remains_denied_for_business_capture_and_trusted_callers()
    {
        var fixture = CreateFixture();
        var session = await CreateSessionAsync(fixture);
        var completed = await CompleteCaptureQualitySessionAsync(fixture, session.Value!.VerificationSessionId);

        var businessRead = await fixture.SessionService.GetSummaryAsync(
            OtherBusinessCaller(),
            session.Value.VerificationSessionId,
            CancellationToken.None);
        var packageRead = await fixture.CompletionService.GetEvidencePackageAsync(
            OtherBusinessCaller(),
            completed.EvidencePackageId,
            CancellationToken.None);
        var captureWrite = await fixture.EvidenceService.AppendCaptureArtifactAsync(
            CaptureCallerForOtherClientWithoutBinding(),
            session.Value.VerificationSessionId,
            DeviceMetadataArtifact(),
            CancellationToken.None,
            $"test-idempotency-{Guid.NewGuid():N}");
        var trustedWrite = await fixture.EvidenceService.AppendEvidenceResultAsync(
            TrustedCallerForOtherClientWithoutBinding(),
            session.Value.VerificationSessionId,
            CaptureQualityEvidence(["00000000000000000000000000000000"]),
            CancellationToken.None,
            $"test-idempotency-{Guid.NewGuid():N}");

        AssertForbidden(businessRead, "FORBIDDEN_CLIENT_APPLICATION");
        AssertForbidden(packageRead, "FORBIDDEN_CLIENT_APPLICATION");
        AssertForbidden(captureWrite, "SESSION_ACCESS_DENIED");
        AssertForbidden(trustedWrite, "SESSION_ACCESS_DENIED");
    }

    [Fact]
    public async Task Reserved_operator_admin_does_not_gain_runtime_behavior()
    {
        var fixture = CreateFixture();
        var session = await CreateSessionAsync(fixture);
        var completed = await CompleteCaptureQualitySessionAsync(fixture, session.Value!.VerificationSessionId);
        var reserved = ReservedOperatorCallerWithAllKnownScopes();

        var create = await fixture.SessionService.CreateAsync(
            reserved,
            StandardRequest(),
            cancellationToken: CancellationToken.None);
        var read = await fixture.SessionService.GetSummaryAsync(
            reserved,
            session.Value.VerificationSessionId,
            CancellationToken.None);
        var complete = await fixture.CompletionService.CompleteAsync(
            reserved,
            session.Value.VerificationSessionId,
            new CompleteVerificationSessionRequestDto(),
            CancellationToken.None);
        var package = await fixture.CompletionService.GetEvidencePackageAsync(
            reserved,
            completed.EvidencePackageId,
            CancellationToken.None);
        var capture = await fixture.EvidenceService.AppendCaptureArtifactAsync(
            reserved,
            session.Value.VerificationSessionId,
            DeviceMetadataArtifact(),
            CancellationToken.None,
            $"test-idempotency-{Guid.NewGuid():N}");
        var evidence = await fixture.EvidenceService.AppendEvidenceResultAsync(
            reserved,
            session.Value.VerificationSessionId,
            CaptureQualityEvidence(["00000000000000000000000000000000"]),
            CancellationToken.None,
            $"test-idempotency-{Guid.NewGuid():N}");

        AssertForbidden(create, "CALLER_CATEGORY_NOT_ALLOWED");
        AssertForbidden(read, "CALLER_CATEGORY_NOT_ALLOWED");
        AssertForbidden(complete, "CALLER_CATEGORY_NOT_ALLOWED");
        AssertForbidden(package, "CALLER_CATEGORY_NOT_ALLOWED");
        AssertForbidden(capture, "CALLER_CATEGORY_NOT_ALLOWED");
        AssertForbidden(evidence, "CALLER_CATEGORY_NOT_ALLOWED");
    }

    [Fact]
    public async Task Non_business_callers_with_business_create_scope_are_denied_by_category()
    {
        var fixture = CreateFixture();

        var captureCreate = await fixture.SessionService.CreateAsync(
            CaptureCallerWithBusinessScopes("business.session.create"),
            StandardRequest(),
            cancellationToken: CancellationToken.None);
        var trustedCreate = await fixture.SessionService.CreateAsync(
            TrustedCallerWithBusinessScopes("business.session.create"),
            StandardRequest(),
            cancellationToken: CancellationToken.None);

        AssertForbidden(captureCreate, "CALLER_CATEGORY_NOT_ALLOWED");
        AssertForbidden(trustedCreate, "CALLER_CATEGORY_NOT_ALLOWED");
    }

    [Fact]
    public async Task Non_business_callers_with_business_read_scope_are_denied_by_category()
    {
        var fixture = CreateFixture();
        var session = await CreateSessionAsync(fixture);

        var captureRead = await fixture.SessionService.GetSummaryAsync(
            CaptureCallerWithBusinessScopes("business.session.read"),
            session.Value!.VerificationSessionId,
            CancellationToken.None);
        var trustedRead = await fixture.SessionService.GetSummaryAsync(
            TrustedCallerWithBusinessScopes("business.session.read"),
            session.Value.VerificationSessionId,
            CancellationToken.None);

        AssertForbidden(captureRead, "CALLER_CATEGORY_NOT_ALLOWED");
        AssertForbidden(trustedRead, "CALLER_CATEGORY_NOT_ALLOWED");
    }

    [Fact]
    public void Localdev_runtime_keys_remain_current_actor_scope_profiles_only()
    {
        var keys = new LocalDevApiKeyStore().ApiKeys;

        Assert.DoesNotContain(keys, key => key.CallerCategory == AuthenticatedCallerCategory.OperatorAdmin);
        Assert.Contains(keys, key =>
            key.CallerCategory == AuthenticatedCallerCategory.BusinessConsumer &&
            key.Scopes.SetEquals(["business.session.create", "business.session.read", "session.complete", "session.cancel"]));
        Assert.Contains(keys, key =>
            key.CallerCategory == AuthenticatedCallerCategory.CaptureAgent &&
            key.Scopes.SetEquals(["capture.artifact.append"]));
        Assert.Contains(keys, key =>
            key.CallerCategory == AuthenticatedCallerCategory.TrustedAdapter &&
            key.Scopes.SetEquals(["trusted.evidence.append"]));
    }

    private static async Task<SessionOperationResult<CreateVerificationSessionResponseDto>> CreateSessionAsync(TestFixture fixture) =>
        await fixture.SessionService.CreateAsync(
            BusinessCaller(),
            StandardRequest(),
            cancellationToken: CancellationToken.None);

    private static async Task<CompleteVerificationSessionResponseDto> CompleteCaptureQualitySessionAsync(
        TestFixture fixture,
        string verificationSessionId)
    {
        var artifact = await fixture.EvidenceService.AppendCaptureArtifactAsync(
            CaptureCaller(),
            verificationSessionId,
            DeviceMetadataArtifact(),
            CancellationToken.None,
            $"test-idempotency-{Guid.NewGuid():N}");

        Assert.True(artifact.IsSuccess);
        return await CompleteSessionWithExistingArtifactAsync(fixture, verificationSessionId, artifact.Value!.CaptureArtifactId);
    }

    private static async Task<CompleteVerificationSessionResponseDto> CompleteSessionWithExistingArtifactAsync(
        TestFixture fixture,
        string verificationSessionId,
        string captureArtifactId)
    {
        var evidence = await fixture.EvidenceService.AppendEvidenceResultAsync(
            TrustedCaller(),
            verificationSessionId,
            CaptureQualityEvidence([captureArtifactId]),
            CancellationToken.None,
            $"test-idempotency-{Guid.NewGuid():N}");
        var completed = await fixture.CompletionService.CompleteAsync(
            BusinessCaller(),
            verificationSessionId,
            new CompleteVerificationSessionRequestDto(
                RequestId: "req-complete",
                CorrelationId: "corr-complete"),
            CancellationToken.None);

        Assert.True(evidence.IsSuccess);
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

        return new TestFixture(sessionService, evidenceService, completionService);
    }

    private static CreateVerificationSessionRequestDto StandardRequest() =>
        new(
            $"external-{Guid.NewGuid():N}",
            "subject-ref",
            "PATIENT_REGISTRATION",
            VerificationProfileDto.StandardEkycProfile,
            [new RequiredCheckRequestDto(RequiredCheckTypeDto.CaptureQuality, Required: true, MinimumConfidence: null)],
            DateTimeOffset.UtcNow.AddMinutes(30),
            RequestId: "req-tip13",
            CorrelationId: "corr-tip13");

    private static CaptureArtifactSubmissionRequestDto DeviceMetadataArtifact() =>
        new(
            CaptureArtifactTypeDto.DeviceCaptureMetadata,
            CaptureSourceDto.MobileSdk,
            "ldev_capture",
            "device-1",
            ArtifactHash: null,
            MetadataHash: "sha256:metadata",
            RequestId: "req-artifact",
            CorrelationId: "corr-tip13");

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
            CorrelationId: "corr-tip13");

    private static AuthenticatedClientContext BusinessCaller() =>
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000001"),
            LocalDevRuntimePolicySource.BusinessClientId,
            "ldev_biz",
            AuthenticatedCallerCategory.BusinessConsumer,
            new HashSet<string> { "business.session.create", "business.session.read", "session.complete" });

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

    private static AuthenticatedClientContext CaptureCallerWithBusinessScopes(params string[] scopes) =>
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000117"),
            LocalDevRuntimePolicySource.BusinessClientId,
            "ldev_capture_business_scope",
            AuthenticatedCallerCategory.CaptureAgent,
            new HashSet<string>(scopes),
            new HashSet<Guid> { LocalDevRuntimePolicySource.BusinessClientId },
            new HashSet<string> { "ldev_capture_business_scope" });

    private static AuthenticatedClientContext TrustedCaller() =>
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000008"),
            LocalDevRuntimePolicySource.BusinessClientId,
            "ldev_adapter",
            AuthenticatedCallerCategory.TrustedAdapter,
            new HashSet<string> { "trusted.evidence.append" },
            new HashSet<Guid> { LocalDevRuntimePolicySource.BusinessClientId });

    private static AuthenticatedClientContext TrustedCallerWithBusinessScopes(params string[] scopes) =>
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000118"),
            LocalDevRuntimePolicySource.BusinessClientId,
            "ldev_adapter_business_scope",
            AuthenticatedCallerCategory.TrustedAdapter,
            new HashSet<string>(scopes),
            new HashSet<Guid> { LocalDevRuntimePolicySource.BusinessClientId });

    private static AuthenticatedClientContext CaptureCallerForOtherClientWithoutBinding() =>
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000107"),
            LocalDevRuntimePolicySource.OtherBusinessClientId,
            "ldev_capture_other",
            AuthenticatedCallerCategory.CaptureAgent,
            new HashSet<string> { "capture.artifact.append" },
            AllowedClientApplicationIds: null,
            AllowedCaptureAgentIds: new HashSet<string> { "ldev_capture_other" });

    private static AuthenticatedClientContext TrustedCallerForOtherClientWithoutBinding() =>
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000108"),
            LocalDevRuntimePolicySource.OtherBusinessClientId,
            "ldev_adapter_other",
            AuthenticatedCallerCategory.TrustedAdapter,
            new HashSet<string> { "trusted.evidence.append" },
            AllowedClientApplicationIds: null);

    private static AuthenticatedClientContext ReservedOperatorCallerWithAllKnownScopes() =>
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000199"),
            LocalDevRuntimePolicySource.BusinessClientId,
            "ldev_operator",
            AuthenticatedCallerCategory.OperatorAdmin,
            new HashSet<string>
            {
                "business.session.create",
                "business.session.read",
                "session.complete",
                "session.cancel",
                "capture.artifact.append",
                "trusted.evidence.append",
            },
            new HashSet<Guid> { LocalDevRuntimePolicySource.BusinessClientId },
            new HashSet<string> { "ldev_operator" });

    private static void AssertForbidden<T>(SessionOperationResult<T> result, string expectedCode)
    {
        Assert.False(result.IsSuccess);
        Assert.Equal(expectedCode, result.Error?.Code);
        Assert.Equal(StatusCodes.Status403Forbidden, result.Error?.StatusCode);
    }

    private sealed record TestFixture(
        VerificationSessionApplicationService SessionService,
        VerificationEvidenceApplicationService EvidenceService,
        VerificationCompletionApplicationService CompletionService);
}
