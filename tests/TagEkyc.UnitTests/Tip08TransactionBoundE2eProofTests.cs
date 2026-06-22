using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TagEkyc.Application;
using TagEkyc.Application.LocalDev;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.BusinessConsumer;
using TagEkyc.Contracts.CaptureAgent;
using TagEkyc.Contracts.Common;
using TagEkyc.Contracts.SignFlowProfile;
using TagEkyc.Contracts.TrustedAdapter;

namespace TagEkyc.UnitTests;

public sealed class Tip08TransactionBoundE2eProofTests
{
    private const string ExternalSessionId = "tip08-external-session";
    private const string ExternalTransactionId = "sf-tip08-transaction";
    private const string RawBindingNonceSentinel = "raw-tip08-binding-nonce-never-submit";
    private const string RawArtifactSentinel = "raw-tip08-artifact-never-expose";
    private const string PlaintextIdentitySentinel = "plaintext-tip08-identity-never-expose";
    private const string AdapterInternalSentinel = "trusted-adapter-internal-tip08-never-expose";
    private const string SignFlowInternalSentinel = "SignFlowRuntimeInternalTip08NeverExpose";
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    [Fact]
    public async Task Tip08_transaction_bound_profile_completes_s1_flow_and_composes_signflow_binding_summary()
    {
        var fixture = CreateFixture();
        var bindingNonceHash = Sha256HashRef(RawBindingNonceSentinel);
        Assert.Matches("^sha256:[0-9a-f]{64}$", bindingNonceHash);
        Assert.DoesNotContain(RawBindingNonceSentinel, bindingNonceHash, StringComparison.Ordinal);

        var session = await fixture.SessionService.CreateAsync(
            BusinessCaller(),
            TransactionBoundRequest(bindingNonceHash),
            cancellationToken: CancellationToken.None);
        Assert.True(session.IsSuccess);

        var storedBeforeCompletion = await fixture.Sessions.GetAsync(
            Guid.Parse(session.Value!.VerificationSessionId),
            CancellationToken.None);
        Assert.NotNull(storedBeforeCompletion);
        Assert.Equal(ExternalSessionId, storedBeforeCompletion!.ExternalSessionId);
        Assert.Equal(ExternalTransactionId, storedBeforeCompletion.ExternalTransactionId);
        Assert.Equal(bindingNonceHash, storedBeforeCompletion.BindingNonceHash?.ToString());

        var artifactIds = await AppendS1CaptureArtifactsAsync(fixture, session.Value.VerificationSessionId);
        await AppendTrustedAdapterS1EvidenceAsync(fixture, session.Value.VerificationSessionId, artifactIds);

        var completed = await fixture.CompletionService.CompleteAsync(
            BusinessCaller(),
            session.Value.VerificationSessionId,
            new CompleteVerificationSessionRequestDto(
                RequestId: "req-tip08-complete",
                CorrelationId: "corr-tip08-complete"),
            CancellationToken.None);
        Assert.True(completed.IsSuccess);
        Assert.Equal(VerificationSessionStateDto.Completed, completed.Value?.State);
        Assert.Equal(VerificationResultDto.Passed, completed.Value?.Result);
        Assert.Equal(AssuranceLevelDto.High, completed.Value?.AssuranceLevel);

        var storedCompleted = await fixture.Sessions.GetAsync(
            Guid.Parse(session.Value.VerificationSessionId),
            CancellationToken.None);
        var summary = await fixture.SessionService.GetSummaryAsync(
            BusinessCaller(),
            session.Value.VerificationSessionId,
            CancellationToken.None);
        var package = await fixture.CompletionService.GetEvidencePackageAsync(
            BusinessCaller(),
            completed.Value!.EvidencePackageId,
            CancellationToken.None);
        var notification = await fixture.CompletionService.GetCompletionNotificationAsync(
            BusinessCaller(),
            session.Value.VerificationSessionId,
            CancellationToken.None);

        Assert.NotNull(storedCompleted);
        Assert.True(summary.IsSuccess);
        Assert.True(package.IsSuccess);
        Assert.True(notification.IsSuccess);

        var storedBindingNonceHash = storedCompleted!.BindingNonceHash?.ToString()
            ?? throw new InvalidOperationException("Completed transaction-bound session must preserve the binding nonce hash.");
        var binding = new SigningAuthorizationBindingDto(
            storedCompleted.ExternalSessionId!,
            storedCompleted.ExternalTransactionId!,
            storedBindingNonceHash,
            package.Value!.EvidencePackageId,
            package.Value.PackageHash);
        var signFlowResult = new SignFlowVerificationResultDto(summary.Value!, binding);

        Assert.Equal(VerificationProfileDto.TransactionBoundEkycProfile, summary.Value?.Profile);
        Assert.Equal(ExternalSessionId, summary.Value?.ExternalSessionId);
        Assert.Equal(ExternalSessionId, binding.ExternalSessionId);
        Assert.Equal(ExternalTransactionId, binding.ExternalTransactionId);
        Assert.Equal(bindingNonceHash, binding.BindingNonceHash);
        Assert.Equal(completed.Value.EvidencePackageId, binding.EvidencePackageId);
        Assert.Equal(completed.Value.EvidencePackageHash, binding.EvidencePackageHash);
        Assert.Equal(summary.Value, signFlowResult.Session);
        Assert.Equal(binding, signFlowResult.Binding);

        Assert.Equal(completed.Value.EvidencePackageId, summary.Value?.EvidencePackageId);
        Assert.Equal(completed.Value.EvidencePackageHash, summary.Value?.EvidencePackageHash);
        Assert.Equal(completed.Value.ManifestHash, summary.Value?.ManifestHash);
        Assert.Equal(completed.Value.EvidencePackageId, package.Value?.EvidencePackageId);
        Assert.Equal(completed.Value.EvidencePackageHash, package.Value?.PackageHash);
        Assert.Equal(completed.Value.ManifestHash, package.Value?.ManifestHash);
        Assert.Equal(completed.Value.EvidencePackageId, notification.Value?.EvidencePackageId);
        Assert.Equal(completed.Value.EvidencePackageHash, notification.Value?.EvidencePackageHash);
        Assert.Equal(completed.Value.ManifestHash, notification.Value?.ManifestHash);
        Assert.Equal("VERIFICATION_COMPLETED", notification.Value?.EventType);
        Assert.Equal("localdev-not-dispatched", notification.Value?.DeliveryId);
        Assert.Equal(completed.Value.CompletedAt, notification.Value?.CompletedAt);

        var requestedChecks = TransactionBoundRequest(bindingNonceHash)
            .RequiredChecks
            .Select(check => check.CheckType)
            .ToArray();

        Assert.Equal(SignFlowS1RequiredChecks.Values, requestedChecks);
        Assert.Contains(RequiredCheckTypeDto.DocumentNfc, requestedChecks);
        AssertDocumentNfcIsEvidencedByNfcValidation(package.Value!.EvidenceRefs);

        Assert.Contains(package.Value.EvidenceRefs, evidence => evidence.ResultType == nameof(EvidenceResultTypeDto.CaptureQuality));
        Assert.Contains(package.Value.EvidenceRefs, evidence => evidence.ResultType == nameof(EvidenceResultTypeDto.FaceMatch));
        Assert.Contains(package.Value.EvidenceRefs, evidence => evidence.ResultType == nameof(EvidenceResultTypeDto.Liveness));
        Assert.Equal(4, package.Value.EvidenceRefs.Count);

        var packageJson = JsonSerializer.Serialize(package.Value, JsonOptions);
        var summaryJson = JsonSerializer.Serialize(signFlowResult, JsonOptions);
        var notificationJson = JsonSerializer.Serialize(notification.Value, JsonOptions);
        var serializedOutputs = string.Join('\n', packageJson, summaryJson, notificationJson);

        Assert.Contains(bindingNonceHash, summaryJson, StringComparison.Ordinal);
        Assert.DoesNotContain(RawBindingNonceSentinel, serializedOutputs, StringComparison.Ordinal);
        Assert.DoesNotContain(RawArtifactSentinel, serializedOutputs, StringComparison.Ordinal);
        Assert.DoesNotContain(PlaintextIdentitySentinel, serializedOutputs, StringComparison.Ordinal);
        Assert.DoesNotContain(AdapterInternalSentinel, serializedOutputs, StringComparison.Ordinal);
        Assert.DoesNotContain(SignFlowInternalSentinel, serializedOutputs, StringComparison.Ordinal);
        Assert.DoesNotContain("vault:", serializedOutputs, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("payloadHash", serializedOutputs, StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertDocumentNfcIsEvidencedByNfcValidation(IReadOnlyList<EvidenceRefSummaryDto> evidenceRefs)
    {
        // Current repo behavior maps the canonical DocumentNfc required check to NfcValidation evidence.
        Assert.Contains(evidenceRefs, evidence => evidence.ResultType == nameof(EvidenceResultTypeDto.NfcValidation));
    }

    private static CreateVerificationSessionRequestDto TransactionBoundRequest(string bindingNonceHash) =>
        new(
            ExternalSessionId,
            PlaintextIdentitySentinel,
            "SIGNING_AUTH",
            VerificationProfileDto.TransactionBoundEkycProfile,
            SignFlowS1RequiredChecks.Values
                .Select(check => new RequiredCheckRequestDto(check, Required: true, MinimumConfidence: null))
                .ToArray(),
            DateTimeOffset.UtcNow.AddMinutes(30),
            ExternalTransactionId,
            bindingNonceHash,
            RequestId: "req-tip08-create",
            CorrelationId: "corr-tip08-create");

    private static async Task<S1ArtifactIds> AppendS1CaptureArtifactsAsync(TestFixture fixture, string verificationSessionId)
    {
        var metadata = await fixture.EvidenceService.AppendCaptureArtifactAsync(
            CaptureCaller(),
            verificationSessionId,
            Artifact(CaptureArtifactTypeDto.DeviceCaptureMetadata, artifactHash: null, metadataHash: "sha256:tip08-device-metadata"),
            CancellationToken.None);
        var nfc = await fixture.EvidenceService.AppendCaptureArtifactAsync(
            CaptureCaller(),
            verificationSessionId,
            Artifact(CaptureArtifactTypeDto.NfcReadArtifact, "sha256:tip08-nfc-artifact", metadataHash: null),
            CancellationToken.None);
        var selfie = await fixture.EvidenceService.AppendCaptureArtifactAsync(
            CaptureCaller(),
            verificationSessionId,
            Artifact(CaptureArtifactTypeDto.SelfieImage, "sha256:tip08-selfie-artifact", metadataHash: null),
            CancellationToken.None);
        var liveness = await fixture.EvidenceService.AppendCaptureArtifactAsync(
            CaptureCaller(),
            verificationSessionId,
            Artifact(CaptureArtifactTypeDto.LivenessMedia, "sha256:tip08-liveness-artifact", metadataHash: null),
            CancellationToken.None);

        Assert.True(metadata.IsSuccess);
        Assert.True(nfc.IsSuccess);
        Assert.True(selfie.IsSuccess);
        Assert.True(liveness.IsSuccess);

        return new S1ArtifactIds(
            metadata.Value!.CaptureArtifactId,
            nfc.Value!.CaptureArtifactId,
            selfie.Value!.CaptureArtifactId,
            liveness.Value!.CaptureArtifactId);
    }

    private static async Task AppendTrustedAdapterS1EvidenceAsync(
        TestFixture fixture,
        string verificationSessionId,
        S1ArtifactIds artifactIds)
    {
        var captureQuality = await fixture.EvidenceService.AppendEvidenceResultAsync(
            TrustedCaller(),
            verificationSessionId,
            Evidence(EvidenceResultTypeDto.CaptureQuality, [artifactIds.Metadata], "summary:tip08-capture-quality"),
            CancellationToken.None);
        var documentNfc = await fixture.EvidenceService.AppendEvidenceResultAsync(
            TrustedCaller(),
            verificationSessionId,
            Evidence(EvidenceResultTypeDto.NfcValidation, [artifactIds.Nfc], "summary:tip08-document-nfc"),
            CancellationToken.None);
        var faceMatch = await fixture.EvidenceService.AppendEvidenceResultAsync(
            TrustedCaller(),
            verificationSessionId,
            Evidence(EvidenceResultTypeDto.FaceMatch, [artifactIds.Selfie], "summary:tip08-face-match"),
            CancellationToken.None);
        var liveness = await fixture.EvidenceService.AppendEvidenceResultAsync(
            TrustedCaller(),
            verificationSessionId,
            Evidence(EvidenceResultTypeDto.Liveness, [artifactIds.Liveness], "summary:tip08-liveness"),
            CancellationToken.None);

        Assert.True(captureQuality.IsSuccess);
        Assert.True(documentNfc.IsSuccess);
        Assert.True(faceMatch.IsSuccess);
        Assert.True(liveness.IsSuccess);
    }

    private static CaptureArtifactSubmissionRequestDto Artifact(
        CaptureArtifactTypeDto artifactType,
        string? artifactHash,
        string? metadataHash) =>
        new(
            artifactType,
            CaptureSourceDto.MobileSdk,
            "ldev_capture",
            RawArtifactSentinel,
            artifactHash,
            metadataHash,
            RequestId: "req-tip08-artifact",
            CorrelationId: "corr-tip08-artifact");

    private static EvidenceResultSubmissionRequestDto Evidence(
        EvidenceResultTypeDto resultType,
        IReadOnlyList<string> artifactIds,
        string sanitizedSummaryRef) =>
        new(
            resultType,
            artifactIds,
            VerificationResultDto.Passed,
            Confidence: 0.99m,
            ReasonCodes: [],
            RetryReasonCode: null,
            SanitizedSummaryRef: sanitizedSummaryRef,
            PayloadHash: "sha256:tip08-payload",
            SignaturePlaceholderStatusDto.PlaceholderUnverified,
            AdapterInternalSentinel,
            SignFlowInternalSentinel,
            RequestId: "req-tip08-evidence",
            CorrelationId: "corr-tip08-evidence");

    private static string Sha256HashRef(string value)
    {
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return $"sha256:{Convert.ToHexString(digest).ToLowerInvariant()}";
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
            new TestEvidenceSigner(),
            finalization);

        return new TestFixture(
            sessionService,
            evidenceService,
            completionService,
            sessions);
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

    private sealed record S1ArtifactIds(
        string Metadata,
        string Nfc,
        string Selfie,
        string Liveness);

    private sealed record TestFixture(
        VerificationSessionApplicationService SessionService,
        VerificationEvidenceApplicationService EvidenceService,
        VerificationCompletionApplicationService CompletionService,
        LocalDevInMemoryVerificationSessionRepository Sessions);
}
