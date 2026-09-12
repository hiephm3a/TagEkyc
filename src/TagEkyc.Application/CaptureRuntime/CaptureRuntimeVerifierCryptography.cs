using System.Security.Cryptography;
using System.Text;
using System.Globalization;
using TagEkyc.Application.Ports;

namespace TagEkyc.Application.CaptureRuntime;

public static class CaptureRuntimeVerifierCryptography
{
    public const int SecretByteLength = 32;
    public const int CanonicalTextLength = 43;

    private static readonly byte[] ZeroSalt = new byte[SecretByteLength];
    private const string PlatformProvisionFingerprintDomain =
        "TAG-EKYC-A1-R01-PLATFORM-PROVISION-FINGERPRINT-v1";
    private const string PlatformRevokeFingerprintDomain =
        "TAG-EKYC-A1-R02-PLATFORM-REVOKE-FINGERPRINT-v1";
    private const string DeploymentRevocation = "DeploymentRevocation";

    public static byte[] ComputePlatformProvisionRequestFingerprint(
        Guid operationId,
        Guid principalId,
        DateTimeOffset expiresAtUtc)
    {
        if (operationId == Guid.Empty)
        {
            throw new ArgumentException("Operation identity must be non-empty.", nameof(operationId));
        }

        if (principalId == Guid.Empty)
        {
            throw new ArgumentException("Principal identity must be non-empty.", nameof(principalId));
        }

        var preimage = string.Concat(
            PlatformProvisionFingerprintDomain, "\n",
            operationId.ToString("N", CultureInfo.InvariantCulture), "\n",
            principalId.ToString("N", CultureInfo.InvariantCulture), "\n",
            expiresAtUtc.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture), "\n");
        return SHA256.HashData(Encoding.ASCII.GetBytes(preimage));
    }

    public static byte[] ComputePlatformRevokeRequestFingerprint(
        Guid operationId,
        Guid credentialId,
        long expectedRevision)
    {
        if (operationId == Guid.Empty)
        {
            throw new ArgumentException("Operation identity must be non-empty.", nameof(operationId));
        }

        if (credentialId == Guid.Empty)
        {
            throw new ArgumentException("Credential identity must be non-empty.", nameof(credentialId));
        }

        if (expectedRevision <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(expectedRevision));
        }

        var preimage = string.Concat(
            PlatformRevokeFingerprintDomain, "\n",
            operationId.ToString("N", CultureInfo.InvariantCulture), "\n",
            credentialId.ToString("N", CultureInfo.InvariantCulture), "\n",
            expectedRevision.ToString(CultureInfo.InvariantCulture), "\n",
            DeploymentRevocation, "\n");
        return SHA256.HashData(Encoding.ASCII.GetBytes(preimage));
    }

    public static bool TryDecodeCanonicalSecret(string? value, Span<byte> destination)
    {
        if (value is null || value.Length != CanonicalTextLength ||
            destination.Length < SecretByteLength)
        {
            return false;
        }

        Span<byte> canonicalAscii = stackalloc byte[CanonicalTextLength];
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (!IsBase64UrlCharacter(character))
            {
                return false;
            }

            canonicalAscii[index] = (byte)character;
        }

        return TryDecodeCanonicalSecret(canonicalAscii, destination);
    }

    public static bool TryDecodeCanonicalSecret(
        ReadOnlySpan<char> value,
        Span<byte> destination)
    {
        if (value.Length != CanonicalTextLength || destination.Length < SecretByteLength)
        {
            return false;
        }

        Span<byte> canonicalAscii = stackalloc byte[CanonicalTextLength];
        for (var index = 0; index < value.Length; index++)
        {
            if (!IsBase64UrlCharacter(value[index]))
            {
                return false;
            }
            canonicalAscii[index] = (byte)value[index];
        }
        return TryDecodeCanonicalSecret(canonicalAscii, destination);
    }

    public static bool TryDecodeCanonicalSecret(
        ReadOnlySpan<byte> canonicalAscii,
        Span<byte> destination)
    {
        if (canonicalAscii.Length != CanonicalTextLength || destination.Length < SecretByteLength)
        {
            return false;
        }

        Span<char> padded = stackalloc char[44];
        for (var index = 0; index < CanonicalTextLength; index++)
        {
            var value = canonicalAscii[index];
            if (!IsBase64UrlByte(value))
            {
                return false;
            }

            padded[index] = value switch
            {
                (byte)'-' => '+',
                (byte)'_' => '/',
                _ => (char)value
            };
        }

        if ("AEIMQUYcgkosw048"u8.IndexOf(canonicalAscii[^1]) < 0)
        {
            return false;
        }

        padded[43] = '=';
        if (!Convert.TryFromBase64Chars(padded, destination, out var written) ||
            written != SecretByteLength)
        {
            CryptographicOperations.ZeroMemory(destination[..Math.Min(written, SecretByteLength)]);
            return false;
        }

        Span<char> encoded = stackalloc char[44];
        if (!Convert.TryToBase64Chars(destination[..SecretByteLength], encoded, out var encodedLength) ||
            encodedLength != 44 || encoded[43] != '=')
        {
            CryptographicOperations.ZeroMemory(destination[..SecretByteLength]);
            return false;
        }

        var matches = true;
        for (var index = 0; index < CanonicalTextLength; index++)
        {
            var expected = encoded[index] switch
            {
                '+' => (byte)'-',
                '/' => (byte)'_',
                var character => (byte)character
            };
            matches &= expected == canonicalAscii[index];
        }

        if (!matches)
        {
            CryptographicOperations.ZeroMemory(destination[..SecretByteLength]);
        }

        return matches;
    }

    public static string EncodeCanonicalSecret(ReadOnlySpan<byte> secret)
    {
        Span<char> canonical = stackalloc char[CanonicalTextLength];
        EncodeCanonicalSecret(secret, canonical);
        return new string(canonical);
    }

    public static void EncodeCanonicalSecret(ReadOnlySpan<byte> secret, Span<char> destination)
    {
        if (secret.Length != SecretByteLength)
        {
            throw new ArgumentException("Capture Runtime verifier secrets must be exactly 32 bytes.", nameof(secret));
        }
        if (destination.Length < CanonicalTextLength)
        {
            throw new ArgumentException("Canonical destination must hold exactly 43 characters.", nameof(destination));
        }

        Span<char> padded = stackalloc char[44];
        if (!Convert.TryToBase64Chars(secret, padded, out var written) || written != 44 || padded[43] != '=')
        {
            throw new CryptographicException("Could not encode Capture Runtime verifier secret.");
        }
        for (var index = 0; index < CanonicalTextLength; index++)
        {
            destination[index] = padded[index] switch
            {
                '+' => '-',
                '/' => '_',
                var character => character
            };
        }
        padded.Clear();
    }

    public static byte[] DeriveDomainKey(
        ReadOnlySpan<byte> master,
        CaptureRuntimeVerifierPepperDomain domain)
    {
        if (master.Length != SecretByteLength)
        {
            throw new ArgumentException("Capture Runtime verifier master material must be exactly 32 bytes.", nameof(master));
        }

        var info = domain switch
        {
            CaptureRuntimeVerifierPepperDomain.PlatformCredential =>
                "TAG-EKYC-C6BA1-VERIFIER-PEPPER/platform-credential-v1",
            CaptureRuntimeVerifierPepperDomain.BootstrapDigest =>
                "TAG-EKYC-C6BA1-VERIFIER-PEPPER/bootstrap-digest-v1",
            CaptureRuntimeVerifierPepperDomain.CapabilityDigest =>
                "TAG-EKYC-C6BA1-VERIFIER-PEPPER/capability-digest-v1",
            _ => throw new ArgumentOutOfRangeException(nameof(domain))
        };

        Span<byte> prk = stackalloc byte[SecretByteLength];
        HMACSHA256.HashData(ZeroSalt, master, prk);
        var infoBytes = Encoding.ASCII.GetBytes(info);
        var expandInput = new byte[infoBytes.Length + 1];
        try
        {
            infoBytes.CopyTo(expandInput, 0);
            expandInput[^1] = 0x01;
            return HMACSHA256.HashData(prk, expandInput);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(prk);
            CryptographicOperations.ZeroMemory(infoBytes);
            CryptographicOperations.ZeroMemory(expandInput);
        }
    }

    public static byte[] ComputeDigest(
        ReadOnlySpan<byte> domainKey,
        CaptureRuntimeVerifierPepperDomain domain,
        ReadOnlySpan<byte> presentedSecret)
    {
        if (domainKey.Length != SecretByteLength)
        {
            throw new ArgumentException("Capture Runtime verifier domain keys must be exactly 32 bytes.", nameof(domainKey));
        }

        if (presentedSecret.Length != SecretByteLength)
        {
            throw new ArgumentException("Capture Runtime presented secrets must be exactly 32 bytes.", nameof(presentedSecret));
        }

        var label = domain switch
        {
            CaptureRuntimeVerifierPepperDomain.PlatformCredential => "platform-credential-v1",
            CaptureRuntimeVerifierPepperDomain.BootstrapDigest => "bootstrap-digest-v1",
            CaptureRuntimeVerifierPepperDomain.CapabilityDigest => "capability-digest-v1",
            _ => throw new ArgumentOutOfRangeException(nameof(domain))
        };

        var labelBytes = Encoding.ASCII.GetBytes(label);
        var message = new byte[labelBytes.Length + 1 + SecretByteLength];
        try
        {
            labelBytes.CopyTo(message, 0);
            presentedSecret.CopyTo(message.AsSpan(labelBytes.Length + 1));
            return HMACSHA256.HashData(domainKey, message);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(labelBytes);
            CryptographicOperations.ZeroMemory(message);
        }
    }

    public static bool FixedTimeEquals(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right) =>
        left.Length == SecretByteLength && right.Length == SecretByteLength &&
        CryptographicOperations.FixedTimeEquals(left, right);

    public static void Zero(Span<byte> buffer) => CryptographicOperations.ZeroMemory(buffer);

    private static bool IsBase64UrlCharacter(char character) =>
        character is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '-' or '_';

    private static bool IsBase64UrlByte(byte value) =>
        value is >= (byte)'A' and <= (byte)'Z'
            or >= (byte)'a' and <= (byte)'z'
            or >= (byte)'0' and <= (byte)'9'
            or (byte)'-' or (byte)'_';
}

// Pure byte composition shared by statically selected operation methods. This
// helper cannot select or invoke any gateway, SQL function, actor or operation.
internal static class CaptureRuntimeHttpFingerprint
{
    internal static byte[] Compute(string physicalId, string canonicalRoute,
        ReadOnlyMemory<byte> exactRequestBody, Guid idempotencyKey,
        ReadOnlySpan<byte> actorPartition, ReadOnlySpan<byte> decodedSecret = default)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(Encoding.UTF8.GetBytes($"TAG-EKYC-A1-{physicalId}-v1\n"));
        hash.AppendData(Encoding.UTF8.GetBytes(canonicalRoute));
        hash.AppendData([0]);
        hash.AppendData(SHA256.HashData(exactRequestBody.Span));
        hash.AppendData([0]);
        if (!decodedSecret.IsEmpty)
        {
            hash.AppendData([1]);
            hash.AppendData(SHA256.HashData(decodedSecret));
            hash.AppendData([0]);
        }
        hash.AppendData(idempotencyKey.ToByteArray(bigEndian: true));
        hash.AppendData([0]);
        hash.AppendData(actorPartition);
        return hash.GetHashAndReset();
    }
}
