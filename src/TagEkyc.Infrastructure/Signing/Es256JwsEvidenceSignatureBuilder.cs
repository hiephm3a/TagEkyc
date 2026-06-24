using System.Security.Cryptography;
using System.Text;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;

namespace TagEkyc.Infrastructure.Signing;

public static class Es256JwsEvidenceSignatureBuilder
{
    public static string BuildSignedClaimJson(EvidenceSignatureRequest request, DateTimeOffset signedAt) =>
        EvidenceCanonicalization.Canonicalize(new
        {
            purpose = request.Purpose,
            signedManifestHash = request.ManifestHash,
            packageId = request.PackageId,
            packageVersion = request.PackageVersion,
            canonicalizationScheme = request.CanonicalizationScheme,
            hashAlgorithm = request.HashAlgorithm,
            signedAt = EvidenceCanonicalization.FormatTimestamp(signedAt),
        });

    public static string BuildProofClaimJson(EvidenceProofSignatureRequest request, DateTimeOffset signedAt)
    {
        var signedAtText = EvidenceCanonicalization.FormatTimestamp(signedAt);
        var orderedRequiredChecks = request.RequiredChecks.Order(StringComparer.Ordinal).ToArray();
        var orderedCompletedChecks = request.CompletedChecks.Order(StringComparer.Ordinal).ToArray();
        var orderedEngines = request.EvidenceEngines
            .OrderBy(engine => engine.EvidenceResultType, StringComparer.Ordinal)
            .ThenBy(engine => engine.EvidenceResultId, StringComparer.Ordinal)
            .ToArray();
        var resultHashPreimage = new
        {
            proofVersion = EvidenceSignatureDefaults.ProofVersionNeutralV1,
            purpose = request.Purpose,
            sessionId = request.SessionId,
            identityRef = request.IdentityRef,
            packageId = request.PackageId,
            packageVersion = request.PackageVersion,
            canonicalizationScheme = request.CanonicalizationScheme,
            hashAlgorithm = request.HashAlgorithm,
            result = request.Result,
            assuranceLevel = request.AssuranceLevel,
            requiredChecks = orderedRequiredChecks,
            completedChecks = orderedCompletedChecks,
            evidenceEngines = orderedEngines,
            signedAt = signedAtText,
            challenge = request.Challenge,
            signedManifestHash = request.SignedManifestHash,
        };
        var resultHash = EvidenceCanonicalization.HashCanonical(
            EvidenceSignatureDefaults.ResultHashLabel,
            resultHashPreimage);

        return EvidenceCanonicalization.Canonicalize(new
        {
            proofVersion = EvidenceSignatureDefaults.ProofVersionNeutralV1,
            purpose = request.Purpose,
            sessionId = request.SessionId,
            identityRef = request.IdentityRef,
            packageId = request.PackageId,
            packageVersion = request.PackageVersion,
            canonicalizationScheme = request.CanonicalizationScheme,
            hashAlgorithm = request.HashAlgorithm,
            result = request.Result,
            assuranceLevel = request.AssuranceLevel,
            requiredChecks = orderedRequiredChecks,
            completedChecks = orderedCompletedChecks,
            evidenceEngines = orderedEngines,
            signedAt = signedAtText,
            challenge = request.Challenge,
            signedManifestHash = request.SignedManifestHash,
            resultHash,
            resultHashAlgorithm = EvidenceSignatureDefaults.ResultHashAlgorithmSha256,
            resultHashCanonicalizationScheme = EvidenceSignatureDefaults.ResultHashCanonicalizationSchemeJcsV1,
        });
    }

    public static string BuildProtectedHeaderJson(string kid) =>
        EvidenceCanonicalization.Canonicalize(new
        {
            alg = EvidenceSignatureDefaults.AlgorithmEs256,
            kid,
        });

    public static string BuildSigningInput(string protectedHeaderJson, string payloadJson) =>
        $"{Base64Url(Encoding.UTF8.GetBytes(protectedHeaderJson))}.{Base64Url(Encoding.UTF8.GetBytes(payloadJson))}";

    public static string BuildCompactJws(string signingInput, byte[] signature) =>
        $"{signingInput}.{Base64Url(signature)}";

    public static string BuildPublicJwk(byte[] x, byte[] y) =>
        EvidenceCanonicalization.Canonicalize(new
        {
            kty = "EC",
            crv = "P-256",
            x = Base64Url(x),
            y = Base64Url(y),
        });

    public static string ComputePublicKeyFingerprint(string publicKeyJwk) =>
        $"{EvidenceSignatureDefaults.ResultHashAlgorithmSha256}:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(publicKeyJwk))).ToLowerInvariant()}";

    public static DateTimeOffset TruncateToMicroseconds(DateTimeOffset value)
    {
        const long ticksPerMicrosecond = TimeSpan.TicksPerMillisecond / 1000;
        var utc = value.ToUniversalTime();
        return new DateTimeOffset(
            utc.Ticks - (utc.Ticks % ticksPerMicrosecond),
            TimeSpan.Zero);
    }

    public static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
