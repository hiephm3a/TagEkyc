using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Domain;

namespace TagEkyc.Infrastructure.Signing;

public sealed class LocalDevEs256JwsEvidenceSigner : IEvidenceSigner, IDisposable
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

        var signedAt = TruncateToMicroseconds(DateTimeOffset.UtcNow);
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

        var signedAt = TruncateToMicroseconds(DateTimeOffset.UtcNow);
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
            ComputePublicKeyFingerprint(publicKeyJwk)));
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

    public void Dispose()
    {
        key.Dispose();
        certificate?.Dispose();
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

    private static string BuildProofClaimJson(EvidenceProofSignatureRequest request, DateTimeOffset signedAt)
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

    private string SignPayload(string payloadJson)
    {
        var headerJson = EvidenceCanonicalization.Canonicalize(new
        {
            alg = EvidenceSignatureDefaults.AlgorithmEs256,
            kid = keyId,
        });
        var signingInput = $"{Base64Url(Encoding.UTF8.GetBytes(headerJson))}.{Base64Url(Encoding.UTF8.GetBytes(payloadJson))}";
        var signature = key.SignData(
            Encoding.ASCII.GetBytes(signingInput),
            HashAlgorithmName.SHA256,
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        return $"{signingInput}.{Base64Url(signature)}";
    }

    private string ExportPublicKeyJwk()
    {
        var parameters = key.ExportParameters(includePrivateParameters: false);
        return EvidenceCanonicalization.Canonicalize(new
        {
            kty = "EC",
            crv = "P-256",
            x = Base64Url(parameters.Q.X ?? throw new InvalidOperationException("ECDSA public key is missing x.")),
            y = Base64Url(parameters.Q.Y ?? throw new InvalidOperationException("ECDSA public key is missing y.")),
        });
    }

    private static string ComputePublicKeyFingerprint(string publicKeyJwk) =>
        $"{EvidenceSignatureDefaults.ResultHashAlgorithmSha256}:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(publicKeyJwk))).ToLowerInvariant()}";

    private static DateTimeOffset TruncateToMicroseconds(DateTimeOffset value)
    {
        const long ticksPerMicrosecond = TimeSpan.TicksPerMillisecond / 1000;
        var utc = value.ToUniversalTime();
        return new DateTimeOffset(
            utc.Ticks - (utc.Ticks % ticksPerMicrosecond),
            TimeSpan.Zero);
    }

    private static ECDsa CreateDefaultDevKey() => ECDsa.Create(ECCurve.NamedCurves.nistP256);

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
