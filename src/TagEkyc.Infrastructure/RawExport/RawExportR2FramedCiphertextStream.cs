using System.Security.Cryptography;
using TagEkyc.Contracts.RawExport;

namespace TagEkyc.Infrastructure.RawExport;

internal sealed class RawExportR2FramedCiphertextStream : Stream
{
    private readonly Stream source;
    private readonly RawExportR2EncryptionContext context;
    private readonly IAttemptAeadEncryptionOperation encryption;
    private readonly IContentCommitmentService contentCommitments;
    private readonly Func<int, byte[]> randomBytes;
    private readonly RawExportR2Header header;
    private readonly byte[] headerDigest;
    private readonly byte[] envelopeMetadataDigest;
    private readonly IncrementalHash plaintextHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
    private readonly IncrementalHash dataCiphertextHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
    private readonly HashSet<byte[]> dataNonceSuffixes = new(ByteArrayComparer.Instance);
    private byte[] authenticationChunkCommitment;
    private byte[]? current;
    private int currentOffset;
    private long remainingPlaintext;
    private uint chunkOrdinal;
    private ulong dataCiphertextLength;
    private bool headerEmitted;
    private bool finalEmitted;
    private bool disposed;

    internal RawExportR2FramedCiphertextStream(
        Stream source,
        RawExportR2EncryptionContext context,
        byte[] objectBindingDigest,
        IAttemptAeadEncryptionOperation encryption,
        IContentCommitmentService contentCommitments,
        Func<int, byte[]>? randomBytes = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(objectBindingDigest);
        this.encryption = encryption ?? throw new ArgumentNullException(nameof(encryption));
        this.contentCommitments = contentCommitments ?? throw new ArgumentNullException(nameof(contentCommitments));
        this.randomBytes = randomBytes ?? RandomNumberGenerator.GetBytes;
        if (!source.CanRead)
            throw new ArgumentException("Plaintext source must be readable.", nameof(source));
        if (context.ClaimedPlaintextLength < 1)
            throw new ArgumentOutOfRangeException(nameof(context));

        this.source = source;
        this.context = context;
        remainingPlaintext = context.ClaimedPlaintextLength;
        header = new(
            context.AttemptId,
            context.AttemptKeyReservationId,
            context.SourceArtifactId,
            context.ProvisionalObjectIdentity,
            context.EncryptionAttemptFingerprint,
            objectBindingDigest,
            context.EncryptionSuiteId,
            context.EncryptionFramingVersion,
            context.ChunkSize,
            context.NonceStrategyId,
            context.FramingParametersDigest);
        var serializedHeader = RawExportR2FrameCodec.SerializeHeader(header);
        try
        {
            headerDigest = SHA256.HashData(serializedHeader);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(serializedHeader);
        }
        authenticationChunkCommitment =
            RawExportR2FrameCodec.InitialAuthenticationChunkCommitment(headerDigest);
        envelopeMetadataDigest = RawExportR2FrameCodec.ComputeEnvelopeMetadataDigest(
            context.AttemptId,
            context.AttemptKeyReservationId,
            context.KeyProviderId,
            context.KekId,
            context.KekVersion,
            context.KekFingerprint,
            context.EncryptionSuiteId,
            context.EncryptionFramingVersion,
            context.ChunkSize,
            context.NonceStrategyId,
            context.FramingParametersDigest,
            context.WrappedDekMetadataDigest);
        ExpectedCiphertextLength =
            RawExportR2FrameCodec.CiphertextLength(context.ClaimedPlaintextLength, header);
        if (ExpectedCiphertextLength > RawExportR2FrameCodec.MaximumCiphertextLength)
            throw new ArgumentOutOfRangeException(nameof(context), "RAW_EXPORT_PROVISIONAL_OBJECT_SIZE_LIMIT_EXCEEDED");
    }

    internal long ExpectedCiphertextLength { get; }

    internal bool Completed => finalEmitted && current is null;

    internal RawExportR2Header Header => header;

    public override bool CanRead => !disposed;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => ExpectedCiphertextLength;
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count) =>
        ReadAsync(buffer.AsMemory(offset, count), CancellationToken.None)
            .AsTask().GetAwaiter().GetResult();

    public override async ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (buffer.Length == 0)
            return 0;

        if (current is null || currentOffset == current.Length)
        {
            ReleaseCurrent();
            current = await BuildNextAsync(cancellationToken).ConfigureAwait(false);
            currentOffset = 0;
            if (current is null)
                return 0;
        }

        var count = Math.Min(buffer.Length, current.Length - currentOffset);
        current.AsMemory(currentOffset, count).CopyTo(buffer);
        currentOffset += count;
        return count;
    }

    private async Task<byte[]?> BuildNextAsync(CancellationToken cancellationToken)
    {
        if (!headerEmitted)
        {
            headerEmitted = true;
            return RawExportR2FrameCodec.SerializeHeader(header);
        }

        if (remainingPlaintext > 0)
            return await BuildDataFrameAsync(cancellationToken).ConfigureAwait(false);

        if (!finalEmitted)
        {
            var final = await BuildFinalFrameAsync(cancellationToken).ConfigureAwait(false);
            finalEmitted = true;
            return final;
        }

        return null;
    }

    private async Task<byte[]> BuildDataFrameAsync(CancellationToken cancellationToken)
    {
        var length = checked((int)Math.Min(context.ChunkSize, remainingPlaintext));
        var plaintext = new byte[length];
        byte[]? nonce = null;
        byte[]? aad = null;
        byte[]? frame = null;
        try
        {
            var read = 0;
            while (read < plaintext.Length)
            {
                var count = await source.ReadAsync(plaintext.AsMemory(read), cancellationToken)
                    .ConfigureAwait(false);
                if (count == 0)
                    throw new RawExportR2DeterministicInvalidException("RAW_EXPORT_R2_PLAINTEXT_LENGTH_MISMATCH");
                read += count;
            }

            plaintextHash.AppendData(plaintext);
            nonce = NewNonce(0x00, enforceUniqueDataSuffix: true);
            aad = RawExportR2FrameCodec.CreateDataAad(
                context.EncryptionAttemptFingerprint,
                headerDigest,
                chunkOrdinal,
                checked((uint)length));
            var encrypted = await encryption.EncryptBoundedChunkAsync(
                new(context.AttemptKeyReservationId, plaintext, nonce, aad),
                cancellationToken).ConfigureAwait(false);
            try
            {
                frame = RawExportR2FrameCodec.SerializeDataFrame(
                    chunkOrdinal,
                    checked((uint)length),
                    nonce,
                    encrypted.Output,
                    encrypted.AuthenticationTag);
                dataCiphertextHash.AppendData(frame);
                dataCiphertextLength = checked(dataCiphertextLength + (ulong)frame.Length);
                var next = RawExportR2FrameCodec.NextAuthenticationChunkCommitment(
                    authenticationChunkCommitment,
                    chunkOrdinal,
                    encrypted.AuthenticationTag);
                CryptographicOperations.ZeroMemory(authenticationChunkCommitment);
                authenticationChunkCommitment = next;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(encrypted.Output);
                CryptographicOperations.ZeroMemory(encrypted.AuthenticationTag);
            }

            remainingPlaintext -= length;
            chunkOrdinal++;
            return frame;
        }
        catch
        {
            if (frame is not null)
                CryptographicOperations.ZeroMemory(frame);
            throw;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
            if (nonce is not null) CryptographicOperations.ZeroMemory(nonce);
            if (aad is not null) CryptographicOperations.ZeroMemory(aad);
        }
    }

    private async Task<byte[]> BuildFinalFrameAsync(CancellationToken cancellationToken)
    {
        var extra = new byte[1];
        byte[]? plaintextDigest = null;
        byte[]? payload = null;
        byte[]? dataDigest = null;
        byte[]? completionPlaintext = null;
        byte[]? nonce = null;
        byte[]? aad = null;
        try
        {
            if (await source.ReadAsync(extra, cancellationToken).ConfigureAwait(false) != 0)
                throw new RawExportR2DeterministicInvalidException("RAW_EXPORT_R2_PLAINTEXT_LENGTH_MISMATCH");
            if (chunkOrdinal == 0)
                throw new RawExportR2DeterministicInvalidException("RAW_EXPORT_R2_ZERO_CHUNKS_FORBIDDEN");

            plaintextDigest = plaintextHash.GetHashAndReset();
            payload = RawExportR2FrameCodec.BuildHistoricCommitmentPayload(
                context.StableDataScopeId,
                context.ControllerIdentity,
                context.VerificationSessionId,
                context.CaptureArtifactId,
                context.CaptureRevision,
                context.RawClass,
                plaintextDigest,
                context.ClaimedPlaintextLength,
                context.MediaType);
            var result = await contentCommitments.ComputeAsync(
                new(context.ContentCommitmentKeyId, context.ContentCommitmentKeyVersion),
                payload,
                cancellationToken).ConfigureAwait(false);
            if (!result.IsSuccess)
                throw new IOException("RAW_EXPORT_R2_CONTENT_COMMITMENT_PROVIDER_UNAVAILABLE");
            var computedCommitment = result.Mac.ToArray();
            try
            {
                if (!CryptographicOperations.FixedTimeEquals(computedCommitment, context.ContentCommitment))
                    throw new RawExportR2DeterministicInvalidException("RAW_EXPORT_R2_CONTENT_COMMITMENT_MISMATCH");
            }
            finally
            {
                CryptographicOperations.ZeroMemory(computedCommitment);
            }

            dataDigest = dataCiphertextHash.GetHashAndReset();
            completionPlaintext = RawExportR2FrameCodec.SerializeCompletion(new(
                chunkOrdinal,
                checked((ulong)context.ClaimedPlaintextLength),
                dataCiphertextLength,
                dataDigest,
                context.ContentCommitment,
                authenticationChunkCommitment,
                envelopeMetadataDigest,
                context.WrappedDekMetadataDigest));
            nonce = NewNonce(0x01, enforceUniqueDataSuffix: false);
            aad = RawExportR2FrameCodec.CreateFinalAad(
                context.EncryptionAttemptFingerprint,
                headerDigest,
                chunkOrdinal,
                checked((ulong)context.ClaimedPlaintextLength));
            var encrypted = await encryption.EncryptBoundedChunkAsync(
                new(context.AttemptKeyReservationId, completionPlaintext, nonce, aad),
                cancellationToken).ConfigureAwait(false);
            try
            {
                return RawExportR2FrameCodec.SerializeFinalFrame(
                    nonce,
                    encrypted.Output,
                    encrypted.AuthenticationTag);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(encrypted.Output);
                CryptographicOperations.ZeroMemory(encrypted.AuthenticationTag);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(extra);
            if (plaintextDigest is not null) CryptographicOperations.ZeroMemory(plaintextDigest);
            if (payload is not null) CryptographicOperations.ZeroMemory(payload);
            if (dataDigest is not null) CryptographicOperations.ZeroMemory(dataDigest);
            if (completionPlaintext is not null) CryptographicOperations.ZeroMemory(completionPlaintext);
            if (nonce is not null) CryptographicOperations.ZeroMemory(nonce);
            if (aad is not null) CryptographicOperations.ZeroMemory(aad);
        }
    }

    private byte[] NewNonce(byte prefix, bool enforceUniqueDataSuffix)
    {
        var suffix = randomBytes(11);
        if (suffix.Length != 11)
        {
            CryptographicOperations.ZeroMemory(suffix);
            throw new RawExportR2DeterministicInvalidException("RAW_EXPORT_R2_NONCE_SOURCE_INVALID");
        }
        try
        {
            if (enforceUniqueDataSuffix)
            {
                var retainedSuffix = suffix.ToArray();
                if (!dataNonceSuffixes.Add(retainedSuffix))
                {
                    CryptographicOperations.ZeroMemory(retainedSuffix);
                    throw new RawExportR2DeterministicInvalidException("RAW_EXPORT_R2_DUPLICATE_DATA_NONCE");
                }
            }
            var nonce = new byte[12];
            nonce[0] = prefix;
            suffix.CopyTo(nonce, 1);
            return nonce;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(suffix);
        }
    }

    private void ReleaseCurrent()
    {
        if (current is not null)
            CryptographicOperations.ZeroMemory(current);
        current = null;
        currentOffset = 0;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !disposed)
        {
            disposed = true;
            ReleaseCurrent();
            plaintextHash.Dispose();
            dataCiphertextHash.Dispose();
            CryptographicOperations.ZeroMemory(headerDigest);
            CryptographicOperations.ZeroMemory(envelopeMetadataDigest);
            CryptographicOperations.ZeroMemory(authenticationChunkCommitment);
            foreach (var suffix in dataNonceSuffixes)
                CryptographicOperations.ZeroMemory(suffix);
            dataNonceSuffixes.Clear();
        }
        base.Dispose(disposing);
    }

    public override ValueTask DisposeAsync()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    private sealed class ByteArrayComparer : IEqualityComparer<byte[]>
    {
        internal static readonly ByteArrayComparer Instance = new();

        public bool Equals(byte[]? left, byte[]? right) =>
            ReferenceEquals(left, right)
            || (left is not null && right is not null
                && left.Length == right.Length
                && CryptographicOperations.FixedTimeEquals(left, right));

        public int GetHashCode(byte[] value)
        {
            var hash = new HashCode();
            hash.AddBytes(value);
            return hash.ToHashCode();
        }
    }
}
