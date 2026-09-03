using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using TagEkyc.Domain;

namespace TagEkyc.Infrastructure.RawExport;

public static class RawExportJobFingerprintCodec
{
    private static readonly byte[] Namespace = Encoding.UTF8.GetBytes("tagekyc:raw-export-job-bind:v1");

    public static byte[] Compute(
        Guid principalId,
        Guid clientApplicationId,
        Guid permitId,
        Guid authorizationDecisionId,
        Guid verificationSessionId,
        string subjectRef,
        Guid policyId,
        int policyVersion,
        string purposeCode,
        Guid recipientClientApplicationId,
        RawExportMode exportMode,
        DateTimeOffset permitExpiresAt,
        DateTimeOffset jobExpiresAt,
        string idempotencyKey,
        IReadOnlyList<RawExportRawClass> classes)
    {
        ArgumentNullException.ThrowIfNull(subjectRef);
        ArgumentNullException.ThrowIfNull(purposeCode);
        ArgumentNullException.ThrowIfNull(idempotencyKey);
        ArgumentNullException.ThrowIfNull(classes);

        using var stream = new MemoryStream();
        stream.Write(Namespace);
        WriteGuid(stream, principalId);
        WriteGuid(stream, clientApplicationId);
        WriteGuid(stream, permitId);
        WriteGuid(stream, authorizationDecisionId);
        WriteGuid(stream, verificationSessionId);
        WriteText(stream, subjectRef);
        WriteGuid(stream, policyId);
        WriteInt32(stream, policyVersion);
        WriteText(stream, purposeCode);
        WriteGuid(stream, recipientClientApplicationId);
        WriteText(stream, exportMode.ToString());
        WriteInt64(stream, permitExpiresAt.UtcTicks);
        WriteInt64(stream, jobExpiresAt.UtcTicks);
        WriteText(stream, idempotencyKey);
        WriteInt32(stream, classes.Count);
        for (var ordinal = 0; ordinal < classes.Count; ordinal++)
        {
            WriteInt32(stream, ordinal);
            WriteText(stream, classes[ordinal].ToString());
        }

        return SHA256.HashData(stream.ToArray());
    }

    private static void WriteGuid(Stream stream, Guid value) =>
        stream.Write(value.ToByteArray(bigEndian: true));

    private static void WriteText(Stream stream, string value) =>
        WriteLengthPrefixed(stream, Encoding.UTF8.GetBytes(value.Normalize(NormalizationForm.FormC)));

    private static void WriteInt32(Stream stream, int value)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(bytes, value);
        stream.Write(bytes);
    }

    private static void WriteInt64(Stream stream, long value)
    {
        Span<byte> bytes = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(bytes, value);
        stream.Write(bytes);
    }

    private static void WriteLengthPrefixed(Stream stream, ReadOnlySpan<byte> value)
    {
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(length, checked((uint)value.Length));
        stream.Write(length);
        stream.Write(value);
    }
}
