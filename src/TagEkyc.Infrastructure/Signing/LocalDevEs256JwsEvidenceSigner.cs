using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using TagEkyc.Application.Ports;
using TagEkyc.Domain;

namespace TagEkyc.Infrastructure.Signing;

public sealed class LocalDevEs256JwsEvidenceSigner : IEvidenceSigner, IEs256PublicJwkSource, IDisposable
{
    public const string DefaultKeyId = "localdev-es256-v1";

    private readonly ECDsa key;
    private readonly string keyId;
    private readonly X509Certificate2? certificate;

    public LocalDevEs256JwsEvidenceSigner()
        : this(new LocalDevEs256JwsEvidenceSignerOptions())
    {
    }

    public LocalDevEs256JwsEvidenceSigner(LocalDevEs256JwsEvidenceSignerOptions options)
    {
        var resolved = CreateConfiguredOrDefaultDevKey(options);
        key = resolved.Key;
        keyId = resolved.KeyId;
        certificate = resolved.Certificate;
    }

    internal LocalDevEs256JwsEvidenceSigner(ECDsa key, string keyId)
    {
        this.key = key;
        this.keyId = keyId;
    }

    private static (ECDsa Key, string KeyId, X509Certificate2? Certificate) CreateConfiguredOrDefaultDevKey(LocalDevEs256JwsEvidenceSignerOptions options)
    {
        var keyId = string.IsNullOrWhiteSpace(options.KeyId) ? DefaultKeyId : options.KeyId;
        if (string.IsNullOrWhiteSpace(options.P12Path))
        {
            return (CreateDefaultDevKey(), keyId, Certificate: null);
        }

        if (!File.Exists(options.P12Path))
        {
            throw new FileNotFoundException("Configured TIP-66 ES256 P12 key file was not found.", options.P12Path);
        }

#pragma warning disable SYSLIB0057
        var certificate = new X509Certificate2(
            options.P12Path,
            options.P12Password,
            X509KeyStorageFlags.EphemeralKeySet);
#pragma warning restore SYSLIB0057

        var certificateKey = certificate.GetECDsaPrivateKey()
            ?? throw new InvalidOperationException("Configured TIP-66 P12 key must contain an ECDSA private key.");
        return (certificateKey, keyId, certificate);
    }

    public Task<EvidenceSignatureEnvelope> SignAsync(
        EvidenceSignatureRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var signedAt = Es256JwsEvidenceSignatureBuilder.TruncateToMicroseconds(DateTimeOffset.UtcNow);
        var payloadJson = BuildSignedClaimJson(request, signedAt);
        var jws = SignPayload(payloadJson);

        return Task.FromResult(new EvidenceSignatureEnvelope(
            SignaturePlaceholderStatus.Signed,
            EvidenceSignatureDefaults.FormatJws,
            EvidenceSignatureDefaults.SchemeJwsEs256V1,
            EvidenceSignatureDefaults.AlgorithmEs256,
            keyId,
            signedAt,
            jws));
    }

    public Task<EvidenceSignatureEnvelope> SignProofAsync(
        EvidenceProofSignatureRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var signedAt = Es256JwsEvidenceSignatureBuilder.TruncateToMicroseconds(DateTimeOffset.UtcNow);
        var payloadJson = BuildProofClaimJson(request, signedAt);
        var jws = SignPayload(payloadJson);
        var publicKeyJwk = ExportPublicKeyJwk();

        return Task.FromResult(new EvidenceSignatureEnvelope(
            SignaturePlaceholderStatus.Signed,
            EvidenceSignatureDefaults.FormatJws,
            EvidenceSignatureDefaults.SchemeJwsEs256V1,
            EvidenceSignatureDefaults.AlgorithmEs256,
            keyId,
            signedAt,
            jws,
            publicKeyJwk,
            Es256JwsEvidenceSignatureBuilder.ComputePublicKeyFingerprint(publicKeyJwk)));
    }

    public ECDsa ExportPublicKey()
    {
        if (certificate is not null)
        {
            return certificate.GetECDsaPublicKey()
                ?? throw new InvalidOperationException("Configured TIP-66 P12 certificate must contain an ECDSA public key.");
        }

        var parameters = key.ExportParameters(includePrivateParameters: false);
        var publicKey = ECDsa.Create();
        publicKey.ImportParameters(parameters);
        return publicKey;
    }

    public string KeyId => keyId;

    public string PublicKeyJwk => ExportPublicKeyJwk();

    public string PublicKeyFingerprint => Es256JwsEvidenceSignatureBuilder.ComputePublicKeyFingerprint(PublicKeyJwk);

    public void Dispose()
    {
        key.Dispose();
        certificate?.Dispose();
    }

    private static string BuildSignedClaimJson(EvidenceSignatureRequest request, DateTimeOffset signedAt) =>
        Es256JwsEvidenceSignatureBuilder.BuildSignedClaimJson(request, signedAt);

    private static string BuildProofClaimJson(EvidenceProofSignatureRequest request, DateTimeOffset signedAt)
    {
        return Es256JwsEvidenceSignatureBuilder.BuildProofClaimJson(request, signedAt);
    }

    private string SignPayload(string payloadJson)
    {
        var headerJson = Es256JwsEvidenceSignatureBuilder.BuildProtectedHeaderJson(keyId);
        var signingInput = Es256JwsEvidenceSignatureBuilder.BuildSigningInput(headerJson, payloadJson);
        var signature = key.SignData(
            Encoding.ASCII.GetBytes(signingInput),
            HashAlgorithmName.SHA256,
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        return Es256JwsEvidenceSignatureBuilder.BuildCompactJws(signingInput, signature);
    }

    private string ExportPublicKeyJwk()
    {
        var parameters = key.ExportParameters(includePrivateParameters: false);
        return Es256JwsEvidenceSignatureBuilder.BuildPublicJwk(
            parameters.Q.X ?? throw new InvalidOperationException("ECDSA public key is missing x."),
            parameters.Q.Y ?? throw new InvalidOperationException("ECDSA public key is missing y."));
    }

    private static ECDsa CreateDefaultDevKey() => ECDsa.Create(ECCurve.NamedCurves.nistP256);

    private static string Base64Url(byte[] bytes) => Es256JwsEvidenceSignatureBuilder.Base64Url(bytes);
}
