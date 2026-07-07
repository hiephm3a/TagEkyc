using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Domain;

namespace TagEkyc.Infrastructure.Signing;

public sealed class ProductionTrialP12Es256JwsEvidenceSigner :
    IEvidenceSigner,
    IEs256PublicJwkSource,
    IEvidenceSignerStartupValidator,
    IDisposable
{
    private const string SelfTestPayload = "tagekyc-production-trial-p12-es256-startup-self-test-v1";

    private readonly ECDsa key;
    private readonly X509Certificate2 certificate;

    public ProductionTrialP12Es256JwsEvidenceSigner(ProductionTrialP12Es256JwsEvidenceSignerOptions options)
    {
        var validated = ValidateOptions(options);
        KeyId = validated.KeyId!;
        var password = ProductionTrialP12SecretResolver.Resolve(validated.P12PasswordSecretRef!);

        try
        {
#pragma warning disable SYSLIB0057
            certificate = new X509Certificate2(
                validated.P12Path!,
                password,
                X509KeyStorageFlags.EphemeralKeySet);
#pragma warning restore SYSLIB0057

            key = certificate.GetECDsaPrivateKey()
                ?? throw new InvalidOperationException("PROD_SIGNING_P12_PRIVATE_KEY_REQUIRED");
        }
        catch (Exception exception) when (exception is not InvalidOperationException { Message: "PROD_SIGNING_P12_PRIVATE_KEY_REQUIRED" })
        {
            throw new InvalidOperationException("PROD_SIGNING_P12_LOAD_FAILED");
        }
    }

    public string KeyId { get; }

    public string PublicKeyJwk => ExportPublicKeyJwk();

    public string PublicKeyFingerprint => Es256JwsEvidenceSignatureBuilder.ComputePublicKeyFingerprint(PublicKeyJwk);

    public Task<EvidenceSignatureEnvelope> SignAsync(
        EvidenceSignatureRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var signedAt = Es256JwsEvidenceSignatureBuilder.TruncateToMicroseconds(DateTimeOffset.UtcNow);
        var payloadJson = Es256JwsEvidenceSignatureBuilder.BuildSignedClaimJson(request, signedAt);
        var jws = SignPayload(payloadJson);

        return Task.FromResult(new EvidenceSignatureEnvelope(
            SignaturePlaceholderStatus.Signed,
            EvidenceSignatureDefaults.FormatJws,
            EvidenceSignatureDefaults.SchemeJwsEs256V1,
            EvidenceSignatureDefaults.AlgorithmEs256,
            KeyId,
            signedAt,
            jws));
    }

    public Task<EvidenceSignatureEnvelope> SignProofAsync(
        EvidenceProofSignatureRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var signedAt = Es256JwsEvidenceSignatureBuilder.TruncateToMicroseconds(DateTimeOffset.UtcNow);
        var payloadJson = Es256JwsEvidenceSignatureBuilder.BuildProofClaimJson(request, signedAt);
        var jws = SignPayload(payloadJson);
        var publicKeyJwk = ExportPublicKeyJwk();

        return Task.FromResult(new EvidenceSignatureEnvelope(
            SignaturePlaceholderStatus.Signed,
            EvidenceSignatureDefaults.FormatJws,
            EvidenceSignatureDefaults.SchemeJwsEs256V1,
            EvidenceSignatureDefaults.AlgorithmEs256,
            KeyId,
            signedAt,
            jws,
            publicKeyJwk,
            Es256JwsEvidenceSignatureBuilder.ComputePublicKeyFingerprint(publicKeyJwk)));
    }

    public void ValidateStartup(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var jws = SignPayload(EvidenceCanonicalization.Canonicalize(new { selfTest = SelfTestPayload }));
        var parts = jws.Split('.');
        if (parts.Length != 3)
        {
            throw new InvalidOperationException("PROD_SIGNING_P12_SELF_TEST_FAILED");
        }

        using var publicKey = certificate.GetECDsaPublicKey()
            ?? throw new InvalidOperationException("PROD_SIGNING_P12_PUBLIC_KEY_REQUIRED");

        if (!publicKey.VerifyData(
                Encoding.ASCII.GetBytes($"{parts[0]}.{parts[1]}"),
                Base64UrlDecode(parts[2]),
                HashAlgorithmName.SHA256,
                DSASignatureFormat.IeeeP1363FixedFieldConcatenation))
        {
            throw new InvalidOperationException("PROD_SIGNING_P12_SELF_TEST_FAILED");
        }
    }

    public void Dispose()
    {
        key.Dispose();
        certificate.Dispose();
    }

    private string SignPayload(string payloadJson)
    {
        var headerJson = Es256JwsEvidenceSignatureBuilder.BuildProtectedHeaderJson(KeyId);
        var signingInput = Es256JwsEvidenceSignatureBuilder.BuildSigningInput(headerJson, payloadJson);
        var signature = key.SignData(
            Encoding.ASCII.GetBytes(signingInput),
            HashAlgorithmName.SHA256,
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        return Es256JwsEvidenceSignatureBuilder.BuildCompactJws(signingInput, signature);
    }

    private string ExportPublicKeyJwk()
    {
        using var publicKey = certificate.GetECDsaPublicKey()
            ?? throw new InvalidOperationException("PROD_SIGNING_P12_PUBLIC_KEY_REQUIRED");
        var parameters = publicKey.ExportParameters(includePrivateParameters: false);
        return Es256JwsEvidenceSignatureBuilder.BuildPublicJwk(
            parameters.Q.X ?? throw new InvalidOperationException("PROD_SIGNING_P12_PUBLIC_KEY_INVALID"),
            parameters.Q.Y ?? throw new InvalidOperationException("PROD_SIGNING_P12_PUBLIC_KEY_INVALID"));
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + ((4 - padded.Length % 4) % 4), '=');
        return Convert.FromBase64String(padded);
    }

    private static ProductionTrialP12Es256JwsEvidenceSignerOptions ValidateOptions(ProductionTrialP12Es256JwsEvidenceSignerOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.P12Path))
        {
            throw new InvalidOperationException("PROD_SIGNING_P12_PATH_REQUIRED");
        }

        if (!Path.IsPathFullyQualified(options.P12Path))
        {
            throw new InvalidOperationException("PROD_SIGNING_P12_PATH_NOT_ABSOLUTE");
        }

        if (!File.Exists(options.P12Path))
        {
            throw new InvalidOperationException("PROD_SIGNING_P12_FILE_NOT_FOUND");
        }

        if (string.IsNullOrWhiteSpace(options.P12PasswordSecretRef))
        {
            throw new InvalidOperationException("PROD_SIGNING_P12_PASSWORD_SECRET_REQUIRED");
        }

        if (string.IsNullOrWhiteSpace(options.KeyId))
        {
            throw new InvalidOperationException("PROD_SIGNING_KEY_ID_REQUIRED");
        }

        if (string.Equals(options.KeyId, LocalDevEs256JwsEvidenceSigner.DefaultKeyId, StringComparison.Ordinal) ||
            options.KeyId.StartsWith("localdev-", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("PROD_SIGNING_KEY_ID_DEV_FORBIDDEN");
        }

        if (!options.KeyId.StartsWith("tagekyc-es256-", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("PROD_SIGNING_KEY_ID_INVALID");
        }

        return options;
    }
}

public sealed class ProductionTrialP12Es256JwsEvidenceSignerOptions
{
    public string? P12Path { get; init; }

    public string? P12PasswordSecretRef { get; init; }

    public string? KeyId { get; init; }
}

public static class ProductionTrialP12SecretResolver
{
    public static string Resolve(string secretRef)
    {
        if (secretRef.StartsWith("env:", StringComparison.OrdinalIgnoreCase))
        {
            var variableName = secretRef["env:".Length..];
            var value = Environment.GetEnvironmentVariable(variableName);
            return string.IsNullOrWhiteSpace(value)
                ? throw new InvalidOperationException("PROD_SIGNING_P12_PASSWORD_SECRET_UNAVAILABLE")
                : value;
        }

        if (secretRef.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
        {
            var path = secretRef["file:".Length..];
            if (!Path.IsPathFullyQualified(path) || !File.Exists(path))
            {
                throw new InvalidOperationException("PROD_SIGNING_P12_PASSWORD_SECRET_UNAVAILABLE");
            }

            var value = File.ReadAllText(path).TrimEnd('\r', '\n');
            return string.IsNullOrWhiteSpace(value)
                ? throw new InvalidOperationException("PROD_SIGNING_P12_PASSWORD_SECRET_UNAVAILABLE")
                : value;
        }

        throw new InvalidOperationException("PROD_SIGNING_P12_PASSWORD_SECRET_REF_INVALID");
    }
}
