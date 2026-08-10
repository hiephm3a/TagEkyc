using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace TagEkyc.Infrastructure.RawExport;

internal sealed record RawExportR2Header(
    Guid AttemptId,
    Guid AttemptKeyReservationId,
    Guid SourceArtifactId,
    Guid ProvisionalObjectIdentity,
    byte[] EncryptionAttemptFingerprint,
    byte[] ObjectBindingDigest,
    string EncryptionSuiteId,
    int EncryptionFramingVersion,
    int ChunkSize,
    string NonceStrategyId,
    byte[] FramingParametersDigest);

internal sealed record RawExportR2Completion(
    uint ChunkCount,
    ulong TotalDataPlaintextLength,
    ulong DataCiphertextLength,
    byte[] DataCiphertextDigest,
    byte[] ContentCommitment,
    byte[] AuthenticationChunkCommitment,
    byte[] EncryptionEnvelopeMetadataDigest,
    byte[] WrappedKeyMetadataDigest);

internal static class RawExportR2FrameCodec
{
    internal const string DomainFamily = "TAG-EKYC:RAW-EXPORT:PROVISIONAL-OBJECT:C1:R2:V1";
    internal const string FixtureEncryptionSuiteId = "fixture-aead-aes256gcm-v1";
    internal const string FixtureNonceStrategyId = "fixture-nonce-random96-v1";
    internal const int FixtureFramingVersion = 1;
    internal const int FixtureChunkSize = 1_048_576;
    internal const int CompletionPlaintextLength = 181;
    internal const int FinalFrameLength = 214;
    internal const long MaximumCiphertextLength = 134_217_728;
    internal const byte DataFrameType = 0x01;
    internal const byte FinalFrameType = 0x02;

    private static readonly byte[] Magic = "TAGEKYC-C1-R2"u8.ToArray();
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    internal static byte[] SerializeHeader(RawExportR2Header header)
    {
        ValidateHeader(header);
        using var stream = new MemoryStream(HeaderLength(header));
        stream.Write(Magic);
        WriteUInt16(stream, 1);
        WriteGuid(stream, header.AttemptId);
        WriteGuid(stream, header.AttemptKeyReservationId);
        WriteGuid(stream, header.SourceArtifactId);
        WriteGuid(stream, header.ProvisionalObjectIdentity);
        WriteFixed(stream, header.EncryptionAttemptFingerprint, 32, nameof(header.EncryptionAttemptFingerprint));
        WriteFixed(stream, header.ObjectBindingDigest, 32, nameof(header.ObjectBindingDigest));
        WriteLpText(stream, header.EncryptionSuiteId, 128);
        WriteUInt16(stream, checked((ushort)header.EncryptionFramingVersion));
        WriteUInt32(stream, checked((uint)header.ChunkSize));
        WriteLpText(stream, header.NonceStrategyId, 128);
        WriteFixed(stream, header.FramingParametersDigest, 32, nameof(header.FramingParametersDigest));
        var result = stream.ToArray();
        if (result.Length != HeaderLength(header))
            throw new InvalidOperationException("RAW_EXPORT_R2_HEADER_LENGTH_INVALID");
        return result;
    }

    internal static int HeaderLength(RawExportR2Header header) =>
        checked(189
            + StrictTextBytes(header.EncryptionSuiteId, 128).Length
            + StrictTextBytes(header.NonceStrategyId, 128).Length);

    internal static long CiphertextLength(long plaintextLength, RawExportR2Header header)
    {
        if (plaintextLength < 1 || header.ChunkSize < 1)
            throw new ArgumentOutOfRangeException(nameof(plaintextLength));
        var chunks = checked((plaintextLength + header.ChunkSize - 1) / header.ChunkSize);
        return checked(HeaderLength(header) + plaintextLength + 37 * chunks + FinalFrameLength);
    }

    internal static byte[] CreateDataAad(
        ReadOnlySpan<byte> encryptionAttemptFingerprint,
        ReadOnlySpan<byte> objectHeaderDigest,
        uint ordinal,
        uint plaintextLength)
    {
        using var stream = new MemoryStream();
        WriteLpText(stream, "tip-88c1-r2-data-aad-v1", 128);
        WriteFixed(stream, encryptionAttemptFingerprint, 32, nameof(encryptionAttemptFingerprint));
        WriteFixed(stream, objectHeaderDigest, 32, nameof(objectHeaderDigest));
        stream.WriteByte(DataFrameType);
        WriteUInt32(stream, ordinal);
        WriteUInt32(stream, plaintextLength);
        return stream.ToArray();
    }

    internal static byte[] SerializeDataFrame(
        uint ordinal,
        uint plaintextLength,
        ReadOnlySpan<byte> nonce,
        ReadOnlySpan<byte> ciphertext,
        ReadOnlySpan<byte> authenticationTag)
    {
        if (plaintextLength < 1 || ciphertext.Length != plaintextLength)
            throw new ArgumentException("RAW_EXPORT_R2_DATA_LENGTH_INVALID", nameof(ciphertext));
        using var stream = new MemoryStream(checked(37 + ciphertext.Length));
        stream.WriteByte(DataFrameType);
        WriteUInt32(stream, ordinal);
        WriteUInt32(stream, plaintextLength);
        WriteFixed(stream, nonce, 12, nameof(nonce));
        stream.Write(ciphertext);
        WriteFixed(stream, authenticationTag, 16, nameof(authenticationTag));
        return stream.ToArray();
    }

    internal static byte[] CreateFinalAad(
        ReadOnlySpan<byte> encryptionAttemptFingerprint,
        ReadOnlySpan<byte> objectHeaderDigest,
        uint chunkCount,
        ulong totalDataPlaintextLength)
    {
        using var stream = new MemoryStream();
        WriteLpText(stream, "tip-88c1-r2-final-aad-v1", 128);
        WriteFixed(stream, encryptionAttemptFingerprint, 32, nameof(encryptionAttemptFingerprint));
        WriteFixed(stream, objectHeaderDigest, 32, nameof(objectHeaderDigest));
        stream.WriteByte(FinalFrameType);
        WriteUInt32(stream, chunkCount);
        WriteUInt64(stream, totalDataPlaintextLength);
        return stream.ToArray();
    }

    internal static byte[] SerializeCompletion(RawExportR2Completion completion)
    {
        using var stream = new MemoryStream(CompletionPlaintextLength);
        WriteUInt32(stream, completion.ChunkCount);
        WriteUInt64(stream, completion.TotalDataPlaintextLength);
        WriteUInt64(stream, completion.DataCiphertextLength);
        WriteFixed(stream, completion.DataCiphertextDigest, 32, nameof(completion.DataCiphertextDigest));
        WriteFixed(stream, completion.ContentCommitment, 32, nameof(completion.ContentCommitment));
        WriteFixed(stream, completion.AuthenticationChunkCommitment, 32, nameof(completion.AuthenticationChunkCommitment));
        WriteFixed(stream, completion.EncryptionEnvelopeMetadataDigest, 32, nameof(completion.EncryptionEnvelopeMetadataDigest));
        WriteFixed(stream, completion.WrappedKeyMetadataDigest, 32, nameof(completion.WrappedKeyMetadataDigest));
        stream.WriteByte(0x01);
        var result = stream.ToArray();
        if (result.Length != CompletionPlaintextLength)
            throw new InvalidOperationException("RAW_EXPORT_R2_COMPLETION_LENGTH_INVALID");
        return result;
    }

    internal static RawExportR2Completion ParseCompletion(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != CompletionPlaintextLength || bytes[^1] != 0x01)
            throw new RawExportR2DeterministicInvalidException("RAW_EXPORT_R2_COMPLETION_INVALID");
        var offset = 0;
        var chunks = ReadUInt32(bytes, ref offset);
        var plaintext = ReadUInt64(bytes, ref offset);
        var dataLength = ReadUInt64(bytes, ref offset);
        var dataDigest = Take(bytes, ref offset, 32);
        var commitment = Take(bytes, ref offset, 32);
        var chunkCommitment = Take(bytes, ref offset, 32);
        var envelopeDigest = Take(bytes, ref offset, 32);
        var wrappedDigest = Take(bytes, ref offset, 32);
        offset++;
        if (offset != bytes.Length)
            throw new RawExportR2DeterministicInvalidException("RAW_EXPORT_R2_COMPLETION_TRAILING_DATA");
        return new(chunks, plaintext, dataLength, dataDigest, commitment,
            chunkCommitment, envelopeDigest, wrappedDigest);
    }

    internal static byte[] SerializeFinalFrame(
        ReadOnlySpan<byte> nonce,
        ReadOnlySpan<byte> completionCiphertext,
        ReadOnlySpan<byte> authenticationTag)
    {
        if (completionCiphertext.Length != CompletionPlaintextLength)
            throw new ArgumentException("RAW_EXPORT_R2_COMPLETION_LENGTH_INVALID", nameof(completionCiphertext));
        using var stream = new MemoryStream(FinalFrameLength);
        stream.WriteByte(FinalFrameType);
        WriteFixed(stream, nonce, 12, nameof(nonce));
        WriteUInt32(stream, CompletionPlaintextLength);
        stream.Write(completionCiphertext);
        WriteFixed(stream, authenticationTag, 16, nameof(authenticationTag));
        return stream.ToArray();
    }

    internal static byte[] InitialAuthenticationChunkCommitment(ReadOnlySpan<byte> headerDigest)
    {
        using var stream = new MemoryStream();
        WriteLpText(stream, "tip-88c1-r2-chunk-chain-v1", 128);
        WriteFixed(stream, headerDigest, 32, nameof(headerDigest));
        return SHA256.HashData(stream.ToArray());
    }

    internal static byte[] NextAuthenticationChunkCommitment(
        ReadOnlySpan<byte> previous,
        uint ordinal,
        ReadOnlySpan<byte> authenticationTag)
    {
        using var stream = new MemoryStream(52);
        WriteFixed(stream, previous, 32, nameof(previous));
        WriteUInt32(stream, ordinal);
        WriteFixed(stream, authenticationTag, 16, nameof(authenticationTag));
        return SHA256.HashData(stream.ToArray());
    }

    internal static byte[] ComputeEnvelopeMetadataDigest(
        Guid attemptId,
        Guid attemptKeyReservationId,
        string keyProviderId,
        string kekId,
        int kekVersion,
        string kekFingerprint,
        string encryptionSuiteId,
        int encryptionFramingVersion,
        int chunkSize,
        string nonceStrategyId,
        ReadOnlySpan<byte> framingParametersDigest,
        ReadOnlySpan<byte> wrappedDekMetadataDigest)
    {
        using var stream = new MemoryStream();
        WriteLpText(stream, "tip-88c1-r2-envelope-metadata-v1", 128);
        WriteGuid(stream, attemptId);
        WriteGuid(stream, attemptKeyReservationId);
        WriteLpText(stream, keyProviderId, 256);
        WriteLpText(stream, kekId, 256);
        WriteUInt32(stream, checked((uint)kekVersion));
        WriteLpText(stream, kekFingerprint, 256);
        WriteLpText(stream, encryptionSuiteId, 128);
        WriteUInt16(stream, checked((ushort)encryptionFramingVersion));
        WriteUInt32(stream, checked((uint)chunkSize));
        WriteLpText(stream, nonceStrategyId, 128);
        WriteFixed(stream, framingParametersDigest, 32, nameof(framingParametersDigest));
        WriteFixed(stream, wrappedDekMetadataDigest, 32, nameof(wrappedDekMetadataDigest));
        return SHA256.HashData(stream.ToArray());
    }

    internal static byte[] ComputeVerificationEvidence(
        Guid attemptId,
        Guid objectCustodyId,
        Guid provisionalObjectIdentity,
        ReadOnlySpan<byte> objectBindingDigest,
        ReadOnlySpan<byte> encryptionAttemptFingerprint,
        ulong ciphertextLength,
        ReadOnlySpan<byte> ciphertextDigest,
        ReadOnlySpan<byte> objectHeaderDigest,
        ReadOnlySpan<byte> dataCiphertextDigest,
        ReadOnlySpan<byte> contentCommitment,
        ReadOnlySpan<byte> authenticationChunkCommitment,
        ReadOnlySpan<byte> encryptionEnvelopeMetadataDigest,
        ReadOnlySpan<byte> wrappedKeyMetadataDigest)
    {
        using var stream = new MemoryStream();
        WriteLpText(stream, "tip-88c1-r2-verification-evidence-v1", 128);
        WriteGuid(stream, attemptId);
        WriteGuid(stream, objectCustodyId);
        WriteGuid(stream, provisionalObjectIdentity);
        WriteFixed(stream, objectBindingDigest, 32, nameof(objectBindingDigest));
        WriteFixed(stream, encryptionAttemptFingerprint, 32, nameof(encryptionAttemptFingerprint));
        WriteUInt64(stream, ciphertextLength);
        WriteFixed(stream, ciphertextDigest, 32, nameof(ciphertextDigest));
        WriteFixed(stream, objectHeaderDigest, 32, nameof(objectHeaderDigest));
        WriteFixed(stream, dataCiphertextDigest, 32, nameof(dataCiphertextDigest));
        WriteFixed(stream, contentCommitment, 32, nameof(contentCommitment));
        WriteFixed(stream, authenticationChunkCommitment, 32, nameof(authenticationChunkCommitment));
        WriteFixed(stream, encryptionEnvelopeMetadataDigest, 32, nameof(encryptionEnvelopeMetadataDigest));
        WriteFixed(stream, wrappedKeyMetadataDigest, 32, nameof(wrappedKeyMetadataDigest));
        return SHA256.HashData(stream.ToArray());
    }

    internal static byte[] BuildHistoricCommitmentPayload(
        string stableDataScopeId,
        string controllerIdentity,
        Guid verificationSessionId,
        Guid captureArtifactId,
        int captureRevision,
        string rawClass,
        ReadOnlySpan<byte> plaintextDigest,
        long claimedPlaintextLength,
        string mediaType)
    {
        if (plaintextDigest.Length != 32)
            throw new ArgumentException("Plaintext digest must contain 32 bytes.", nameof(plaintextDigest));
        using var stream = new MemoryStream();
        WriteLpText(stream, "TAG-EKYC:RAW-EXPORT:CONTENT-COMMITMENT:C1:V1", 256);
        WriteLpText(stream, stableDataScopeId, 256);
        WriteLpText(stream, controllerIdentity, 256);
        WriteLpText(stream, verificationSessionId.ToString("N"), 32);
        WriteLpText(stream, captureArtifactId.ToString("N"), 32);
        WriteLpText(stream, captureRevision.ToString(CultureInfo.InvariantCulture), 32);
        WriteLpText(stream, rawClass, 128);
        Span<byte> digestText = stackalloc byte[64];
        const string alphabet = "0123456789abcdef";
        for (var i = 0; i < plaintextDigest.Length; i++)
        {
            digestText[i * 2] = (byte)alphabet[plaintextDigest[i] >> 4];
            digestText[i * 2 + 1] = (byte)alphabet[plaintextDigest[i] & 0x0f];
        }
        WriteUInt32(stream, 64);
        stream.Write(digestText);
        CryptographicOperations.ZeroMemory(digestText);
        WriteLpText(stream, claimedPlaintextLength.ToString(CultureInfo.InvariantCulture), 32);
        WriteLpText(stream, mediaType, 256);
        return stream.ToArray();
    }

    internal static void ValidateHeaderBytes(ReadOnlySpan<byte> observed, RawExportR2Header expected)
    {
        var canonical = SerializeHeader(expected);
        try
        {
            if (!CryptographicOperations.FixedTimeEquals(observed, canonical))
                throw new RawExportR2DeterministicInvalidException("RAW_EXPORT_R2_HEADER_INVALID");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(canonical);
        }
    }

    private static void ValidateHeader(RawExportR2Header header)
    {
        if (header.AttemptId == Guid.Empty || header.AttemptKeyReservationId == Guid.Empty
            || header.SourceArtifactId == Guid.Empty || header.ProvisionalObjectIdentity == Guid.Empty
            || header.EncryptionFramingVersion < 1 || header.ChunkSize < 1)
            throw new ArgumentException("RAW_EXPORT_R2_HEADER_INVALID", nameof(header));
        WriteOnlyValidate(header.EncryptionAttemptFingerprint, 32, nameof(header.EncryptionAttemptFingerprint));
        WriteOnlyValidate(header.ObjectBindingDigest, 32, nameof(header.ObjectBindingDigest));
        WriteOnlyValidate(header.FramingParametersDigest, 32, nameof(header.FramingParametersDigest));
        _ = StrictTextBytes(header.EncryptionSuiteId, 128);
        _ = StrictTextBytes(header.NonceStrategyId, 128);
    }

    private static byte[] StrictTextBytes(string value, int maximum)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!value.IsNormalized(NormalizationForm.FormC))
            throw new ArgumentException("Text must be NFC normalized.", nameof(value));
        var bytes = StrictUtf8.GetBytes(value);
        if (bytes.Length > maximum)
            throw new ArgumentOutOfRangeException(nameof(value));
        return bytes;
    }

    private static void WriteLpText(Stream stream, string value, int maximum)
    {
        var bytes = StrictTextBytes(value, maximum);
        WriteUInt32(stream, checked((uint)bytes.Length));
        stream.Write(bytes);
    }

    private static void WriteGuid(Stream stream, Guid value) =>
        stream.Write(value.ToByteArray(bigEndian: true));

    private static void WriteUInt16(Stream stream, ushort value)
    {
        Span<byte> bytes = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(bytes, value);
        stream.Write(bytes);
    }

    private static void WriteUInt32(Stream stream, uint value)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
        stream.Write(bytes);
    }

    private static void WriteUInt64(Stream stream, ulong value)
    {
        Span<byte> bytes = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(bytes, value);
        stream.Write(bytes);
    }

    private static uint ReadUInt32(ReadOnlySpan<byte> bytes, ref int offset)
    {
        if (offset > bytes.Length - 4)
            throw new RawExportR2DeterministicInvalidException("RAW_EXPORT_R2_FRAME_TRUNCATED");
        var value = BinaryPrimitives.ReadUInt32BigEndian(bytes[offset..]);
        offset += 4;
        return value;
    }

    private static ulong ReadUInt64(ReadOnlySpan<byte> bytes, ref int offset)
    {
        if (offset > bytes.Length - 8)
            throw new RawExportR2DeterministicInvalidException("RAW_EXPORT_R2_FRAME_TRUNCATED");
        var value = BinaryPrimitives.ReadUInt64BigEndian(bytes[offset..]);
        offset += 8;
        return value;
    }

    private static byte[] Take(ReadOnlySpan<byte> bytes, ref int offset, int count)
    {
        if (offset > bytes.Length - count)
            throw new RawExportR2DeterministicInvalidException("RAW_EXPORT_R2_FRAME_TRUNCATED");
        var value = bytes.Slice(offset, count).ToArray();
        offset += count;
        return value;
    }

    private static void WriteFixed(Stream stream, ReadOnlySpan<byte> value, int length, string parameter)
    {
        WriteOnlyValidate(value, length, parameter);
        stream.Write(value);
    }

    private static void WriteOnlyValidate(ReadOnlySpan<byte> value, int length, string parameter)
    {
        if (value.Length != length)
            throw new ArgumentException($"{parameter} must contain {length} bytes.", parameter);
    }
}
