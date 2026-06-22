using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Domain;
using TagEkyc.Infrastructure.Signing;

namespace TagEkyc.IntegrationTests;

public sealed class Tip66EvidenceSigningTests
{
    [Fact]
    public async Task Localdev_es256_signer_produces_attached_claim_jws_that_verifies()
    {
        using var signer = new LocalDevEs256JwsEvidenceSigner();
        using var publicKey = signer.ExportPublicKey();
        var request = SignedRequest();

        var envelope = await signer.SignAsync(request, CancellationToken.None);

        Assert.Equal(SignaturePlaceholderStatus.Signed, envelope.SignatureStatus);
        Assert.Equal(EvidenceSignatureDefaults.FormatJws, envelope.SignatureFormat);
        Assert.Equal(EvidenceSignatureDefaults.SchemeJwsEs256V1, envelope.SignatureScheme);
        Assert.Equal(EvidenceSignatureDefaults.AlgorithmEs256, envelope.SignatureAlgorithm);
        Assert.Equal(LocalDevEs256JwsEvidenceSigner.DefaultKeyId, envelope.KeyId);
        Assert.Equal(3, envelope.SignatureValue.Split('.').Length);
        Assert.True(Tip66EvidenceSignatureTestVerifier.Verify(envelope, request, publicKey));
    }

    [Fact]
    public async Task Localdev_es256_signer_fails_closed_for_wrong_key_alg_scheme_tamper_and_claim_mismatch()
    {
        using var signer = new LocalDevEs256JwsEvidenceSigner();
        using var publicKey = signer.ExportPublicKey();
        using var wrongKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = SignedRequest();
        var envelope = await signer.SignAsync(request, CancellationToken.None);

        Assert.False(Tip66EvidenceSignatureTestVerifier.Verify(envelope, request, wrongKey));
        Assert.False(Tip66EvidenceSignatureTestVerifier.Verify(
            envelope with { SignatureAlgorithm = "RS256" },
            request,
            publicKey));
        Assert.False(Tip66EvidenceSignatureTestVerifier.Verify(
            envelope with { SignatureScheme = "jws-rs256-v1" },
            request,
            publicKey));
        Assert.False(Tip66EvidenceSignatureTestVerifier.Verify(
            envelope with { SignatureValue = Tip66EvidenceSignatureTestVerifier.ReplaceHeaderAlg(envelope.SignatureValue, "RS256") },
            request,
            publicKey));
        var contentTampered = envelope with
        {
            SignatureValue = Tip66EvidenceSignatureTestVerifier.ReplacePayload(
                envelope.SignatureValue,
                SignedRequest(manifestHash: "sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"),
                envelope.SignedAt),
        };
        Assert.False(Tip66EvidenceSignatureTestVerifier.Verify(
            contentTampered,
            request,
            publicKey));
        Assert.False(Tip66EvidenceSignatureTestVerifier.VerifySignatureOnly(contentTampered, publicKey));
        var signatureTampered = envelope with
        {
            SignatureValue = Tip66EvidenceSignatureTestVerifier.ReplaceSignature(envelope.SignatureValue),
        };
        Assert.False(Tip66EvidenceSignatureTestVerifier.Verify(signatureTampered, request, publicKey));
        Assert.False(Tip66EvidenceSignatureTestVerifier.VerifySignatureOnly(signatureTampered, publicKey));
        Assert.False(Tip66EvidenceSignatureTestVerifier.Verify(
            envelope with { SignedAt = envelope.SignedAt.AddTicks(1) },
            request,
            publicKey));
        Assert.False(Tip66EvidenceSignatureTestVerifier.Verify(
            envelope,
            SignedRequest(packageId: "22222222222252228222222222222222"),
            publicKey));
    }

    [Fact]
    public async Task Localdev_es256_signer_can_load_configured_p12_key()
    {
        var p12Path = Path.Combine(Path.GetTempPath(), $"tagekyc-tip66-{Guid.NewGuid():N}.p12");
        const string password = "tip66-test-password";
        await File.WriteAllBytesAsync(p12Path, CreateP12(password));

        try
        {
            using var signer = new LocalDevEs256JwsEvidenceSigner(new LocalDevEs256JwsEvidenceSignerOptions
            {
                P12Path = p12Path,
                P12Password = password,
                KeyId = "test-p12-es256-v1",
            });
            using var publicKey = signer.ExportPublicKey();
            var request = SignedRequest();

            var envelope = await signer.SignAsync(request, CancellationToken.None);

            Assert.Equal("test-p12-es256-v1", envelope.KeyId);
            Assert.True(Tip66EvidenceSignatureTestVerifier.Verify(envelope, request, publicKey));
        }
        finally
        {
            File.Delete(p12Path);
        }
    }

    private static EvidenceSignatureRequest SignedRequest(
        string packageId = "11111111111151118111111111111111",
        string manifestHash = "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa") =>
        new(
            packageId,
            manifestHash,
            EvidenceCanonicalization.PackageVersion,
            EvidenceCanonicalization.CanonicalizationScheme,
            EvidenceCanonicalization.HashAlgorithm,
            EvidenceSignatureDefaults.PurposeEvidencePackageManifest);

    private static byte[] CreateP12(string password)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest(
            "CN=tagekyc-tip66-dev",
            key,
            HashAlgorithmName.SHA256);
        using var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow.AddDays(1));
        return certificate.Export(X509ContentType.Pkcs12, password);
    }
}
