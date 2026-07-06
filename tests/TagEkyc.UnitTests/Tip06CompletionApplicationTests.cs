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
            CancellationToken.None,
            $"test-idempotency-{Guid.NewGuid():N}");
        await fixture.EvidenceService.AppendEvidenceResultAsync(
            TrustedCaller(),
            session.Value!.VerificationSessionId,
            CaptureQualityEvidence([artifact.Value!.CaptureArtifactId]),
            CancellationToken.None,
            $"test-idempotency-{Guid.NewGuid():N}");

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
        var manifest = fixture.Manifests.Manifests.Single();
        var manifestJson = JsonSerializer.Serialize(manifest, JsonOptions);

        Assert.True(package.IsSuccess);
        Assert.Equal(VerificationResultDto.Passed, package.Value?.Result);
        Assert.Equal("req-complete", package.Value?.RequestId);
        Assert.Equal("corr-complete", package.Value?.CorrelationId);
        Assert.Single(package.Value!.EvidenceRefs);
        Assert.Equal(package.Value.EvidenceRefs[0].EvidenceResultId, package.Value.EvidenceRefs[0].Id);
        Assert.Equal(package.Value.EvidenceRefs[0].ResultType, package.Value.EvidenceRefs[0].Type);
        Assert.Equal(EvidenceCanonicalization.PackageVersion, package.Value.PackageVersion);
        Assert.Equal(EvidenceCanonicalization.PackageVersion, fixture.Packages.Packages.Single().PackageVersion);
        Assert.Equal(EvidenceCanonicalization.CanonicalizationScheme, fixture.Packages.Packages.Single().CanonicalizationScheme);
        Assert.Equal(EvidenceCanonicalization.HashAlgorithm, fixture.Packages.Packages.Single().HashAlgorithm);
        Assert.Equal(EvidenceCanonicalization.PackageVersion, manifest.PackageVersion);
        Assert.Equal(EvidenceCanonicalization.CanonicalizationScheme, manifest.CanonicalizationScheme);
        Assert.Equal(EvidenceCanonicalization.HashAlgorithm, manifest.HashAlgorithm);
        Assert.DoesNotContain("payloadHash", packageJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PayloadHash", packageJson, StringComparison.Ordinal);
        Assert.DoesNotContain("vaultRef", packageJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("canonicalizationScheme", packageJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hashAlgorithm", packageJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("canonicalizationScheme", manifestJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("hashAlgorithm", manifestJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("clientApplicationId", packageJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("InternalAudit", packageJson, StringComparison.Ordinal);
        AssertTip11MetadataNotExposed(completedJson);
        AssertTip11MetadataNotExposed(packageJson);
        AssertTip11MetadataNotExposed(manifestJson);
        Assert.DoesNotContain(PolicySnapshotId.LocalDevS1Value, manifestJson, StringComparison.Ordinal);
        Assert.Contains(manifest.EvidenceRefs, evidenceRef => evidenceRef.PayloadHash == "sha256:payload");
    }

    [Fact]
    public async Task Tip69_nfc_decision_basis_payload_hash_is_proof_bound_without_reason_codes()
    {
        var fixture = CreateFixture();
        var session = await CreateChallengeBoundSessionAsync(fixture, [RequiredCheckTypeDto.DocumentNfc]);
        var nfcArtifact = await fixture.EvidenceService.AppendCaptureArtifactAsync(
            CaptureCaller(),
            session.Value!.VerificationSessionId,
            NfcArtifact(),
            CancellationToken.None,
            $"test-idempotency-{Guid.NewGuid():N}");
        var expectedPayloadHash = ExpectedNfcPayloadHash(session.Value.VerificationSessionId, nfcArtifact.Value!.CaptureArtifactId);
        var mutatedPayloadHash = ExpectedNfcPayloadHash(
            session.Value.VerificationSessionId,
            nfcArtifact.Value.CaptureArtifactId,
            [
                "CAPTURE_BOUND_TO_SESSION",
                "CHIP_AUTH_RESPONSE_VALID",
                "CSCA_VERIFIED",
                "DG_HASHES_MATCH_SOD",
                "NFC_READ_OK",
                "PACE_SUCCESS",
                "SOD_INTERNAL_VALID",
            ]);

        var evidenceAccepted = await fixture.EvidenceService.AppendEvidenceResultAsync(
            TrustedCaller(),
            session.Value.VerificationSessionId,
            NfcEvidence(session.Value.VerificationSessionId, nfcArtifact.Value.CaptureArtifactId) with
            {
                ReasonCodes = ["OPERATIONAL_REASON_ONLY"],
            },
            CancellationToken.None,
            $"test-idempotency-{Guid.NewGuid():N}");
        var completed = await fixture.CompletionService.CompleteAsync(
            BusinessCaller(),
            session.Value.VerificationSessionId,
            new CompleteVerificationSessionRequestDto(),
            CancellationToken.None);
        var manifest = fixture.Manifests.Manifests.Single();
        var nfcRef = manifest.EvidenceRefs.Single(evidenceRef => evidenceRef.Type == "NfcValidation");
        var mutatedManifestHash = RecomputeSingleEvidenceManifestHash(
            manifest,
            completed.Value!,
            mutatedPayloadHash);

        Assert.True(evidenceAccepted.IsSuccess);
        Assert.True(completed.IsSuccess);
        Assert.Equal(VerificationResultDto.Passed, completed.Value?.Result);
        Assert.Contains("OPERATIONAL_REASON_ONLY", fixture.Decisions.Decisions.Single().DecisionReasonCodes);
        Assert.Equal(expectedPayloadHash, nfcRef.PayloadHash);
        Assert.NotEqual(expectedPayloadHash, mutatedPayloadHash);
        Assert.NotEqual(manifest.ManifestHash, mutatedManifestHash);
        Assert.DoesNotContain("OPERATIONAL_REASON_ONLY", JsonSerializer.Serialize(manifest.EvidenceRefs, JsonOptions), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Tip70_face_match_decision_basis_payload_hash_is_proof_bound_without_reason_codes()
    {
        var fixture = CreateFixture();
        var session = await CreateChallengeBoundSessionAsync(
            fixture,
            [RequiredCheckTypeDto.DocumentNfc, RequiredCheckTypeDto.FaceMatch]);
        var nfcArtifact = await fixture.EvidenceService.AppendCaptureArtifactAsync(
            CaptureCaller(),
            session.Value!.VerificationSessionId,
            NfcArtifact(),
            CancellationToken.None,
            $"test-idempotency-{Guid.NewGuid():N}");
        var selfieArtifact = await fixture.EvidenceService.AppendCaptureArtifactAsync(
            CaptureCaller(),
            session.Value.VerificationSessionId,
            SelfieArtifact(),
            CancellationToken.None,
            $"test-idempotency-{Guid.NewGuid():N}");
        await fixture.EvidenceService.AppendEvidenceResultAsync(
            TrustedCaller(),
            session.Value.VerificationSessionId,
            NfcEvidence(session.Value.VerificationSessionId, nfcArtifact.Value!.CaptureArtifactId),
            CancellationToken.None,
            $"test-idempotency-{Guid.NewGuid():N}");
        var nfcEvidence = fixture.Evidence.EvidenceResults.Single(evidence => evidence.ResultType == EvidenceResultType.NfcValidation);
        var scenario = new FaceMatchScenario(
            session.Value.VerificationSessionId,
            nfcArtifact.Value.CaptureArtifactId,
            nfcEvidence.Id.ToString("N"),
            nfcEvidence.PayloadHash!.ToString()!,
            selfieArtifact.Value!.CaptureArtifactId);
        var expectedPayloadHash = ExpectedFaceMatchPayloadHash(scenario);
        var mutatedPayloadHash = ExpectedFaceMatchPayloadHash(
            scenario,
            score: 0.93m);

        var evidenceAccepted = await fixture.EvidenceService.AppendEvidenceResultAsync(
            TrustedCaller(),
            session.Value.VerificationSessionId,
            FaceMatchEvidence(scenario) with
            {
                ReasonCodes = ["OPERATIONAL_REASON_ONLY"],
            },
            CancellationToken.None,
            $"test-idempotency-{Guid.NewGuid():N}");
        var completed = await fixture.CompletionService.CompleteAsync(
            BusinessCaller(),
            session.Value.VerificationSessionId,
            new CompleteVerificationSessionRequestDto(),
            CancellationToken.None);
        var manifest = fixture.Manifests.Manifests.Single();
        var faceMatchRef = manifest.EvidenceRefs.Single(evidenceRef => evidenceRef.Type == "FaceMatch");
        var mutatedManifestHash = RecomputeEvidenceManifestHash(
            manifest,
            completed.Value!,
            "FaceMatch",
            mutatedPayloadHash);

        Assert.True(evidenceAccepted.IsSuccess);
        Assert.True(completed.IsSuccess);
        Assert.Equal(VerificationResultDto.Passed, completed.Value?.Result);
        Assert.Contains("OPERATIONAL_REASON_ONLY", fixture.Decisions.Decisions.Single().DecisionReasonCodes);
        Assert.Equal(expectedPayloadHash, faceMatchRef.PayloadHash);
        Assert.NotEqual(expectedPayloadHash, mutatedPayloadHash);
        Assert.NotEqual(manifest.ManifestHash, mutatedManifestHash);
        Assert.DoesNotContain("OPERATIONAL_REASON_ONLY", JsonSerializer.Serialize(manifest.EvidenceRefs, JsonOptions), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Tip72_liveness_decision_basis_payload_hash_is_proof_bound_without_reason_codes()
    {
        var fixture = CreateFixture();
        var session = await CreateChallengeBoundSessionAsync(fixture, [RequiredCheckTypeDto.Liveness]);
        var liveArtifact = await fixture.EvidenceService.AppendCaptureArtifactAsync(
            CaptureCaller(),
            session.Value!.VerificationSessionId,
            LivenessArtifact(),
            CancellationToken.None,
            $"test-idempotency-{Guid.NewGuid():N}");
        var scenario = new LivenessScenario(
            session.Value.VerificationSessionId,
            liveArtifact.Value!.CaptureArtifactId);
        var expectedPayloadHash = ExpectedLivenessPayloadHash(scenario);
        var mutatedPayloadHash = ExpectedLivenessPayloadHash(
            scenario,
            score: 0.93m);

        var evidenceAccepted = await fixture.EvidenceService.AppendEvidenceResultAsync(
            TrustedCaller(),
            session.Value.VerificationSessionId,
            LivenessEvidence(scenario) with
            {
                ReasonCodes = ["OPERATIONAL_REASON_ONLY"],
            },
            CancellationToken.None,
            $"test-idempotency-{Guid.NewGuid():N}");
        var completed = await fixture.CompletionService.CompleteAsync(
            BusinessCaller(),
            session.Value.VerificationSessionId,
            new CompleteVerificationSessionRequestDto(),
            CancellationToken.None);
        var manifest = fixture.Manifests.Manifests.Single();
        var livenessRef = manifest.EvidenceRefs.Single(evidenceRef => evidenceRef.Type == "Liveness");
        var mutatedManifestHash = RecomputeEvidenceManifestHash(
            manifest,
            completed.Value!,
            "Liveness",
            mutatedPayloadHash);

        Assert.True(evidenceAccepted.IsSuccess);
        Assert.True(completed.IsSuccess);
        Assert.Equal(VerificationResultDto.Passed, completed.Value?.Result);
        Assert.Contains("OPERATIONAL_REASON_ONLY", fixture.Decisions.Decisions.Single().DecisionReasonCodes);
        Assert.Equal(expectedPayloadHash, livenessRef.PayloadHash);
        Assert.NotEqual(expectedPayloadHash, mutatedPayloadHash);
        Assert.NotEqual(manifest.ManifestHash, mutatedManifestHash);
        Assert.DoesNotContain("OPERATIONAL_REASON_ONLY", JsonSerializer.Serialize(manifest.EvidenceRefs, JsonOptions), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Tip69_external_prestaged_nfc_cannot_complete_as_passed_or_high()
    {
        var fixture = CreateFixture();
        var session = await CreateChallengeBoundSessionAsync(fixture, [RequiredCheckTypeDto.DocumentNfc]);
        var nfcArtifact = await fixture.EvidenceService.AppendCaptureArtifactAsync(
            CaptureCaller(),
            session.Value!.VerificationSessionId,
            NfcArtifact(CaptureSourceDto.ExternalPreStaged),
            CancellationToken.None,
            $"test-idempotency-{Guid.NewGuid():N}");

        var evidenceAccepted = await fixture.EvidenceService.AppendEvidenceResultAsync(
            TrustedCaller(),
            session.Value.VerificationSessionId,
            NfcEvidence(session.Value.VerificationSessionId, nfcArtifact.Value!.CaptureArtifactId),
            CancellationToken.None,
            $"test-idempotency-{Guid.NewGuid():N}");
        var persistedEvidence = fixture.Evidence.EvidenceResults.Single();
        var storedBeforeCompletion = await fixture.Sessions.GetAsync(
            Guid.Parse(session.Value.VerificationSessionId),
            CancellationToken.None);
        var completed = await fixture.CompletionService.CompleteAsync(
            BusinessCaller(),
            session.Value.VerificationSessionId,
            new CompleteVerificationSessionRequestDto(),
            CancellationToken.None);

        Assert.True(evidenceAccepted.IsSuccess);
        Assert.Equal(VerificationResult.ReviewRequired, persistedEvidence.Result);
        Assert.NotEqual(VerificationResult.Passed, persistedEvidence.Result);
        Assert.Contains("DIRECT_CLIENT_UPLOAD_UNTRUSTED", persistedEvidence.ReasonCodes);
        Assert.DoesNotContain("CAPTURE_BOUND_TO_SESSION", persistedEvidence.ReasonCodes);
        Assert.NotEqual(VerificationSessionState.ReadyToComplete, storedBeforeCompletion?.State);
        Assert.True(completed.IsSuccess);
        Assert.NotEqual(VerificationResultDto.Passed, completed.Value?.Result);
        Assert.Equal(VerificationResultDto.ReviewRequired, completed.Value?.Result);
        Assert.NotEqual(AssuranceLevelDto.High, completed.Value?.AssuranceLevel);
        Assert.Equal(AssuranceLevelDto.Low, completed.Value?.AssuranceLevel);
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
            CancellationToken.None,
            $"test-idempotency-{Guid.NewGuid():N}");
        await fixture.EvidenceService.AppendEvidenceResultAsync(
            TrustedCaller(),
            session.Value.VerificationSessionId,
            DocumentOcrEvidence([artifact.Value!.CaptureArtifactId]) with
            {
                Result = VerificationResultDto.FailedIdentity,
                ReasonCodes = ["DOCUMENT_DATA_MISMATCH"],
            },
            CancellationToken.None,
            $"test-idempotency-{Guid.NewGuid():N}");

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
            CancellationToken.None,
            $"test-idempotency-{Guid.NewGuid():N}");
        await fixture.EvidenceService.AppendEvidenceResultAsync(
            TrustedCaller(),
            session.Value.VerificationSessionId,
            CaptureQualityEvidence([artifact.Value!.CaptureArtifactId]),
            CancellationToken.None,
            $"test-idempotency-{Guid.NewGuid():N}");

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
            CancellationToken.None,
            $"test-idempotency-{Guid.NewGuid():N}");
        await fixture.EvidenceService.AppendEvidenceResultAsync(
            TrustedCaller(),
            session.Value.VerificationSessionId,
            CaptureQualityEvidence([artifact.Value!.CaptureArtifactId]),
            CancellationToken.None,
            $"test-idempotency-{Guid.NewGuid():N}");
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
            Challenge: Tip69Challenge,
            RequestId: "req-tip69",
            CorrelationId: "corr-tip69");

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

        return new TestFixture(
            sessionService,
            evidenceService,
            completionService,
            sessions,
            evidence,
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

    private static CaptureArtifactSubmissionRequestDto NfcArtifact(CaptureSourceDto captureSource = CaptureSourceDto.MobileSdk) =>
        new(
            CaptureArtifactTypeDto.NfcReadArtifact,
            captureSource,
            "ldev_capture",
            "device-1",
            "sha256:nfc-artifact",
            MetadataHash: null,
            RequestId: "req-nfc-artifact",
            CorrelationId: "corr-tip69");

    private static CaptureArtifactSubmissionRequestDto SelfieArtifact(CaptureSourceDto captureSource = CaptureSourceDto.MobileSdk) =>
        new(
            CaptureArtifactTypeDto.SelfieImage,
            captureSource,
            "ldev_capture",
            "device-1",
            "sha256:selfie-artifact",
            MetadataHash: null,
            RequestId: "req-selfie-artifact",
            CorrelationId: "corr-tip70");

    private static CaptureArtifactSubmissionRequestDto LivenessArtifact(CaptureSourceDto captureSource = CaptureSourceDto.MobileSdk) =>
        new(
            CaptureArtifactTypeDto.LivenessMedia,
            captureSource,
            "ldev_capture",
            "device-1",
            "sha256:liveness-artifact",
            MetadataHash: null,
            RequestId: "req-liveness-artifact",
            CorrelationId: "corr-tip72");

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

    private static EvidenceResultSubmissionRequestDto NfcEvidence(
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
            NfcEvidenceDecisionBasis: new NfcEvidenceDecisionBasisDto(
                [
                    "NFC_READ_OK",
                    "PACE_SUCCESS",
                    "SOD_INTERNAL_VALID",
                    "DG_HASHES_MATCH_SOD",
                    "CSCA_NOT_VERIFIED",
                    "CHIP_AUTH_RESPONSE_VALID",
                ],
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
                    nameof(EvidenceResultTypeDto.NfcValidation))));

    private static EvidenceResultSubmissionRequestDto FaceMatchEvidence(
        FaceMatchScenario scenario,
        decimal score = 0.92m,
        bool? isMatch = true) =>
        new(
            EvidenceResultTypeDto.FaceMatch,
            [scenario.LiveArtifactId],
            VerificationResultDto.Passed,
            Confidence: score,
            ReasonCodes: [],
            RetryReasonCode: null,
            SanitizedSummaryRef: "summary:face-match",
            PayloadHash: null,
            SignaturePlaceholderStatusDto.PlaceholderUnverified,
            "fixture-face-match",
            "tip70",
            RequestId: "req-face-match",
            CorrelationId: "corr-tip70",
            FaceMatchEvidenceDecisionBasis: new FaceMatchEvidenceDecisionBasisDto(
                scenario.LiveArtifactId,
                "sha256:selfie-artifact",
                score,
                ThresholdApplied: null,
                isMatch,
                FaceMatchReferenceFaceSourceDto.ChipDg2FromTrustedNfc,
                scenario.NfcEvidenceResultId,
                nameof(EvidenceResultTypeDto.NfcValidation),
                scenario.NfcArtifactId,
                "sha256:nfc-artifact",
                scenario.NfcPayloadHash,
                new FaceMatchCaptureBindingDto(
                    ChallengeHash(scenario.SessionId),
                    scenario.SessionId,
                    "ldev_capture",
                    "device-1",
                    CapturedAt: Tip70CapturedAt,
                    "sha256:selfie-artifact"),
                ServerDecisionResult: null,
                AdapterRequestedResult: VerificationResultDto.Passed,
                "fixture-face-match",
                "tip70",
                SanitizedSummaryLabel: "summary:face-match",
                new VerificationExtensionDescriptorDto(
                    "face-match",
                    nameof(RequiredCheckTypeDto.FaceMatch),
                    VerificationExtensionCategoryDto.IdentityEvidence,
                    nameof(EvidenceResultTypeDto.FaceMatch))));

    private static EvidenceResultSubmissionRequestDto LivenessEvidence(
        LivenessScenario scenario,
        decimal score = 0.92m,
        string? adapterRequestedVerdict = "live",
        bool? serverDerivedIsLive = true) =>
        new(
            EvidenceResultTypeDto.Liveness,
            [scenario.LiveArtifactId],
            VerificationResultDto.Passed,
            Confidence: score,
            ReasonCodes: [],
            RetryReasonCode: null,
            SanitizedSummaryRef: "summary:liveness",
            PayloadHash: null,
            SignaturePlaceholderStatusDto.PlaceholderUnverified,
            "fixture-liveness",
            "tip72",
            RequestId: "req-liveness",
            CorrelationId: "corr-tip72",
            LivenessEvidenceDecisionBasis: new LivenessEvidenceDecisionBasisDto(
                scenario.LiveArtifactId,
                "sha256:liveness-artifact",
                score,
                adapterRequestedVerdict,
                "fixture-liveness",
                "passive-2d-only",
                ThresholdApplied: null,
                new LivenessCaptureBindingDto(
                    ChallengeHash(scenario.SessionId),
                    scenario.SessionId,
                    "ldev_capture",
                    "device-1",
                    CapturedAt: Tip72CapturedAt,
                    "sha256:liveness-artifact"),
                serverDerivedIsLive,
                ServerDecisionResult: null,
                AdapterRequestedResult: VerificationResultDto.Passed,
                SanitizedSummaryLabel: "summary:liveness",
                new VerificationExtensionDescriptorDto(
                    "liveness",
                    nameof(RequiredCheckTypeDto.Liveness),
                    VerificationExtensionCategoryDto.IdentityEvidence,
                    nameof(EvidenceResultTypeDto.Liveness))));

    private static string ExpectedNfcPayloadHash(
        string sessionId,
        string artifactId,
        IReadOnlyList<string>? flags = null) =>
        EvidenceCanonicalization.HashCanonical(
            "tip-69-nfc-evidence-decision-basis",
            Tip69NormalizedBasis(
                sessionId,
                artifactId,
                flags ??
                [
                    "CAPTURE_BOUND_TO_SESSION",
                    "CHIP_AUTH_RESPONSE_VALID",
                    "CSCA_NOT_VERIFIED",
                    "DG_HASHES_MATCH_SOD",
                    "NFC_READ_OK",
                    "PACE_SUCCESS",
                    "SOD_INTERNAL_VALID",
                ],
                "Passed"));

    private static string ExpectedFaceMatchPayloadHash(
        FaceMatchScenario scenario,
        decimal score = 0.92m) =>
        EvidenceCanonicalization.HashCanonical(
            "tip-70-face-match-decision-basis",
            new
            {
                extension = new
                {
                    id = "face-match",
                    requiredCheckType = "FaceMatch",
                    category = "IdentityEvidence",
                    emitsEvidenceType = "FaceMatch",
                },
                flags = new[] { "CAPTURE_BOUND_TO_SESSION" },
                liveFaceArtifact = new
                {
                    artifactId = scenario.LiveArtifactId,
                    artifactHash = "sha256:selfie-artifact",
                },
                matchScore = (decimal?)score,
                thresholdApplied = 0.80m,
                isMatch = true,
                reference = new
                {
                    referenceFaceSource = "ChipDg2FromTrustedNfc",
                    referenceEvidenceResultId = scenario.NfcEvidenceResultId,
                    referenceEvidenceType = nameof(EvidenceResultTypeDto.NfcValidation),
                    referenceArtifactId = scenario.NfcArtifactId,
                    referenceArtifactHash = "sha256:nfc-artifact",
                    referencePayloadHash = scenario.NfcPayloadHash,
                },
                liveCaptureBinding = new
                {
                    challengeHash = ChallengeHash(scenario.SessionId),
                    sessionId = scenario.SessionId,
                    captureAgentId = "ldev_capture",
                    deviceId = "device-1",
                    capturedAt = Tip70CapturedAt,
                    artifactHash = "sha256:selfie-artifact",
                },
                serverDecisionResult = "Passed",
                adapterRequestedResult = "Passed",
                engineName = "fixture-face-match",
                engineVersion = "tip70",
                sanitizedSummaryLabel = "summary:face-match",
            });

    private static string ExpectedLivenessPayloadHash(
        LivenessScenario scenario,
        decimal score = 0.92m,
        string? adapterRequestedVerdict = "live",
        bool serverDerivedIsLive = true,
        string serverDecisionResult = "Passed",
        IReadOnlyList<string>? flags = null) =>
        EvidenceCanonicalization.HashCanonical(
            "tip-72-liveness-decision-basis",
            new
            {
                extension = new
                {
                    id = "liveness",
                    requiredCheckType = "Liveness",
                    category = "IdentityEvidence",
                    emitsEvidenceType = "Liveness",
                },
                flags = (flags ?? ["CAPTURE_BOUND_TO_SESSION"]).Order(StringComparer.Ordinal).ToArray(),
                liveMediaArtifact = new
                {
                    artifactId = scenario.LiveArtifactId,
                    artifactHash = "sha256:liveness-artifact",
                },
                livenessScore = (decimal?)score,
                adapterRequestedVerdict,
                method = "fixture-liveness",
                livenessGrade = "passive-2d-only",
                thresholdApplied = 0.80m,
                liveCaptureBinding = new
                {
                    challengeHash = ChallengeHash(scenario.SessionId),
                    sessionId = scenario.SessionId,
                    captureAgentId = "ldev_capture",
                    deviceId = "device-1",
                    capturedAt = Tip72CapturedAt,
                    artifactHash = "sha256:liveness-artifact",
                },
                serverDerivedIsLive,
                serverDecisionResult,
                adapterRequestedResult = "Passed",
                sanitizedSummaryLabel = "summary:liveness",
            });

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
            challenge = Tip69Challenge,
        });

    private static string RecomputeSingleEvidenceManifestHash(
        TagEkyc.Contracts.InternalAudit.Manifest.EvidenceManifestDto manifest,
        CompleteVerificationSessionResponseDto completed,
        string payloadHash)
    {
        return RecomputeEvidenceManifestHash(
            manifest,
            completed,
            manifest.EvidenceRefs.Single().Type,
            payloadHash);
    }

    private static string RecomputeEvidenceManifestHash(
        TagEkyc.Contracts.InternalAudit.Manifest.EvidenceManifestDto manifest,
        CompleteVerificationSessionResponseDto completed,
        string evidenceType,
        string payloadHash)
    {
        var mutatedRefs = manifest.EvidenceRefs
            .Select(evidenceRef => evidenceRef.Type == evidenceType
                ? evidenceRef with { PayloadHash = payloadHash }
                : evidenceRef)
            .ToArray();
        var createdAt = EvidenceCanonicalization.FormatTimestamp(manifest.CreatedAt);
        var manifestBodyHash = EvidenceCanonicalization.HashCanonical("tip-06-manifest-body", new
        {
            manifest.EvidencePackageId,
            manifest.VerificationSessionId,
            manifest.PackageVersion,
            manifest.CanonicalizationScheme,
            manifest.HashAlgorithm,
            EvidenceRefs = mutatedRefs,
            manifest.AuditEventRefs,
            manifest.ResultRef,
            Result = completed.Result.ToString(),
            AssuranceLevel = completed.AssuranceLevel.ToString(),
            completed.RequestId,
            completed.CorrelationId,
            CreatedAt = createdAt,
        });
        var packageHash = EvidenceCanonicalization.HashCanonical("tip-06-evidence-package", new
        {
            manifest.EvidencePackageId,
            manifest.VerificationSessionId,
            manifest.PackageVersion,
            manifest.CanonicalizationScheme,
            manifest.HashAlgorithm,
            ManifestBodyHash = manifestBodyHash,
            manifest.ResultRef,
            EvidenceRefs = mutatedRefs.Select(evidenceRef => evidenceRef.Id).ToArray(),
            Result = completed.Result.ToString(),
            AssuranceLevel = completed.AssuranceLevel.ToString(),
            CreatedAt = createdAt,
        });

        return EvidenceCanonicalization.HashCanonical("tip-06-evidence-manifest", new
        {
            BodyHash = manifestBodyHash,
            PackageHash = packageHash,
        });
    }

    private const string Tip69Challenge = "opaque-tip69-challenge";
    private static readonly DateTimeOffset Tip69CapturedAt = new(2026, 6, 26, 1, 2, 3, TimeSpan.Zero);
    private static readonly DateTimeOffset Tip70CapturedAt = new(2026, 6, 26, 4, 5, 6, TimeSpan.Zero);
    private static readonly DateTimeOffset Tip72CapturedAt = new(2026, 6, 26, 7, 8, 9, TimeSpan.Zero);

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
        LocalDevInMemoryVerificationSessionRepository Sessions,
        LocalDevInMemoryEvidenceResultRepository Evidence,
        LocalDevInMemoryVerificationDecisionRepository Decisions,
        LocalDevInMemoryEvidencePackageRepository Packages,
        LocalDevInMemoryEvidenceManifestRepository Manifests,
        LocalDevInMemoryAuditEventRepository Audit);

    private sealed record FaceMatchScenario(
        string SessionId,
        string NfcArtifactId,
        string NfcEvidenceResultId,
        string NfcPayloadHash,
        string LiveArtifactId);

    private sealed record LivenessScenario(
        string SessionId,
        string LiveArtifactId);
}
