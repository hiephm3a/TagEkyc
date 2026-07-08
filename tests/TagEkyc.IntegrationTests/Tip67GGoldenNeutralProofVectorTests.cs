using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TagEkyc.Application;
using TagEkyc.Application.LocalDev;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.BusinessConsumer;
using TagEkyc.Contracts.CaptureAgent;
using TagEkyc.Contracts.Common;
using TagEkyc.Contracts.TrustedAdapter;
using TagEkyc.Domain;
using TagEkyc.Infrastructure.Signing;

namespace TagEkyc.IntegrationTests;

public sealed class Tip67GGoldenNeutralProofVectorTests
{
    private const string FixtureRelativePath = "docs/contracts/golden_neutral_proof_vectors.json";
    private const string SchemaVersion = "tip-67g-golden-neutral-proof-v1";
    private const string GeneratedByCommit = "f0c3cffdfd56ec37da78a8e7e8a8e9a2ef82c624";
    private const string SignerKid = "tip67g-golden-key-v1";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public void Golden_neutral_proof_vectors_self_verify_signature_hashes_identity_challenge_and_trust_anchor()
    {
        var fixture = LoadFixture();

        Assert.Equal(SchemaVersion, fixture.SchemaVersion);
        Assert.NotEmpty(fixture.Vectors);
        foreach (var vector in fixture.Vectors)
        {
            VerifyVectorOrThrow(
                vector,
                vector.ChallengeInput.ExpectedChallenge,
                vector.ExpectedTrustAnchor);
        }
    }

    [Fact]
    public void Golden_neutral_proof_vector_rejects_wrong_external_challenge()
    {
        var vector = Assert.Single(LoadFixture().Vectors, item => item.Name == "medium-capture-quality-v1");

        Assert.False(VerifyVector(
            vector,
            "wrong external challenge",
            vector.ExpectedTrustAnchor));
    }

    [Fact]
    public void Golden_neutral_proof_vector_rejects_wrong_expected_trust_anchor()
    {
        var vector = Assert.Single(LoadFixture().Vectors, item => item.Name == "medium-capture-quality-v1");
        var wrongAnchor = vector.ExpectedTrustAnchor with
        {
            PublicKeyFingerprint = "sha256:0000000000000000000000000000000000000000000000000000000000000000",
        };

        Assert.False(VerifyVector(
            vector,
            vector.ChallengeInput.ExpectedChallenge,
            wrongAnchor));
    }

    [Fact]
    public void Golden_neutral_proof_fixture_contains_only_synthetic_no_pii_inputs()
    {
        var fixture = LoadFixture();
        var text = File.ReadAllText(FixturePath());

        foreach (var vector in fixture.Vectors)
        {
            Assert.True(vector.SyntheticNoPii);
            Assert.StartsWith("subject-ref", vector.IdentityRefInputs.SubjectRef, StringComparison.Ordinal);
            Assert.StartsWith("opaque", vector.ChallengeInput.ExpectedChallenge, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("BHXH", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MRN", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Nguyen", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CCCD", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fullName", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("dateOfBirth", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("documentNumber", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hospitalCode", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(Skip = "Manual one-shot fixture generator; do not run during normal dotnet test.")]
    public async Task Manual_generate_tip67g_golden_vectors()
    {
        var generatedAt = EvidenceCanonicalization.FormatTimestamp(
            Es256JwsEvidenceSignatureBuilder.TruncateToMicroseconds(DateTimeOffset.UtcNow));

        using var signer = new LocalDevEs256JwsEvidenceSigner(new LocalDevEs256JwsEvidenceSignerOptions { KeyId = SignerKid });
        var fixture = CreateFixture(signer);
        var medium = await CaptureMediumVectorAsync(fixture, generatedAt);
        var golden = new GoldenVectorsFixture(SchemaVersion, [medium]);

        Directory.CreateDirectory(Path.GetDirectoryName(FixturePath())!);
        File.WriteAllText(
            FixturePath(),
            $"{JsonSerializer.Serialize(golden, JsonOptions)}{Environment.NewLine}");
    }

    private static async Task<GoldenVector> CaptureMediumVectorAsync(TestFixture fixture, string generatedAt)
    {
        var session = await CreateSessionAsync(
            fixture,
            externalSessionId: "external-tip67g-medium",
            subjectRef: "subject-ref-medium",
            purpose: "PATIENT_REGISTRATION",
            requiredChecks: [RequiredCheckTypeDto.CaptureQuality],
            clientReference: "client-ref-medium",
            challenge: "opaque challenge: medium-123",
            requestId: "req-create-medium",
            correlationId: "corr-create-medium");

        var artifact = await AppendArtifactAsync(
            fixture,
            session.VerificationSessionId,
            CaptureArtifactTypeDto.DeviceCaptureMetadata,
            artifactHash: null,
            metadataHash: "sha256:metadata-medium",
            requestId: "req-artifact-medium",
            correlationId: "corr-artifact-medium",
            key: "tip67g-medium|artifact");

        await AppendEvidenceAsync(
            fixture,
            session.VerificationSessionId,
            EvidenceResultTypeDto.CaptureQuality,
            [artifact.CaptureArtifactId],
            confidence: 0.9m,
            sanitizedSummaryRef: "summary:capture-quality-medium",
            payloadHash: "sha256:payload-medium",
            engineName: "localdev-quality",
            engineVersion: "s1",
            requestId: "req-evidence-medium",
            correlationId: "corr-evidence-medium",
            key: "tip67g-medium|evidence");

        var view = await CompleteAndReadViewAsync(
            fixture,
            session.VerificationSessionId,
            requestId: "req-complete-medium",
            correlationId: "corr-complete-medium");

        return BuildVector(
            "medium-capture-quality-v1",
            "Medium",
            generatedAt,
            view,
            subjectRef: "subject-ref-medium");
    }

    private static async Task<CreateVerificationSessionResponseDto> CreateSessionAsync(
        TestFixture fixture,
        string externalSessionId,
        string subjectRef,
        string purpose,
        IReadOnlyList<RequiredCheckTypeDto> requiredChecks,
        string clientReference,
        string challenge,
        string requestId,
        string correlationId)
    {
        var result = await fixture.SessionService.CreateAsync(
            BusinessCaller(),
            new CreateVerificationSessionRequestDto(
                externalSessionId,
                subjectRef,
                purpose,
                VerificationProfileDto.ChallengeBoundEkycProfile,
                requiredChecks.Select(check => new RequiredCheckRequestDto(check, Required: true, MinimumConfidence: null)).ToArray(),
                DateTimeOffset.UtcNow.AddMinutes(30),
                ClientReference: clientReference,
                Challenge: challenge,
                RequestId: requestId,
                CorrelationId: correlationId),
            cancellationToken: CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Code);
        return result.Value!;
    }

    private static async Task<CaptureArtifactSubmissionResponseDto> AppendArtifactAsync(
        TestFixture fixture,
        string sessionId,
        CaptureArtifactTypeDto artifactType,
        string? artifactHash,
        string? metadataHash,
        string requestId,
        string correlationId,
        string key)
    {
        var result = await fixture.EvidenceService.AppendCaptureArtifactAsync(
            CaptureCaller(),
            sessionId,
            new CaptureArtifactSubmissionRequestDto(
                artifactType,
                CaptureSourceDto.MobileSdk,
                "ldev_capture",
                "device-1",
                artifactHash,
                metadataHash,
                requestId,
                correlationId),
            CancellationToken.None,
            key);

        Assert.True(result.IsSuccess, result.Error?.Code);
        return result.Value!;
    }

    private static async Task<EvidenceResultSubmissionResponseDto> AppendEvidenceAsync(
        TestFixture fixture,
        string sessionId,
        EvidenceResultTypeDto resultType,
        IReadOnlyList<string> inputArtifactIds,
        decimal confidence,
        string sanitizedSummaryRef,
        string payloadHash,
        string engineName,
        string engineVersion,
        string requestId,
        string correlationId,
        string key)
    {
        var result = await fixture.EvidenceService.AppendEvidenceResultAsync(
            TrustedCaller(),
            sessionId,
            new EvidenceResultSubmissionRequestDto(
                resultType,
                inputArtifactIds,
                VerificationResultDto.Passed,
                Confidence: confidence,
                ReasonCodes: [],
                RetryReasonCode: null,
                sanitizedSummaryRef,
                payloadHash,
                SignaturePlaceholderStatusDto.PlaceholderUnverified,
                engineName,
                engineVersion,
                requestId,
                correlationId),
            CancellationToken.None,
            key);

        Assert.True(result.IsSuccess, result.Error?.Code);
        return result.Value!;
    }

    private static async Task<EvidencePackageVerificationViewDto> CompleteAndReadViewAsync(
        TestFixture fixture,
        string sessionId,
        string requestId,
        string correlationId)
    {
        var completed = await fixture.CompletionService.CompleteAsync(
            BusinessCaller(),
            sessionId,
            new CompleteVerificationSessionRequestDto(RequestId: requestId, CorrelationId: correlationId),
            CancellationToken.None);
        Assert.True(completed.IsSuccess, completed.Error?.Code);

        var view = await fixture.CompletionService.GetEvidencePackageVerificationViewAsync(
            BusinessCaller(),
            completed.Value!.EvidencePackageId,
            CancellationToken.None);
        Assert.True(view.IsSuccess, view.Error?.Code);
        return view.Value!;
    }

    private static GoldenVector BuildVector(
        string name,
        string assuranceTarget,
        string generatedAt,
        EvidencePackageVerificationViewDto view,
        string subjectRef)
    {
        var claims = DecodeClaims(view.SignatureValue);
        var trustAnchor = new ExpectedTrustAnchor(view.KeyId, view.PublicKeyFingerprint);
        return new GoldenVector(
            name,
            assuranceTarget,
            GeneratedByCommit,
            generatedAt,
            "Tip67GGoldenNeutralProofVectorTests.Manual_generate_tip67g_golden_vectors",
            true,
            view.KeyId,
            view.KeyId,
            view.PublicKeyFingerprint,
            trustAnchor,
            view.PublicKeyJwk,
            view.SignatureValue,
            new SignatureMetadata(view.SignatureFormat, view.SignatureScheme, view.SignatureAlgorithm),
            new GoldenConstants(
                EvidenceSignatureDefaults.ProofVersionNeutralV1,
                EvidenceSignatureDefaults.ResultHashAlgorithmSha256,
                EvidenceSignatureDefaults.ResultHashCanonicalizationSchemeJcsV1,
                EvidenceSignatureDefaults.ResultHashLabel,
                "tip-67b-identity-ref-v1"),
            new IdentityRefInputs(LocalDevRuntimePolicySource.BusinessClientId.ToString("N"), subjectRef, "tip-67b-identity-ref-v1"),
            new ChallengeInput(view.Challenge),
            claims);
    }

    private static bool VerifyVector(GoldenVector vector, string expectedChallenge, ExpectedTrustAnchor expectedTrustAnchor)
    {
        try
        {
            VerifyVectorOrThrow(vector, expectedChallenge, expectedTrustAnchor);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void VerifyVectorOrThrow(GoldenVector vector, string expectedChallenge, ExpectedTrustAnchor expectedTrustAnchor)
    {
        Assert.Equal(GeneratedByCommit, vector.GeneratedByCommit);
        Assert.True(vector.SyntheticNoPii);
        Assert.Equal(vector.Kid, vector.SignerKid);
        Assert.Equal(vector.Kid, vector.ExpectedTrustAnchor.Kid);
        Assert.Equal(vector.PublicKeyFingerprint, vector.ExpectedTrustAnchor.PublicKeyFingerprint);
        Assert.Equal(expectedTrustAnchor.Kid, vector.Kid);
        Assert.Equal(expectedTrustAnchor.PublicKeyFingerprint, vector.PublicKeyFingerprint);
        Assert.Equal(expectedChallenge, vector.ChallengeInput.ExpectedChallenge);
        Assert.Equal(EvidenceSignatureDefaults.ProofVersionNeutralV1, vector.Constants.ProofVersion);
        Assert.Equal(EvidenceSignatureDefaults.ResultHashAlgorithmSha256, vector.Constants.ResultHashAlgorithm);
        Assert.Equal(EvidenceSignatureDefaults.ResultHashCanonicalizationSchemeJcsV1, vector.Constants.ResultHashCanonicalizationScheme);
        Assert.Equal(EvidenceSignatureDefaults.ResultHashLabel, vector.Constants.ResultHashLabel);
        Assert.Equal("tip-67b-identity-ref-v1", vector.Constants.IdentityRefLabel);

        var parts = vector.Jws.Split('.');
        Assert.Equal(3, parts.Length);
        using var headerDocument = JsonDocument.Parse(Encoding.UTF8.GetString(Base64UrlDecode(parts[0])));
        var header = headerDocument.RootElement;
        Assert.Equal(["alg", "kid"], header.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal).ToArray());
        Assert.Equal(EvidenceSignatureDefaults.AlgorithmEs256, ReadString(header, "alg"));
        Assert.Equal(expectedTrustAnchor.Kid, ReadString(header, "kid"));
        Assert.Equal(EvidenceSignatureDefaults.AlgorithmEs256, vector.Signature.Algorithm);
        Assert.Equal(EvidenceSignatureDefaults.FormatJws, vector.Signature.Format);
        Assert.Equal(EvidenceSignatureDefaults.SchemeJwsEs256V1, vector.Signature.Scheme);

        using var key = ParsePinnedPublicJwk(vector.PublicKeyJwk, expectedTrustAnchor.PublicKeyFingerprint);
        Assert.True(key.VerifyData(
            Encoding.ASCII.GetBytes($"{parts[0]}.{parts[1]}"),
            Base64UrlDecode(parts[2]),
            HashAlgorithmName.SHA256,
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation));

        var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
        using var claimDocument = JsonDocument.Parse(payloadJson);
        Assert.Equal(ExpectedClaimNames, claimDocument.RootElement.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal).ToArray());
        Assert.Equal(EvidenceCanonicalization.Canonicalize(vector.Claims), EvidenceCanonicalization.CanonicalizeJson(payloadJson));
        Assert.Equal(expectedChallenge, vector.Claims.Challenge);
        Assert.Equal(RecomputeResultHash(vector.Claims), vector.Claims.ResultHash);
        Assert.Equal(RecomputeIdentityRef(vector.IdentityRefInputs), vector.Claims.IdentityRef);
    }

    private static ECDsa ParsePinnedPublicJwk(string publicKeyJwk, string expectedFingerprint)
    {
        using var document = JsonDocument.Parse(publicKeyJwk);
        var root = document.RootElement;
        var names = root.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(["crv", "kty", "x", "y"], names);
        var canonicalJwk = EvidenceCanonicalization.Canonicalize(new
        {
            crv = ReadString(root, "crv"),
            kty = ReadString(root, "kty"),
            x = ReadString(root, "x"),
            y = ReadString(root, "y"),
        });
        var fingerprint = $"{EvidenceCanonicalization.HashAlgorithm}:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalJwk))).ToLowerInvariant()}";
        Assert.Equal(expectedFingerprint, fingerprint);
        Assert.Equal("EC", ReadString(root, "kty"));
        Assert.Equal("P-256", ReadString(root, "crv"));

        var key = ECDsa.Create();
        key.ImportParameters(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint
            {
                X = Base64UrlDecode(ReadString(root, "x")),
                Y = Base64UrlDecode(ReadString(root, "y")),
            },
        });
        return key;
    }

    private static string RecomputeResultHash(GoldenProofClaims claims) =>
        EvidenceCanonicalization.HashCanonical(
            EvidenceSignatureDefaults.ResultHashLabel,
            new
            {
                proofVersion = claims.ProofVersion,
                purpose = claims.Purpose,
                sessionId = claims.SessionId,
                identityRef = claims.IdentityRef,
                packageId = claims.PackageId,
                packageVersion = claims.PackageVersion,
                canonicalizationScheme = claims.CanonicalizationScheme,
                hashAlgorithm = claims.HashAlgorithm,
                result = claims.Result,
                assuranceLevel = claims.AssuranceLevel,
                requiredChecks = claims.RequiredChecks,
                completedChecks = claims.CompletedChecks,
                evidenceEngines = claims.EvidenceEngines,
                signedAt = claims.SignedAt,
                challenge = claims.Challenge,
                signedManifestHash = claims.SignedManifestHash,
            });

    private static string RecomputeIdentityRef(IdentityRefInputs inputs)
    {
        var preimage = Encoding.UTF8.GetBytes($"{inputs.Label}\n{inputs.ClientApplicationId}\n{inputs.SubjectRef}");
        return $"{EvidenceCanonicalization.HashAlgorithm}:{Convert.ToHexString(SHA256.HashData(preimage)).ToLowerInvariant()}";
    }

    private static GoldenProofClaims DecodeClaims(string jws)
    {
        var parts = jws.Split('.');
        Assert.Equal(3, parts.Length);
        return JsonSerializer.Deserialize<GoldenProofClaims>(
            Encoding.UTF8.GetString(Base64UrlDecode(parts[1])),
            JsonOptions)!;
    }

    private static GoldenVectorsFixture LoadFixture() =>
        JsonSerializer.Deserialize<GoldenVectorsFixture>(
            File.ReadAllText(FixturePath()),
            JsonOptions)!;

    private static string FixturePath() =>
        Path.Combine(RepoRoot(), FixtureRelativePath.Replace('/', Path.DirectorySeparatorChar));

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "TagEkyc.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Could not locate TagEkyc.sln.");
    }

    private static TestFixture CreateFixture(IEvidenceSigner signer)
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
        var finalization = new LocalDevInMemoryVerificationFinalizationBoundary(sessions, decisions, packages, manifests, audit);
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
            signer,
            finalization);

        return new TestFixture(sessionService, evidenceService, completionService);
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

    private static string ReadString(JsonElement root, string propertyName) =>
        root.GetProperty(propertyName).GetString() ?? string.Empty;

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + ((4 - padded.Length % 4) % 4), '=');
        return Convert.FromBase64String(padded);
    }

    private static readonly string[] ExpectedClaimNames =
    [
        "assuranceLevel",
        "canonicalizationScheme",
        "challenge",
        "completedChecks",
        "evidenceEngines",
        "hashAlgorithm",
        "identityRef",
        "packageId",
        "packageVersion",
        "proofVersion",
        "purpose",
        "requiredChecks",
        "result",
        "resultHash",
        "resultHashAlgorithm",
        "resultHashCanonicalizationScheme",
        "sessionId",
        "signedAt",
        "signedManifestHash",
    ];

    private sealed record TestFixture(
        VerificationSessionApplicationService SessionService,
        VerificationEvidenceApplicationService EvidenceService,
        VerificationCompletionApplicationService CompletionService);

    private sealed record GoldenVectorsFixture(
        string SchemaVersion,
        IReadOnlyList<GoldenVector> Vectors);

    private sealed record GoldenVector(
        string Name,
        string AssuranceTarget,
        string GeneratedByCommit,
        string GeneratedAt,
        string SourceTestMethod,
        bool SyntheticNoPii,
        string SignerKid,
        string Kid,
        string PublicKeyFingerprint,
        ExpectedTrustAnchor ExpectedTrustAnchor,
        string PublicKeyJwk,
        string Jws,
        SignatureMetadata Signature,
        GoldenConstants Constants,
        IdentityRefInputs IdentityRefInputs,
        ChallengeInput ChallengeInput,
        GoldenProofClaims Claims);

    private sealed record ExpectedTrustAnchor(
        string Kid,
        string PublicKeyFingerprint);

    private sealed record SignatureMetadata(
        string Format,
        string Scheme,
        string Algorithm);

    private sealed record GoldenConstants(
        string ProofVersion,
        string ResultHashAlgorithm,
        string ResultHashCanonicalizationScheme,
        string ResultHashLabel,
        string IdentityRefLabel);

    private sealed record IdentityRefInputs(
        string ClientApplicationId,
        string SubjectRef,
        string Label);

    private sealed record ChallengeInput(
        string ExpectedChallenge);

    private sealed record GoldenProofClaims(
        string ProofVersion,
        string Purpose,
        string SessionId,
        string IdentityRef,
        string PackageId,
        string PackageVersion,
        string CanonicalizationScheme,
        string HashAlgorithm,
        string Result,
        string AssuranceLevel,
        IReadOnlyList<string> RequiredChecks,
        IReadOnlyList<string> CompletedChecks,
        IReadOnlyList<EvidenceProofEngineRefDto> EvidenceEngines,
        string SignedAt,
        string Challenge,
        string SignedManifestHash,
        string ResultHash,
        string ResultHashAlgorithm,
        string ResultHashCanonicalizationScheme);
}
