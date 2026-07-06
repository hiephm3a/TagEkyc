using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
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

public sealed class Tip67BNeutralProofVerificationTests
{
    [Fact]
    public async Task Verification_view_contains_self_contained_signed_neutral_proof_that_reference_verifier_accepts()
    {
        using var signer = new LocalDevEs256JwsEvidenceSigner(new LocalDevEs256JwsEvidenceSignerOptions { KeyId = "tip67b-sign-time-key-v1" });
        var fixture = CreateFixture(signer);
        var completed = await CompleteChallengeBoundSessionAsync(fixture);

        var view = await fixture.CompletionService.GetEvidencePackageVerificationViewAsync(
            BusinessCaller(),
            completed.EvidencePackageId,
            CancellationToken.None);

        Assert.True(view.IsSuccess, view.Error?.Code);
        Assert.True(Tip67BReferenceVerifier.Verify(
            view.Value!,
            "tip67b-sign-time-key-v1",
            view.Value!.PublicKeyFingerprint,
            "opaque challenge: 123"));
        Assert.Equal(EvidenceSignatureDefaults.ProofVersionNeutralV1, view.Value.ProofVersion);
        Assert.StartsWith("sha256:", view.Value.IdentityRef, StringComparison.Ordinal);
        Assert.Equal("opaque challenge: 123", view.Value.Challenge);
        Assert.Equal("client-ref-123", view.Value.ClientReference);
        Assert.Equal([RequiredCheckTypeDto.CaptureQuality], view.Value.RequiredChecks);
        Assert.Equal([RequiredCheckTypeDto.CaptureQuality], view.Value.CompletedChecks);

        var json = JsonSerializer.Serialize(view.Value);
        Assert.DoesNotContain("subject-ref", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("clientApplicationId", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("payloadHash", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("vaultRef", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"d\"", view.Value.PublicKeyJwk, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Verification_view_uses_sign_time_public_key_after_signer_rotation()
    {
        using var signingKey = new LocalDevEs256JwsEvidenceSigner(new LocalDevEs256JwsEvidenceSignerOptions { KeyId = "tip67b-old-key" });
        var fixture = CreateFixture(signingKey);
        var completed = await CompleteChallengeBoundSessionAsync(fixture);

        using var rotatedCurrentSigner = new LocalDevEs256JwsEvidenceSigner(new LocalDevEs256JwsEvidenceSignerOptions { KeyId = "tip67b-new-key" });
        var readServiceAfterRotation = fixture.WithSigner(rotatedCurrentSigner);
        var view = await readServiceAfterRotation.GetEvidencePackageVerificationViewAsync(
            BusinessCaller(),
            completed.EvidencePackageId,
            CancellationToken.None);

        Assert.True(view.IsSuccess, view.Error?.Code);
        Assert.Equal("tip67b-old-key", view.Value!.KeyId);
        Assert.True(Tip67BReferenceVerifier.Verify(
            view.Value,
            "tip67b-old-key",
            view.Value.PublicKeyFingerprint,
            "opaque challenge: 123"));
        Assert.False(Tip67BReferenceVerifier.Verify(
            view.Value with { KeyId = "tip67b-new-key" },
            "tip67b-new-key",
            view.Value.PublicKeyFingerprint,
            "opaque challenge: 123"));
    }

    [Fact]
    public async Task Reference_verifier_fails_closed_for_trust_anchor_mirror_result_hash_and_challenge_tamper()
    {
        using var signer = new LocalDevEs256JwsEvidenceSigner(new LocalDevEs256JwsEvidenceSignerOptions { KeyId = "tip67b-trusted-key" });
        var fixture = CreateFixture(signer);
        var completed = await CompleteChallengeBoundSessionAsync(fixture);
        var viewResult = await fixture.CompletionService.GetEvidencePackageVerificationViewAsync(
            BusinessCaller(),
            completed.EvidencePackageId,
            CancellationToken.None);
        var view = viewResult.Value!;

        using var attacker = new LocalDevEs256JwsEvidenceSigner(new LocalDevEs256JwsEvidenceSignerOptions { KeyId = "tip67b-attacker-key" });
        var attackerEnvelope = await attacker.SignProofAsync(ProofRequest(), CancellationToken.None);

        Assert.False(Tip67BReferenceVerifier.Verify(
            view with { PublicKeyJwk = attackerEnvelope.PublicKeyJwk!, PublicKeyFingerprint = attackerEnvelope.PublicKeyFingerprint! },
            "tip67b-trusted-key",
            view.PublicKeyFingerprint,
            "opaque challenge: 123"));
        Assert.False(Tip67BReferenceVerifier.Verify(
            view with { KeyId = "tip67b-other-key" },
            "tip67b-other-key",
            view.PublicKeyFingerprint,
            "opaque challenge: 123"));
        Assert.False(Tip67BReferenceVerifier.Verify(
            view with { PublicKeyJwk = InsertPrivateD(view.PublicKeyJwk) },
            "tip67b-trusted-key",
            view.PublicKeyFingerprint,
            "opaque challenge: 123"));
        Assert.False(Tip67BReferenceVerifier.Verify(
            view with { Result = VerificationResultDto.FailedIdentity },
            "tip67b-trusted-key",
            view.PublicKeyFingerprint,
            "opaque challenge: 123"));
        var malformedResultHashView = BuildSignedMalformedResultHashView(view, "tip67b-trusted-key");
        Assert.False(Tip67BReferenceVerifier.Verify(
            malformedResultHashView,
            "tip67b-trusted-key",
            malformedResultHashView.PublicKeyFingerprint,
            "opaque challenge: 123"));
        Assert.False(Tip67BReferenceVerifier.Verify(
            view,
            "tip67b-trusted-key",
            view.PublicKeyFingerprint,
            "different challenge"));
        Assert.False(Tip67BReferenceVerifier.Verify(
            view with { SignatureAlgorithm = "RS256" },
            "tip67b-trusted-key",
            view.PublicKeyFingerprint,
            "opaque challenge: 123"));
    }

    [Fact]
    public async Task Reference_verifier_rejects_signature_segment_tamper_under_matching_trust_anchor()
    {
        using var signer = new LocalDevEs256JwsEvidenceSigner(new LocalDevEs256JwsEvidenceSignerOptions { KeyId = "tip67b-trusted-key" });
        var fixture = CreateFixture(signer);
        var completed = await CompleteChallengeBoundSessionAsync(fixture);
        var viewResult = await fixture.CompletionService.GetEvidencePackageVerificationViewAsync(
            BusinessCaller(),
            completed.EvidencePackageId,
            CancellationToken.None);
        var view = viewResult.Value!;
        var signatureTampered = view with
        {
            SignatureValue = ReplaceSignatureSegment(view.SignatureValue),
        };

        Assert.False(Tip67BReferenceVerifier.Verify(
            signatureTampered,
            "tip67b-trusted-key",
            view.PublicKeyFingerprint,
            "opaque challenge: 123"));
    }

    [Fact]
    public void Proof_claim_result_hash_is_stable_for_shuffled_checks_and_engines()
    {
        var signedAt = new DateTimeOffset(2026, 6, 23, 1, 2, 3, TimeSpan.Zero);
        var first = ExtractResultHash(BuildProofClaimJson(
            ProofRequest(
                requiredChecks: ["Liveness", "CaptureQuality", "FaceMatch"],
                completedChecks: ["FaceMatch", "CaptureQuality"],
                evidenceEngines:
                [
                    new EvidenceProofEngineRef("FaceMatch", "bbbbbbbbbbbb5bbb8bbbbbbbbbbbbbbb", "face", "2", "FaceMatch"),
                    new EvidenceProofEngineRef("CaptureQuality", "aaaaaaaaaaaa5aaa8aaaaaaaaaaaaaaaa", "quality", "1", "CaptureQuality"),
                ]),
            signedAt));
        var second = ExtractResultHash(BuildProofClaimJson(
            ProofRequest(
                requiredChecks: ["FaceMatch", "CaptureQuality", "Liveness"],
                completedChecks: ["CaptureQuality", "FaceMatch"],
                evidenceEngines:
                [
                    new EvidenceProofEngineRef("CaptureQuality", "aaaaaaaaaaaa5aaa8aaaaaaaaaaaaaaaa", "quality", "1", "CaptureQuality"),
                    new EvidenceProofEngineRef("FaceMatch", "bbbbbbbbbbbb5bbb8bbbbbbbbbbbbbbb", "face", "2", "FaceMatch"),
                ]),
            signedAt));

        Assert.Equal(first, second);
    }

    private static async Task<CompleteVerificationSessionResponseDto> CompleteChallengeBoundSessionAsync(TestFixture fixture)
    {
        var session = await fixture.SessionService.CreateAsync(
            BusinessCaller(),
            new CreateVerificationSessionRequestDto(
                "external-tip67b",
                "subject-ref",
                "PATIENT_REGISTRATION",
                VerificationProfileDto.ChallengeBoundEkycProfile,
                [new RequiredCheckRequestDto(RequiredCheckTypeDto.CaptureQuality, Required: true, MinimumConfidence: null)],
                DateTimeOffset.UtcNow.AddMinutes(30),
                ClientReference: "client-ref-123",
                Challenge: "opaque challenge: 123",
                RequestId: "req-create",
                CorrelationId: "corr-create"),
            cancellationToken: CancellationToken.None);
        Assert.True(session.IsSuccess, session.Error?.Code);

        var artifact = await fixture.EvidenceService.AppendCaptureArtifactAsync(
            CaptureCaller(),
            session.Value!.VerificationSessionId,
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
        Assert.True(artifact.IsSuccess, artifact.Error?.Code);

        var evidence = await fixture.EvidenceService.AppendEvidenceResultAsync(
            TrustedCaller(),
            session.Value.VerificationSessionId,
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
        Assert.True(evidence.IsSuccess, evidence.Error?.Code);

        var completed = await fixture.CompletionService.CompleteAsync(
            BusinessCaller(),
            session.Value.VerificationSessionId,
            new CompleteVerificationSessionRequestDto(
                RequestId: "req-complete",
                CorrelationId: "corr-complete"),
            CancellationToken.None);
        Assert.True(completed.IsSuccess, completed.Error?.Code);
        return completed.Value!;
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
        var finalization = new LocalDevInMemoryVerificationFinalizationBoundary(sessions, decisions, packages, manifests, audit);
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
            signer,
            finalization);

        return new TestFixture(sessionService, evidenceService, completionService, sessions, artifacts, evidence, decisions, packages, manifests, audit, finalization);
    }

    private static EvidenceProofSignatureRequest ProofRequest(
        IReadOnlyList<string>? requiredChecks = null,
        IReadOnlyList<string>? completedChecks = null,
        IReadOnlyList<EvidenceProofEngineRef>? evidenceEngines = null) =>
        new(
            "11111111111151118111111111111111",
            EvidenceCanonicalization.PackageVersion,
            EvidenceCanonicalization.CanonicalizationScheme,
            EvidenceCanonicalization.HashAlgorithm,
            EvidenceSignatureDefaults.PurposeNeutralEkycProof,
            "22222222222252228222222222222222",
            "sha256:3333333333333333333333333333333333333333333333333333333333333333",
            "Passed",
            "Medium",
            requiredChecks ?? ["CaptureQuality"],
            completedChecks ?? ["CaptureQuality"],
            evidenceEngines ??
            [
                new EvidenceProofEngineRef("CaptureQuality", "44444444444454448444444444444444", "quality", "1", "CaptureQuality"),
            ],
            "opaque challenge: 123",
            "sha256:5555555555555555555555555555555555555555555555555555555555555555");

    private static string BuildProofClaimJson(EvidenceProofSignatureRequest request, DateTimeOffset signedAt)
    {
        var method = typeof(LocalDevEs256JwsEvidenceSigner).GetMethod(
            "BuildProofClaimJson",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("BuildProofClaimJson missing.");
        return (string)method.Invoke(null, [request, signedAt])!;
    }

    private static string ExtractResultHash(string payloadJson)
    {
        using var document = JsonDocument.Parse(payloadJson);
        return document.RootElement.GetProperty("resultHash").GetString()!;
    }

    private static EvidencePackageVerificationViewDto BuildSignedMalformedResultHashView(EvidencePackageVerificationViewDto template, string keyId)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var payloadJson = BuildPayloadJson(template, "sha256:0000000000000000000000000000000000000000000000000000000000000000");
        var jwk = ExportPublicKeyJwk(key);
        return template with
        {
            KeyId = keyId,
            PublicKeyJwk = jwk,
            PublicKeyFingerprint = ComputePublicKeyFingerprint(jwk),
            SignatureValue = SignJws(payloadJson, key, keyId),
            ResultHash = "sha256:0000000000000000000000000000000000000000000000000000000000000000",
        };
    }

    private static string BuildPayloadJson(EvidencePackageVerificationViewDto view, string resultHash) =>
        EvidenceCanonicalization.Canonicalize(new
        {
            proofVersion = view.ProofVersion,
            purpose = view.Purpose,
            sessionId = view.SessionId,
            identityRef = view.IdentityRef,
            packageId = view.PackageId,
            packageVersion = view.PackageVersion,
            canonicalizationScheme = view.CanonicalizationScheme,
            hashAlgorithm = view.HashAlgorithm,
            result = view.Result.ToString(),
            assuranceLevel = view.AssuranceLevel.ToString(),
            requiredChecks = view.RequiredChecks.Select(check => check.ToString()).ToArray(),
            completedChecks = view.CompletedChecks.Select(check => check.ToString()).ToArray(),
            evidenceEngines = view.EvidenceEngines,
            signedAt = EvidenceCanonicalization.FormatTimestamp(view.SignedAt),
            challenge = view.Challenge,
            signedManifestHash = view.SignedManifestHash,
            resultHash,
            resultHashAlgorithm = view.ResultHashAlgorithm,
            resultHashCanonicalizationScheme = view.ResultHashCanonicalizationScheme,
        });

    private static string InsertPrivateD(string publicKeyJwk)
    {
        using var document = JsonDocument.Parse(publicKeyJwk);
        var root = document.RootElement;
        return EvidenceCanonicalization.Canonicalize(new
        {
            kty = root.GetProperty("kty").GetString(),
            crv = root.GetProperty("crv").GetString(),
            x = root.GetProperty("x").GetString(),
            y = root.GetProperty("y").GetString(),
            d = "private-material",
        });
    }

    private static string SignJws(string payloadJson, ECDsa key, string kid)
    {
        var headerJson = EvidenceCanonicalization.Canonicalize(new { alg = EvidenceSignatureDefaults.AlgorithmEs256, kid });
        var signingInput = $"{Base64Url(Encoding.UTF8.GetBytes(headerJson))}.{Base64Url(Encoding.UTF8.GetBytes(payloadJson))}";
        var signature = key.SignData(
            Encoding.ASCII.GetBytes(signingInput),
            HashAlgorithmName.SHA256,
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        return $"{signingInput}.{Base64Url(signature)}";
    }

    private static string ExportPublicKeyJwk(ECDsa key)
    {
        var parameters = key.ExportParameters(includePrivateParameters: false);
        return EvidenceCanonicalization.Canonicalize(new
        {
            kty = "EC",
            crv = "P-256",
            x = Base64Url(parameters.Q.X!),
            y = Base64Url(parameters.Q.Y!),
        });
    }

    private static string ComputePublicKeyFingerprint(string publicKeyJwk) =>
        $"{EvidenceCanonicalization.HashAlgorithm}:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(publicKeyJwk))).ToLowerInvariant()}";

    private static string ReplaceSignatureSegment(string jws)
    {
        var parts = jws.Split('.');
        var signature = Base64UrlDecode(parts[2]);
        signature[0] ^= 0x01;
        return $"{parts[0]}.{parts[1]}.{Base64Url(signature)}";
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + ((4 - padded.Length % 4) % 4), '=');
        return Convert.FromBase64String(padded);
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

    private sealed record TestFixture(
        VerificationSessionApplicationService SessionService,
        VerificationEvidenceApplicationService EvidenceService,
        VerificationCompletionApplicationService CompletionService,
        LocalDevInMemoryVerificationSessionRepository Sessions,
        LocalDevInMemoryCaptureArtifactRepository Artifacts,
        LocalDevInMemoryEvidenceResultRepository Evidence,
        LocalDevInMemoryVerificationDecisionRepository Decisions,
        LocalDevInMemoryEvidencePackageRepository Packages,
        LocalDevInMemoryEvidenceManifestRepository Manifests,
        LocalDevInMemoryAuditEventRepository Audit,
        LocalDevInMemoryVerificationFinalizationBoundary Finalization)
    {
        public VerificationCompletionApplicationService WithSigner(IEvidenceSigner signer) =>
            new(Sessions, Artifacts, Evidence, Decisions, Packages, Manifests, Audit, signer, Finalization);
    }
}

internal static class Tip67BReferenceVerifier
{
    public static bool Verify(
        EvidencePackageVerificationViewDto view,
        string expectedKeyId,
        string expectedPublicKeyFingerprint,
        string expectedChallenge)
    {
        try
        {
            if (view.ProofVersion != EvidenceSignatureDefaults.ProofVersionNeutralV1 ||
                view.SignatureFormat != EvidenceSignatureDefaults.FormatJws ||
                view.SignatureScheme != EvidenceSignatureDefaults.SchemeJwsEs256V1 ||
                view.SignatureAlgorithm != EvidenceSignatureDefaults.AlgorithmEs256 ||
                view.KeyId != expectedKeyId ||
                view.PublicKeyFingerprint != expectedPublicKeyFingerprint ||
                view.Challenge != expectedChallenge)
            {
                return false;
            }

            var publicKey = ParsePublicJwk(view.PublicKeyJwk, expectedPublicKeyFingerprint);
            using var key = publicKey;
            var parts = view.SignatureValue.Split('.');
            if (parts.Length != 3)
            {
                return false;
            }

            using var headerDocument = JsonDocument.Parse(Encoding.UTF8.GetString(Base64UrlDecode(parts[0])));
            var header = headerDocument.RootElement;
            if (header.GetProperty("alg").GetString() != view.SignatureAlgorithm ||
                header.GetProperty("kid").GetString() != view.KeyId)
            {
                return false;
            }

            if (!key.VerifyData(
                    Encoding.ASCII.GetBytes($"{parts[0]}.{parts[1]}"),
                    Base64UrlDecode(parts[2]),
                    HashAlgorithmName.SHA256,
                    DSASignatureFormat.IeeeP1363FixedFieldConcatenation))
            {
                return false;
            }

            using var claimDocument = JsonDocument.Parse(Encoding.UTF8.GetString(Base64UrlDecode(parts[1])));
            var claim = claimDocument.RootElement;
            return MirrorsClaim(view, claim) &&
                   RecomputeResultHash(claim) == ReadString(claim, "resultHash");
        }
        catch
        {
            return false;
        }
    }

    private static bool MirrorsClaim(EvidencePackageVerificationViewDto view, JsonElement claim) =>
        ReadString(claim, "proofVersion") == view.ProofVersion &&
        ReadString(claim, "purpose") == view.Purpose &&
        ReadString(claim, "sessionId") == view.SessionId &&
        ReadString(claim, "identityRef") == view.IdentityRef &&
        ReadString(claim, "packageId") == view.PackageId &&
        ReadString(claim, "packageVersion") == view.PackageVersion &&
        ReadString(claim, "canonicalizationScheme") == view.CanonicalizationScheme &&
        ReadString(claim, "hashAlgorithm") == view.HashAlgorithm &&
        ReadString(claim, "result") == view.Result.ToString() &&
        ReadString(claim, "assuranceLevel") == view.AssuranceLevel.ToString() &&
        ReadStringArray(claim, "requiredChecks").SequenceEqual(view.RequiredChecks.Select(check => check.ToString()), StringComparer.Ordinal) &&
        ReadStringArray(claim, "completedChecks").SequenceEqual(view.CompletedChecks.Select(check => check.ToString()), StringComparer.Ordinal) &&
        ReadEngines(claim).SequenceEqual(view.EvidenceEngines) &&
        ReadString(claim, "signedAt") == EvidenceCanonicalization.FormatTimestamp(view.SignedAt) &&
        ReadString(claim, "challenge") == view.Challenge &&
        ReadString(claim, "signedManifestHash") == view.SignedManifestHash &&
        ReadString(claim, "resultHash") == view.ResultHash &&
        ReadString(claim, "resultHashAlgorithm") == view.ResultHashAlgorithm &&
        ReadString(claim, "resultHashCanonicalizationScheme") == view.ResultHashCanonicalizationScheme;

    private static string RecomputeResultHash(JsonElement claim) =>
        EvidenceCanonicalization.HashCanonical(
            EvidenceSignatureDefaults.ResultHashLabel,
            new
            {
                proofVersion = ReadString(claim, "proofVersion"),
                purpose = ReadString(claim, "purpose"),
                sessionId = ReadString(claim, "sessionId"),
                identityRef = ReadString(claim, "identityRef"),
                packageId = ReadString(claim, "packageId"),
                packageVersion = ReadString(claim, "packageVersion"),
                canonicalizationScheme = ReadString(claim, "canonicalizationScheme"),
                hashAlgorithm = ReadString(claim, "hashAlgorithm"),
                result = ReadString(claim, "result"),
                assuranceLevel = ReadString(claim, "assuranceLevel"),
                requiredChecks = ReadStringArray(claim, "requiredChecks"),
                completedChecks = ReadStringArray(claim, "completedChecks"),
                evidenceEngines = ReadEngines(claim),
                signedAt = ReadString(claim, "signedAt"),
                challenge = ReadString(claim, "challenge"),
                signedManifestHash = ReadString(claim, "signedManifestHash"),
            });

    private static ECDsa ParsePublicJwk(string publicKeyJwk, string expectedFingerprint)
    {
        using var document = JsonDocument.Parse(publicKeyJwk);
        var root = document.RootElement;
        var allowedNames = new HashSet<string>(StringComparer.Ordinal) { "kty", "crv", "x", "y" };
        if (root.EnumerateObject().Any(property => !allowedNames.Contains(property.Name)))
        {
            throw new InvalidOperationException("Public JWK contains non-public or unsupported fields.");
        }

        var canonicalJwk = EvidenceCanonicalization.Canonicalize(new
        {
            kty = ReadString(root, "kty"),
            crv = ReadString(root, "crv"),
            x = ReadString(root, "x"),
            y = ReadString(root, "y"),
        });
        var fingerprint = $"{EvidenceCanonicalization.HashAlgorithm}:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalJwk))).ToLowerInvariant()}";
        if (fingerprint != expectedFingerprint ||
            ReadString(root, "kty") != "EC" ||
            ReadString(root, "crv") != "P-256")
        {
            throw new InvalidOperationException("Public JWK trust anchor mismatch.");
        }

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

    private static IReadOnlyList<EvidenceProofEngineRefDto> ReadEngines(JsonElement claim) =>
        claim.GetProperty("evidenceEngines").EnumerateArray()
            .Select(engine => new EvidenceProofEngineRefDto(
                ReadString(engine, "evidenceResultType"),
                ReadString(engine, "evidenceResultId"),
                ReadString(engine, "engineName"),
                ReadString(engine, "engineVersion"),
                ReadString(engine, "checkType")))
            .ToArray();

    private static IReadOnlyList<string> ReadStringArray(JsonElement root, string propertyName) =>
        root.GetProperty(propertyName).EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToArray();

    private static string ReadString(JsonElement root, string propertyName) =>
        root.GetProperty(propertyName).GetString() ?? string.Empty;

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + ((4 - padded.Length % 4) % 4), '=');
        return Convert.FromBase64String(padded);
    }
}
