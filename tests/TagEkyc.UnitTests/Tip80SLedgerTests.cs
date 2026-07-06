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

public sealed class Tip80SLedgerTests
{
    [Fact]
    public async Task Ledger_reports_evidence_gate_without_false_all_passed_claim()
    {
        var fixture = CreateFixture();
        var session = await CreateSessionAsync(fixture);
        var artifact = await AppendCaptureArtifactAsync(fixture, session.VerificationSessionId);
        await AppendCaptureQualityEvidenceAsync(
            fixture,
            session.VerificationSessionId,
            artifact.CaptureArtifactId,
            VerificationResultDto.FailedIdentity,
            "sha256:failed-identity-payload");

        var ledger = await fixture.SessionService.GetEvidenceLedgerAsync(
            BusinessCaller(),
            session.VerificationSessionId,
            CancellationToken.None);

        Assert.True(ledger.IsSuccess, ledger.Error?.Code);
        Assert.True(ledger.Value!.EvidenceCompleteEligible);
        Assert.False(ledger.Value.AllRequiredChecksPassed);
        var check = Assert.Single(ledger.Value.RequiredChecks);
        Assert.Equal(RequiredCheckTypeDto.CaptureQuality, check.CheckType);
        Assert.Equal(EvidenceLedgerSubmissionStatusDto.Submitted, check.SubmissionStatus);
        Assert.Equal(VerificationResultDto.FailedIdentity, check.Result);
        Assert.Equal("sha256:failed-identity-payload", check.PayloadHash);
        Assert.NotNull(check.CurrentEvidenceResultId);
    }

    [Fact]
    public async Task Ledger_uses_latest_evidence_per_check_and_surfaces_duplicates()
    {
        var fixture = CreateFixture();
        var session = await CreateSessionAsync(fixture);
        var artifact = await AppendCaptureArtifactAsync(fixture, session.VerificationSessionId);
        await AppendCaptureQualityEvidenceAsync(
            fixture,
            session.VerificationSessionId,
            artifact.CaptureArtifactId,
            VerificationResultDto.Passed,
            "sha256:passed-payload");
        var newer = await AppendPersistedCaptureQualityEvidenceAsync(
            fixture,
            session.VerificationSessionId,
            artifact.CaptureArtifactId,
            VerificationResultDto.RetryRequired,
            "sha256:retry-payload");

        var ledger = await fixture.SessionService.GetEvidenceLedgerAsync(
            BusinessCaller(),
            session.VerificationSessionId,
            CancellationToken.None);

        Assert.True(ledger.IsSuccess, ledger.Error?.Code);
        Assert.True(ledger.Value!.EvidenceCompleteEligible);
        Assert.False(ledger.Value.AllRequiredChecksPassed);
        Assert.Equal(2, ledger.Value.AcceptedEvidenceResults.Count);
        Assert.Contains(ledger.Value.AcceptedEvidenceResults, result => result.PayloadHash == "sha256:passed-payload");
        Assert.Contains(ledger.Value.AcceptedEvidenceResults, result => result.PayloadHash == "sha256:retry-payload");
        var check = Assert.Single(ledger.Value.RequiredChecks);
        Assert.Equal(VerificationResultDto.RetryRequired, check.Result);
        Assert.Equal(newer.Id.ToString("N"), check.CurrentEvidenceResultId);
        Assert.Equal("sha256:retry-payload", check.PayloadHash);
    }

    [Fact]
    public async Task Ledger_successful_read_is_read_only()
    {
        var fixture = CreateFixture();
        var session = await CreateSessionAsync(fixture);
        var artifact = await AppendCaptureArtifactAsync(fixture, session.VerificationSessionId);
        await AppendCaptureQualityEvidenceAsync(
            fixture,
            session.VerificationSessionId,
            artifact.CaptureArtifactId,
            VerificationResultDto.Passed,
            "sha256:passed-payload");
        var sessionId = Guid.Parse(session.VerificationSessionId);
        var before = await fixture.Sessions.GetAsync(sessionId, CancellationToken.None);
        var auditCountBefore = fixture.Audit.Events.Count;

        var ledger = await fixture.SessionService.GetEvidenceLedgerAsync(
            BusinessCaller(),
            session.VerificationSessionId,
            CancellationToken.None);
        var after = await fixture.Sessions.GetAsync(sessionId, CancellationToken.None);

        Assert.True(ledger.IsSuccess, ledger.Error?.Code);
        Assert.Equal(before, after);
        Assert.Equal(auditCountBefore, fixture.Audit.Events.Count);
        Assert.Null(after!.EvidencePackageId);
        Assert.Null(after.EvidencePackageHash);
        Assert.Null(after.ManifestHash);
    }

    [Fact]
    public async Task Ledger_keeps_business_read_scope_boundary()
    {
        var fixture = CreateFixture();
        var session = await CreateSessionAsync(fixture);

        var captureRead = await fixture.SessionService.GetEvidenceLedgerAsync(
            CaptureCallerWithBusinessReadScope(),
            session.VerificationSessionId,
            CancellationToken.None);
        var trustedRead = await fixture.SessionService.GetEvidenceLedgerAsync(
            TrustedCallerWithBusinessReadScope(),
            session.VerificationSessionId,
            CancellationToken.None);
        var crossTenantRead = await fixture.SessionService.GetEvidenceLedgerAsync(
            OtherBusinessCaller(),
            session.VerificationSessionId,
            CancellationToken.None);

        AssertForbidden(captureRead, "CALLER_CATEGORY_NOT_ALLOWED");
        AssertForbidden(trustedRead, "CALLER_CATEGORY_NOT_ALLOWED");
        AssertForbidden(crossTenantRead, "FORBIDDEN_CLIENT_APPLICATION");
        Assert.Contains(fixture.Audit.Events, audit => audit.EventType == "SESSION_ACCESS_DENIED");
    }

    [Fact]
    public async Task Ledger_contract_is_sanitized()
    {
        var fixture = CreateFixture();
        var session = await CreateSessionAsync(fixture);
        var artifact = await AppendCaptureArtifactAsync(fixture, session.VerificationSessionId);
        await AppendCaptureQualityEvidenceAsync(
            fixture,
            session.VerificationSessionId,
            artifact.CaptureArtifactId,
            VerificationResultDto.Passed,
            "sha256:passed-payload");

        var ledger = await fixture.SessionService.GetEvidenceLedgerAsync(
            BusinessCaller(),
            session.VerificationSessionId,
            CancellationToken.None);

        var json = JsonSerializer.Serialize(
            ledger.Value!,
            new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter() },
            });

        Assert.DoesNotContain("VaultRef", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SanitizedSummaryRef", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("InputCaptureArtifactIds", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CaptureAgentId", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DeviceId", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("EvidenceCompleteEligible", json, StringComparison.Ordinal);
        Assert.Contains("AllRequiredChecksPassed", json, StringComparison.Ordinal);
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
                RequestId: "req-tip80s",
                CorrelationId: "corr-tip80s"),
            cancellationToken: CancellationToken.None);

        Assert.True(created.IsSuccess, created.Error?.Code);
        return created.Value!;
    }

    private static async Task<CaptureArtifactSubmissionResponseDto> AppendCaptureArtifactAsync(
        TestFixture fixture,
        string sessionId)
    {
        var result = await fixture.EvidenceService.AppendCaptureArtifactAsync(
            CaptureCaller(),
            sessionId,
            new CaptureArtifactSubmissionRequestDto(
                CaptureArtifactTypeDto.DeviceCaptureMetadata,
                CaptureSourceDto.PcAgent,
                "ldev_capture",
                "device-1",
                ArtifactHash: null,
                "sha256:metadata",
                RequestId: "req-artifact",
                CorrelationId: "corr-artifact"),
            CancellationToken.None,
            $"test-idempotency-{Guid.NewGuid():N}");

        Assert.True(result.IsSuccess, result.Error?.Code);
        return result.Value!;
    }

    private static async Task<EvidenceResultSubmissionResponseDto> AppendCaptureQualityEvidenceAsync(
        TestFixture fixture,
        string sessionId,
        string artifactId,
        VerificationResultDto result,
        string payloadHash)
    {
        var accepted = await fixture.EvidenceService.AppendEvidenceResultAsync(
            TrustedCaller(),
            sessionId,
            new EvidenceResultSubmissionRequestDto(
                EvidenceResultTypeDto.CaptureQuality,
                [artifactId],
                result,
                Confidence: 0.9m,
                ReasonCodes: [],
                RetryReasonCode: result == VerificationResultDto.RetryRequired ? "RECROP" : null,
                SanitizedSummaryRef: "summary:capture-quality",
                payloadHash,
                SignaturePlaceholderStatusDto.PlaceholderUnverified,
                "localdev-quality",
                "s1",
                RequestId: $"req-{payloadHash}",
                CorrelationId: $"corr-{payloadHash}"),
            CancellationToken.None,
            $"test-idempotency-{Guid.NewGuid():N}");

        Assert.True(accepted.IsSuccess, accepted.Error?.Code);
        return accepted.Value!;
    }

    private static async Task<EvidenceResult> AppendPersistedCaptureQualityEvidenceAsync(
        TestFixture fixture,
        string sessionId,
        string artifactId,
        VerificationResultDto result,
        string payloadHash)
    {
        var evidence = new EvidenceResult(
            Guid.NewGuid(),
            Guid.Parse(sessionId),
            VerificationCheckId: null,
            EvidenceResultType.CaptureQuality,
            [Guid.Parse(artifactId)],
            ToDomain(result),
            Confidence: 0.9m,
            ReasonCodes: [],
            RetryReasonCode: result == VerificationResultDto.RetryRequired ? "RECROP" : null,
            SanitizedSummaryRef: "summary:capture-quality",
            new HashRef(payloadHash),
            SignaturePlaceholderStatus.PlaceholderUnverified,
            "localdev-quality",
            "s1",
            RequestId: $"req-{payloadHash}",
            CorrelationId: $"corr-{payloadHash}",
            DateTimeOffset.UtcNow.AddSeconds(1));

        await fixture.Evidence.AppendAsync(evidence, CancellationToken.None);
        return evidence;
    }

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

        return new TestFixture(sessionService, evidenceService, sessions, evidence, audit);
    }

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
            new HashSet<string> { "business.session.read" });

    private static AuthenticatedClientContext CaptureCaller() =>
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000007"),
            LocalDevRuntimePolicySource.BusinessClientId,
            "ldev_capture",
            AuthenticatedCallerCategory.CaptureAgent,
            new HashSet<string> { "capture.artifact.append" },
            new HashSet<Guid> { LocalDevRuntimePolicySource.BusinessClientId },
            new HashSet<string> { "ldev_capture" });

    private static AuthenticatedClientContext CaptureCallerWithBusinessReadScope() =>
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000117"),
            LocalDevRuntimePolicySource.BusinessClientId,
            "ldev_capture_read",
            AuthenticatedCallerCategory.CaptureAgent,
            new HashSet<string> { "business.session.read" },
            new HashSet<Guid> { LocalDevRuntimePolicySource.BusinessClientId },
            new HashSet<string> { "ldev_capture_read" });

    private static AuthenticatedClientContext TrustedCaller() =>
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000008"),
            LocalDevRuntimePolicySource.BusinessClientId,
            "ldev_adapter",
            AuthenticatedCallerCategory.TrustedAdapter,
            new HashSet<string> { "trusted.evidence.append" },
            new HashSet<Guid> { LocalDevRuntimePolicySource.BusinessClientId });

    private static AuthenticatedClientContext TrustedCallerWithBusinessReadScope() =>
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000118"),
            LocalDevRuntimePolicySource.BusinessClientId,
            "ldev_adapter_read",
            AuthenticatedCallerCategory.TrustedAdapter,
            new HashSet<string> { "business.session.read" },
            new HashSet<Guid> { LocalDevRuntimePolicySource.BusinessClientId });

    private static void AssertForbidden<T>(SessionOperationResult<T> result, string expectedCode)
    {
        Assert.False(result.IsSuccess);
        Assert.Equal(expectedCode, result.Error?.Code);
        Assert.Equal(StatusCodes.Status403Forbidden, result.Error?.StatusCode);
    }

    private static VerificationResult ToDomain(VerificationResultDto result) =>
        result switch
        {
            VerificationResultDto.Passed => VerificationResult.Passed,
            VerificationResultDto.RetryRequired => VerificationResult.RetryRequired,
            VerificationResultDto.FailedCaptureQuality => VerificationResult.FailedCaptureQuality,
            VerificationResultDto.FailedIdentity => VerificationResult.FailedIdentity,
            VerificationResultDto.ReviewRequired => VerificationResult.ReviewRequired,
            VerificationResultDto.TechnicalError => VerificationResult.TechnicalError,
            _ => throw new ArgumentOutOfRangeException(nameof(result), result, null),
        };

    private sealed record TestFixture(
        VerificationSessionApplicationService SessionService,
        VerificationEvidenceApplicationService EvidenceService,
        LocalDevInMemoryVerificationSessionRepository Sessions,
        LocalDevInMemoryEvidenceResultRepository Evidence,
        LocalDevInMemoryAuditEventRepository Audit);
}
