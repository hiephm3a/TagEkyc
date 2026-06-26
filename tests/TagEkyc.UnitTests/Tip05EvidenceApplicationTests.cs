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
    public async Task Tip69_bare_nfc_passed_is_rejected_fail_closed()
    {
        var fixture = CreateFixture();
        var session = await CreateChallengeBoundSessionAsync(fixture, [RequiredCheckTypeDto.DocumentNfc]);
        var nfcArtifact = await fixture.Service.AppendCaptureArtifactAsync(
            CaptureCaller(),
            session.Value!.VerificationSessionId,
            NfcArtifact(),
            CancellationToken.None);

        var barePassed = await fixture.Service.AppendEvidenceResultAsync(
            TrustedCaller(),
            session.Value.VerificationSessionId,
            CaptureQualityEvidence([nfcArtifact.Value!.CaptureArtifactId]) with
            {
                ResultType = EvidenceResultTypeDto.NfcValidation,
                PayloadHash = null,
                NfcEvidenceDecisionBasis = null,
            },
            CancellationToken.None);

        Assert.False(barePassed.IsSuccess);
        Assert.Equal("NFC_EVIDENCE_DECOMPOSITION_REQUIRED", barePassed.Error?.Code);
        Assert.Empty(fixture.Evidence.EvidenceResults);
    }

    [Fact]
    public async Task Tip69_nfc_payload_hash_is_server_owned_and_rejects_adapter_mismatch()
    {
        var fixture = CreateFixture();
        var session = await CreateChallengeBoundSessionAsync(fixture, [RequiredCheckTypeDto.DocumentNfc]);
        var nfcArtifact = await fixture.Service.AppendCaptureArtifactAsync(
            CaptureCaller(),
            session.Value!.VerificationSessionId,
            NfcArtifact(),
            CancellationToken.None);

        var spoofed = await fixture.Service.AppendEvidenceResultAsync(
            TrustedCaller(),
            session.Value.VerificationSessionId,
            ValidNfcEvidence(session.Value.VerificationSessionId, nfcArtifact.Value!.CaptureArtifactId) with
            {
                PayloadHash = "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            },
            CancellationToken.None);

        Assert.False(spoofed.IsSuccess);
        Assert.Equal("NFC_PAYLOAD_HASH_MISMATCH", spoofed.Error?.Code);
        Assert.Empty(fixture.Evidence.EvidenceResults);
    }

    [Fact]
    public async Task Tip69_trusted_nfc_capture_gets_server_stamped_positive_binding_flag()
    {
        var fixture = CreateFixture();
        var session = await CreateChallengeBoundSessionAsync(fixture, [RequiredCheckTypeDto.DocumentNfc]);
        var nfcArtifact = await fixture.Service.AppendCaptureArtifactAsync(
            CaptureCaller(),
            session.Value!.VerificationSessionId,
            NfcArtifact(),
            CancellationToken.None);

        var accepted = await fixture.Service.AppendEvidenceResultAsync(
            TrustedCaller(),
            session.Value.VerificationSessionId,
            ValidNfcEvidence(session.Value.VerificationSessionId, nfcArtifact.Value!.CaptureArtifactId) with
            {
                NfcEvidenceDecisionBasis = ValidNfcBasis(
                    session.Value.VerificationSessionId,
                    nfcArtifact.Value.CaptureArtifactId,
                    includePositiveBindingFlag: false),
            },
            CancellationToken.None);
        var evidence = fixture.Evidence.EvidenceResults.Single();

        Assert.True(accepted.IsSuccess);
        Assert.Equal("ReadyToComplete", accepted.Value?.SessionState);
        Assert.Equal(VerificationResult.Passed, evidence.Result);
        Assert.Equal(ExpectedTrustedPayloadHash(session.Value.VerificationSessionId, nfcArtifact.Value.CaptureArtifactId), evidence.PayloadHash?.ToString());
    }

    [Fact]
    public async Task Tip69_external_prestaged_nfc_passed_is_coerced_to_review_required()
    {
        var fixture = CreateFixture();
        var session = await CreateChallengeBoundSessionAsync(fixture, [RequiredCheckTypeDto.DocumentNfc]);
        var nfcArtifact = await fixture.Service.AppendCaptureArtifactAsync(
            CaptureCaller(),
            session.Value!.VerificationSessionId,
            NfcArtifact(CaptureSourceDto.ExternalPreStaged),
            CancellationToken.None);

        var accepted = await fixture.Service.AppendEvidenceResultAsync(
            TrustedCaller(),
            session.Value.VerificationSessionId,
            ValidNfcEvidence(session.Value.VerificationSessionId, nfcArtifact.Value!.CaptureArtifactId),
            CancellationToken.None);
        var evidence = fixture.Evidence.EvidenceResults.Single();

        Assert.True(accepted.IsSuccess);
        Assert.Equal("InProgress", accepted.Value?.SessionState);
        Assert.Equal(VerificationResult.ReviewRequired, evidence.Result);
        Assert.Contains("DIRECT_CLIENT_UPLOAD_UNTRUSTED", evidence.ReasonCodes);
        Assert.Equal(ExpectedExternalPrestagedPayloadHash(session.Value.VerificationSessionId, nfcArtifact.Value.CaptureArtifactId), evidence.PayloadHash?.ToString());
        Assert.NotEqual(ExpectedTrustedPayloadHash(session.Value.VerificationSessionId, nfcArtifact.Value.CaptureArtifactId), evidence.PayloadHash?.ToString());
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

    private static async Task<SessionOperationResult<CreateVerificationSessionResponseDto>> CreateChallengeBoundSessionAsync(
        TestFixture fixture,
        IReadOnlyList<RequiredCheckTypeDto> checks)
    {
        var request = new CreateVerificationSessionRequestDto(
            $"external-{Guid.NewGuid():N}",
            "subject-ref",
            "SIGNING_AUTH",
            VerificationProfileDto.ChallengeBoundEkycProfile,
            checks.Select(check => new RequiredCheckRequestDto(check, Required: true, MinimumConfidence: null)).ToArray(),
            DateTimeOffset.UtcNow.AddMinutes(30),
            ClientReference: "client-ref-tip69",
            Challenge: "opaque-tip69-challenge",
            RequestId: "req-tip69",
            CorrelationId: "corr-tip69");

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

    private static CaptureArtifactSubmissionRequestDto NfcArtifact(CaptureSourceDto source = CaptureSourceDto.MobileSdk) =>
        new(
            CaptureArtifactTypeDto.NfcReadArtifact,
            source,
            "ldev_capture",
            "device-1",
            "sha256:nfc-artifact",
            MetadataHash: null,
            RequestId: "req-nfc-artifact",
            CorrelationId: "corr-tip69");

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

    private static EvidenceResultSubmissionRequestDto ValidNfcEvidence(
        string sessionId,
        string artifactId) =>
        new(
            EvidenceResultTypeDto.NfcValidation,
            [artifactId],
            VerificationResultDto.Passed,
            Confidence: 0.99m,
            ReasonCodes: [],
            RetryReasonCode: null,
            SanitizedSummaryRef: "summary:nfc-validation",
            PayloadHash: null,
            SignaturePlaceholderStatusDto.PlaceholderUnverified,
            "fixture-nfc",
            "tip69",
            RequestId: "req-nfc-evidence",
            CorrelationId: "corr-tip69",
            NfcEvidenceDecisionBasis: ValidNfcBasis(sessionId, artifactId));

    private static NfcEvidenceDecisionBasisDto ValidNfcBasis(
        string sessionId,
        string artifactId,
        bool includePositiveBindingFlag = true) =>
        new(
            NfcFlags(includePositiveBindingFlag),
            new NfcCaptureBindingDto(
                ChallengeHash(sessionId),
                sessionId,
                "ldev_capture",
                "device-1",
                CapturedAt: Tip69CapturedAt,
                "sha256:nfc-artifact"),
            ServerDecisionResult: null,
            AdapterRequestedResult: VerificationResultDto.Passed,
            "fixture-nfc",
            "tip69",
            [new NfcInputArtifactRefDto(artifactId, "sha256:nfc-artifact")],
            SanitizedSummaryLabel: "summary:nfc-validation",
            new VerificationExtensionDescriptorDto(
                "nfc-validation",
                nameof(RequiredCheckTypeDto.DocumentNfc),
                VerificationExtensionCategoryDto.IdentityEvidence,
                nameof(EvidenceResultTypeDto.NfcValidation)));

    private static IReadOnlyList<string> NfcFlags(bool includePositiveBindingFlag)
    {
        var flags = new List<string>
        {
            "NFC_READ_OK",
            "PACE_SUCCESS",
            "SOD_INTERNAL_VALID",
            "DG_HASHES_MATCH_SOD",
            "CSCA_NOT_VERIFIED",
            "CHIP_AUTH_RESPONSE_VALID",
        };
        if (includePositiveBindingFlag)
        {
            flags.Add("CAPTURE_BOUND_TO_SESSION");
        }

        return flags;
    }

    private static string ExpectedTrustedPayloadHash(string sessionId, string artifactId)
    {
        var basis = Tip69NormalizedBasis(
            sessionId,
            artifactId,
            [
                "CAPTURE_BOUND_TO_SESSION",
                "CHIP_AUTH_RESPONSE_VALID",
                "CSCA_NOT_VERIFIED",
                "DG_HASHES_MATCH_SOD",
                "NFC_READ_OK",
                "PACE_SUCCESS",
                "SOD_INTERNAL_VALID",
            ],
            "Passed");

        return EvidenceCanonicalization.HashCanonical("tip-69-nfc-evidence-decision-basis", basis);
    }

    private static string ExpectedExternalPrestagedPayloadHash(string sessionId, string artifactId)
    {
        var basis = Tip69NormalizedBasis(
            sessionId,
            artifactId,
            [
                "CHIP_AUTH_RESPONSE_VALID",
                "CSCA_NOT_VERIFIED",
                "DG_HASHES_MATCH_SOD",
                "DIRECT_CLIENT_UPLOAD_UNTRUSTED",
                "NFC_READ_OK",
                "PACE_SUCCESS",
                "SOD_INTERNAL_VALID",
            ],
            "ReviewRequired");

        return EvidenceCanonicalization.HashCanonical("tip-69-nfc-evidence-decision-basis", basis);
    }

    private static object Tip69NormalizedBasis(
        string sessionId,
        string artifactId,
        IReadOnlyList<string> flags,
        string serverDecisionResult) =>
        new
        {
            extension = new
            {
                id = "nfc-validation",
                requiredCheckType = "DocumentNfc",
                category = "IdentityEvidence",
                emitsEvidenceType = "NfcValidation",
            },
            flags = flags.ToArray(),
            captureBinding = new
            {
                challengeHash = ChallengeHash(sessionId),
                sessionId,
                captureAgentId = "ldev_capture",
                deviceId = "device-1",
                capturedAt = Tip69CapturedAt,
                artifactHash = "sha256:nfc-artifact",
            },
            serverDecisionResult,
            adapterRequestedResult = "Passed",
            engineName = "fixture-nfc",
            engineVersion = "tip69",
            inputArtifacts = new[]
            {
                new
                {
                    artifactId,
                    artifactHash = "sha256:nfc-artifact",
                },
            },
            sanitizedSummaryLabel = "summary:nfc-validation",
        };

    private static string ChallengeHash(string sessionId) =>
        EvidenceCanonicalization.HashCanonical("tip-69-capture-session-challenge", new
        {
            sessionId,
            challenge = "opaque-tip69-challenge",
        });

    private static readonly DateTimeOffset Tip69CapturedAt = new(2026, 6, 26, 1, 2, 3, TimeSpan.Zero);

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
