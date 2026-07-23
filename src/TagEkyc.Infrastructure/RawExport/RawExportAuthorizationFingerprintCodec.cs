using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using TagEkyc.Domain;

namespace TagEkyc.Infrastructure.RawExport;

public static class RawExportAuthorizationFingerprintCodec
{
    private static readonly byte[] NamespaceBytes = Encoding.UTF8.GetBytes("tagekyc:raw-export-authz:v1");

    public static byte[] ComputeHash(
        Guid principalId,
        Guid clientApplicationId,
        Guid requestedVerificationSessionId,
        Guid policyId,
        int policyVersion,
        string purposeCode,
        RawExportRawClassSelectionMode selectionMode,
        IEnumerable<RawExportRawClass>? requestedRawClasses)
    {
        ArgumentNullException.ThrowIfNull(purposeCode);
        if (policyVersion < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(policyVersion));
        }

        var canonicalClasses = CanonicalizeClasses(selectionMode, requestedRawClasses);
        using var stream = new MemoryStream();
        stream.Write(NamespaceBytes);
        WriteLengthPrefixed(stream, principalId.ToByteArray(bigEndian: true));
        WriteLengthPrefixed(stream, clientApplicationId.ToByteArray(bigEndian: true));
        WriteLengthPrefixed(stream, requestedVerificationSessionId.ToByteArray(bigEndian: true));
        WriteLengthPrefixed(stream, policyId.ToByteArray(bigEndian: true));

        Span<byte> policyVersionBytes = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(policyVersionBytes, policyVersion);
        WriteLengthPrefixed(stream, policyVersionBytes);
        WriteLengthPrefixed(stream, Encoding.UTF8.GetBytes(purposeCode));
        WriteLengthPrefixed(stream, Encoding.UTF8.GetBytes(selectionMode.ToString()));

        Span<byte> ordinalBytes = stackalloc byte[4];
        foreach (var rawClass in canonicalClasses)
        {
            BinaryPrimitives.WriteInt32BigEndian(ordinalBytes, (int)rawClass);
            WriteLengthPrefixed(stream, ordinalBytes);
        }

        return SHA256.HashData(stream.ToArray());
    }

    private static IReadOnlyList<RawExportRawClass> CanonicalizeClasses(
        RawExportRawClassSelectionMode selectionMode,
        IEnumerable<RawExportRawClass>? requestedRawClasses)
    {
        if (!Enum.IsDefined(selectionMode))
        {
            throw new ArgumentOutOfRangeException(nameof(selectionMode));
        }

        if (selectionMode == RawExportRawClassSelectionMode.DefaultPolicySet)
        {
            if (requestedRawClasses is not null)
            {
                throw new ArgumentException("DefaultPolicySet must not carry requested raw classes.", nameof(requestedRawClasses));
            }

            return [];
        }

        if (requestedRawClasses is null)
        {
            throw new ArgumentNullException(nameof(requestedRawClasses));
        }

        var supplied = requestedRawClasses.ToArray();
        if (supplied.Length == 0)
        {
            throw new ArgumentException("ExplicitSubset requires at least one requested raw class.", nameof(requestedRawClasses));
        }

        if (supplied.Any(rawClass => !Enum.IsDefined(rawClass)))
        {
            throw new ArgumentException("Requested raw classes contain an unknown value.", nameof(requestedRawClasses));
        }

        if (supplied.Distinct().Count() != supplied.Length)
        {
            throw new ArgumentException("Requested raw classes contain a duplicate value.", nameof(requestedRawClasses));
        }

        return supplied.OrderBy(rawClass => (int)rawClass).ToArray();
    }

    private static void WriteLengthPrefixed(Stream stream, ReadOnlySpan<byte> value)
    {
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, value.Length);
        stream.Write(length);
        stream.Write(value);
    }
}
