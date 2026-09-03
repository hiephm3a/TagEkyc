using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using TagEkyc.Domain;

namespace TagEkyc.Infrastructure.Persistence;

public static class RawExportConsentScopeCodec
{
    private static readonly byte[] NamespaceBytes = Encoding.UTF8.GetBytes("tagekyc:consent-scope:v1");

    public static byte[] ComputeScopeHash(
        Guid verificationSessionId,
        string subjectRef,
        Guid policyId,
        int policyVersion,
        string purposeCode,
        Guid recipientClientApplicationId)
    {
        using var stream = new MemoryStream();
        stream.Write(NamespaceBytes);
        WriteLengthPrefixed(stream, verificationSessionId.ToByteArray(bigEndian: true));
        WriteLengthPrefixed(stream, Encoding.UTF8.GetBytes(subjectRef.Normalize(NormalizationForm.FormC)));
        WriteLengthPrefixed(stream, policyId.ToByteArray(bigEndian: true));
        Span<byte> versionBytes = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(versionBytes, policyVersion);
        WriteLengthPrefixed(stream, versionBytes);
        WriteLengthPrefixed(stream, Encoding.UTF8.GetBytes(purposeCode));
        WriteLengthPrefixed(stream, recipientClientApplicationId.ToByteArray(bigEndian: true));
        return SHA256.HashData(stream.ToArray());
    }

    public static long ToAdvisoryLockKey(byte[] scopeHash)
    {
        if (scopeHash.Length < 8)
        {
            throw new ArgumentException("Scope hash must contain at least 8 bytes.", nameof(scopeHash));
        }

        return BinaryPrimitives.ReadInt64BigEndian(scopeHash.AsSpan(0, 8));
    }

    public static byte[] CanonicalGuidBytes(Guid value) => value.ToByteArray(bigEndian: true);

    public static IReadOnlyList<RawExportRawClass> CanonicalizeClasses(IEnumerable<RawExportRawClass> classes) =>
        classes
            .Distinct()
            .OrderBy(rawClass => rawClass.ToString(), StringComparer.Ordinal)
            .ToArray();

    private static void WriteLengthPrefixed(Stream stream, ReadOnlySpan<byte> value)
    {
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, value.Length);
        stream.Write(length);
        stream.Write(value);
    }
}
