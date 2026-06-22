using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Domain;
using TagEkyc.Infrastructure.Signing;

namespace TagEkyc.IntegrationTests;

internal static class Tip66EvidenceSignatureTestVerifier
{
    public static bool Verify(
        EvidenceSignatureEnvelope envelope,
        EvidenceSignatureRequest expectedRequest,
        ECDsa publicKey)
    {
        if (!string.Equals(envelope.SignatureFormat, EvidenceSignatureDefaults.FormatJws, StringComparison.Ordinal) ||
            !string.Equals(envelope.SignatureScheme, EvidenceSignatureDefaults.SchemeJwsEs256V1, StringComparison.Ordinal) ||
            !string.Equals(envelope.SignatureAlgorithm, EvidenceSignatureDefaults.AlgorithmEs256, StringComparison.Ordinal))
        {
            return false;
        }

        var parts = envelope.SignatureValue.Split('.');
        if (parts.Length != 3)
        {
            return false;
        }

        using var headerDocument = JsonDocument.Parse(Encoding.UTF8.GetString(Base64UrlDecode(parts[0])));
        var header = headerDocument.RootElement;
        if (!header.TryGetProperty("alg", out var alg) ||
            !header.TryGetProperty("kid", out var kid) ||
            !string.Equals(alg.GetString(), envelope.SignatureAlgorithm, StringComparison.Ordinal) ||
            !string.Equals(kid.GetString(), envelope.KeyId, StringComparison.Ordinal))
        {
            return false;
        }

        var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
        if (!string.Equals(payloadJson, BuildSignedClaimJson(expectedRequest, envelope.SignedAt), StringComparison.Ordinal))
        {
            return false;
        }

        return VerifySignatureOnly(envelope, publicKey);
    }

    public static bool VerifySignatureOnly(EvidenceSignatureEnvelope envelope, ECDsa publicKey)
    {
        var parts = envelope.SignatureValue.Split('.');
        if (parts.Length != 3)
        {
            return false;
        }

        return publicKey.VerifyData(
            Encoding.ASCII.GetBytes($"{parts[0]}.{parts[1]}"),
            Base64UrlDecode(parts[2]),
            HashAlgorithmName.SHA256,
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
    }

    public static string ReplaceHeaderAlg(string jws, string algorithm)
    {
        var parts = jws.Split('.');
        var headerJson = EvidenceCanonicalization.Canonicalize(new
        {
            alg = algorithm,
            kid = LocalDevEs256JwsEvidenceSigner.DefaultKeyId,
        });
        return $"{Base64Url(Encoding.UTF8.GetBytes(headerJson))}.{parts[1]}.{parts[2]}";
    }

    public static string ReplacePayload(string jws, EvidenceSignatureRequest request, DateTimeOffset signedAt)
    {
        var parts = jws.Split('.');
        return $"{parts[0]}.{Base64Url(Encoding.UTF8.GetBytes(BuildSignedClaimJson(request, signedAt)))}.{parts[2]}";
    }

    public static string ReplaceSignature(string jws)
    {
        var parts = jws.Split('.');
        var signature = Base64UrlDecode(parts[2]);
        signature[0] ^= 0x01;
        return $"{parts[0]}.{parts[1]}.{Base64Url(signature)}";
    }

    private static string BuildSignedClaimJson(EvidenceSignatureRequest request, DateTimeOffset signedAt) =>
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

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + ((4 - padded.Length % 4) % 4), '=');
        return Convert.FromBase64String(padded);
    }
}
