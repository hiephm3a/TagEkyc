using System.Security.Cryptography;
using System.Text.Json;
using TagEkyc.Application.Ports;
using TagEkyc.Domain;

namespace TagEkyc.Infrastructure.Signing;

public interface IEs256PublicJwkSource
{
    string KeyId { get; }

    string PublicKeyJwk { get; }

    string PublicKeyFingerprint { get; }
}

public interface IEvidenceSignerStartupValidator
{
    void ValidateStartup(CancellationToken cancellationToken = default);
}

public interface IEs256JwksProvider
{
    Es256JwksDocument GetJwks();

    void ValidateStartup(CancellationToken cancellationToken = default);
}

public sealed record Es256JwksDocument(IReadOnlyList<Es256Jwk> Keys);

public sealed record Es256Jwk(
    string Kid,
    string Kty,
    string Crv,
    string X,
    string Y,
    string Use,
    string Alg,
    string Fingerprint,
    DateTimeOffset? NotBefore = null,
    DateTimeOffset? NotAfter = null,
    DateTimeOffset? RetiredAt = null);

public sealed class Es256JwksOptions
{
    public string? PreviousKeysFile { get; init; }

    public int? PreviousKeyOverlapDays { get; init; }
}

public sealed class Es256JwksProvider(IEs256PublicJwkSource currentKey, Es256JwksOptions options) : IEs256JwksProvider
{
    public Es256JwksDocument GetJwks()
    {
        var current = FromPublicJwk(currentKey.KeyId, currentKey.PublicKeyJwk, currentKey.PublicKeyFingerprint);
        var keys = new List<Es256Jwk> { current };
        keys.AddRange(LoadPreviousKeys(current));
        return new Es256JwksDocument(keys);
    }

    public void ValidateStartup(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = GetJwks();
    }

    private IReadOnlyList<Es256Jwk> LoadPreviousKeys(Es256Jwk current)
    {
        if (string.IsNullOrWhiteSpace(options.PreviousKeysFile))
        {
            return [];
        }

        if (!Path.IsPathFullyQualified(options.PreviousKeysFile) || !File.Exists(options.PreviousKeysFile))
        {
            throw new InvalidOperationException("JWKS_PREVIOUS_KEYS_FILE_INVALID");
        }

        using var document = JsonDocument.Parse(File.ReadAllText(options.PreviousKeysFile));
        var root = document.RootElement;
        var keyElements = root.ValueKind switch
        {
            JsonValueKind.Array => root.EnumerateArray(),
            JsonValueKind.Object when root.TryGetProperty("keys", out var keys) && keys.ValueKind == JsonValueKind.Array => keys.EnumerateArray(),
            _ => throw new InvalidOperationException("JWKS_PREVIOUS_KEYS_FILE_INVALID"),
        };

        var seenKids = new HashSet<string>(StringComparer.Ordinal) { current.Kid };
        var previous = new List<Es256Jwk>();
        foreach (var keyElement in keyElements)
        {
            var key = ParsePreviousKey(keyElement);
            if (!seenKids.Add(key.Kid))
            {
                throw new InvalidOperationException("JWKS_DUPLICATE_KID");
            }

            previous.Add(key);
        }

        return previous;
    }

    private static Es256Jwk ParsePreviousKey(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("JWKS_PREVIOUS_KEY_MALFORMED");
        }

        if (root.TryGetProperty("d", out _))
        {
            throw new InvalidOperationException("JWKS_PRIVATE_KEY_MATERIAL_FORBIDDEN");
        }

        var kid = RequiredString(root, "kid");
        var kty = RequiredString(root, "kty");
        var crv = RequiredString(root, "crv");
        var x = RequiredString(root, "x");
        var y = RequiredString(root, "y");
        var use = RequiredString(root, "use");
        var alg = RequiredString(root, "alg");
        var fingerprint = RequiredString(root, "fingerprint");
        var notBefore = OptionalDate(root, "notBefore");
        var notAfter = OptionalDate(root, "notAfter");
        var retiredAt = OptionalDate(root, "retiredAt");

        if (notBefore is null && notAfter is null && retiredAt is null)
        {
            throw new InvalidOperationException("JWKS_PREVIOUS_KEY_VALIDITY_REQUIRED");
        }

        var key = new Es256Jwk(kid, kty, crv, x, y, use, alg, fingerprint, notBefore, notAfter, retiredAt);
        ValidatePublicKey(key);
        return key;
    }

    private static Es256Jwk FromPublicJwk(string kid, string publicKeyJwk, string fingerprint)
    {
        using var document = JsonDocument.Parse(publicKeyJwk);
        var root = document.RootElement;
        var key = new Es256Jwk(
            kid,
            RequiredString(root, "kty"),
            RequiredString(root, "crv"),
            RequiredString(root, "x"),
            RequiredString(root, "y"),
            "sig",
            EvidenceSignatureDefaults.AlgorithmEs256,
            fingerprint);
        ValidatePublicKey(key);
        return key;
    }

    private static void ValidatePublicKey(Es256Jwk key)
    {
        if (string.IsNullOrWhiteSpace(key.Kid) ||
            key.Kty != "EC" ||
            key.Crv != "P-256" ||
            key.Use != "sig" ||
            key.Alg != EvidenceSignatureDefaults.AlgorithmEs256)
        {
            throw new InvalidOperationException("JWKS_PUBLIC_KEY_INVALID");
        }

        var canonicalPublicJwk = Es256JwsEvidenceSignatureBuilder.BuildPublicJwk(Base64UrlDecode(key.X), Base64UrlDecode(key.Y));
        var expectedFingerprint = Es256JwsEvidenceSignatureBuilder.ComputePublicKeyFingerprint(canonicalPublicJwk);
        if (!string.Equals(key.Fingerprint, expectedFingerprint, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("JWKS_PUBLIC_KEY_FINGERPRINT_MISMATCH");
        }

        using var ecdsa = ECDsa.Create();
        ecdsa.ImportParameters(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint
            {
                X = Base64UrlDecode(key.X),
                Y = Base64UrlDecode(key.Y),
            },
        });
    }

    private static string RequiredString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(property.GetString()))
        {
            throw new InvalidOperationException("JWKS_PUBLIC_KEY_INVALID");
        }

        return property.GetString()!;
    }

    private static DateTimeOffset? OptionalDate(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.String &&
               DateTimeOffset.TryParse(property.GetString(), out var parsed)
            ? parsed
            : throw new InvalidOperationException("JWKS_PREVIOUS_KEY_VALIDITY_INVALID");
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + ((4 - padded.Length % 4) % 4), '=');
        return Convert.FromBase64String(padded);
    }
}
