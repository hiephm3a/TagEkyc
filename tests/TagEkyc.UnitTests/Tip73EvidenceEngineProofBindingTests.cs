using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TagEkyc.Application.Ports;
using TagEkyc.Infrastructure.Signing;

namespace TagEkyc.UnitTests;

public sealed class Tip73EvidenceEngineProofBindingTests
{
    [Fact]
    public async Task Liveness_engine_ref_is_bound_by_result_hash_and_es256_jws_not_manifest_hash()
    {
        using var signer = new LocalDevEs256JwsEvidenceSigner();
        var request = ProofRequest(
            engineName: "silent-face-minifasnet",
            engineVersion: "minifasnet-v2+onnxruntime-1.20.1");
        var mutatedRequest = ProofRequest(
            engineName: "silent-face-minifasnet-typo",
            engineVersion: "minifasnet-v2+onnxruntime-1.20.1");

        var envelope = await signer.SignProofAsync(request, CancellationToken.None);
        var signedPayloadJson = DecodeJwsPayload(envelope.SignatureValue);
        var mutatedPayloadJson = Es256JwsEvidenceSignatureBuilder.BuildProofClaimJson(
            mutatedRequest,
            envelope.SignedAt);
        var signedPayload = JsonDocument.Parse(signedPayloadJson).RootElement;
        var mutatedPayload = JsonDocument.Parse(mutatedPayloadJson).RootElement;

        Assert.Equal("sha256:manifest-tip73", signedPayload.GetProperty("signedManifestHash").GetString());
        Assert.Equal(
            signedPayload.GetProperty("signedManifestHash").GetString(),
            mutatedPayload.GetProperty("signedManifestHash").GetString());
        Assert.NotEqual(
            signedPayload.GetProperty("resultHash").GetString(),
            mutatedPayload.GetProperty("resultHash").GetString());
        Assert.Equal(
            "silent-face-minifasnet",
            signedPayload.GetProperty("evidenceEngines")[0].GetProperty("engineName").GetString());

        Assert.True(VerifyCompactJws(envelope.SignatureValue, signer.ExportPublicKey()));
        Assert.False(VerifyCompactJws(ReplaceJwsPayload(envelope.SignatureValue, mutatedPayloadJson), signer.ExportPublicKey()));
    }

    private static EvidenceProofSignatureRequest ProofRequest(string engineName, string engineVersion) =>
        new(
            PackageId: "pkg-tip73",
            PackageVersion: "2026.06",
            CanonicalizationScheme: "rfc8785-jcs-v1",
            HashAlgorithm: "sha256",
            Purpose: EvidenceSignatureDefaults.PurposeNeutralEkycProof,
            SessionId: "session-tip73",
            IdentityRef: "subject-ref",
            Result: "Passed",
            AssuranceLevel: "High",
            RequiredChecks: ["Liveness"],
            CompletedChecks: ["Liveness"],
            EvidenceEngines:
            [
                new EvidenceProofEngineRef(
                    nameof(EvidenceResultType.Liveness),
                    "evidence-liveness-tip73",
                    engineName,
                    engineVersion,
                    "Liveness"),
            ],
            Challenge: "opaque challenge",
            SignedManifestHash: "sha256:manifest-tip73");

    private static string DecodeJwsPayload(string jws) =>
        Encoding.UTF8.GetString(Base64UrlDecode(jws.Split('.')[1]));

    private static string ReplaceJwsPayload(string jws, string payloadJson)
    {
        var parts = jws.Split('.');
        return $"{parts[0]}.{Base64Url(Encoding.UTF8.GetBytes(payloadJson))}.{parts[2]}";
    }

    private static bool VerifyCompactJws(string jws, ECDsa publicKey)
    {
        var parts = jws.Split('.');
        return publicKey.VerifyData(
            Encoding.ASCII.GetBytes($"{parts[0]}.{parts[1]}"),
            Base64UrlDecode(parts[2]),
            HashAlgorithmName.SHA256,
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value
            .Replace('-', '+')
            .Replace('_', '/')
            .PadRight(value.Length + ((4 - value.Length % 4) % 4), '=');
        return Convert.FromBase64String(padded);
    }

    private enum EvidenceResultType
    {
        Liveness,
    }
}
