using System.Buffers;
using System.Security.Cryptography;

namespace TagEkyc.Infrastructure.RawExport;

internal sealed class RecipientPackageEncryptedSpool : Stream, IAsyncDisposable
{
    private const int SegmentSize = 64 * 1024;
    private readonly List<byte[]> segments = [];
    private readonly List<int> segmentLengths = [];
    private readonly IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
    private byte[]? digest;
    private bool sealedForRead;
    private bool disposed;
    private long length;

    internal long EncryptedLength => length;

    internal byte[] PackageCiphertextDigest => digest?.ToArray()
        ?? throw new InvalidOperationException("RAW_EXPORT_RECIPIENT_PACKAGE_SPOOL_NOT_SEALED");

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => !sealedForRead && !disposed;
    public override long Length => length;
    public override long Position { get => length; set => throw new NotSupportedException(); }

    public override void Write(byte[] buffer, int offset, int count) =>
        Write(buffer.AsSpan(offset, count));

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (sealedForRead) throw new InvalidOperationException("RAW_EXPORT_RECIPIENT_PACKAGE_SPOOL_SEALED");
        if (checked(length + buffer.Length) > RecipientPackageOptions.MaximumEncryptedPackageLength)
            throw new InvalidOperationException("RAW_EXPORT_RECIPIENT_PACKAGE_SIZE_LIMIT_EXCEEDED");
        hash.AppendData(buffer);
        while (!buffer.IsEmpty)
        {
            EnsureWritableSegment();
            var index = segments.Count - 1;
            var used = segmentLengths[index];
            var copy = Math.Min(SegmentSize - used, buffer.Length);
            buffer[..copy].CopyTo(segments[index].AsSpan(used, copy));
            segmentLengths[index] = used + copy;
            length += copy;
            buffer = buffer[copy..];
        }
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Write(buffer.Span);
        return ValueTask.CompletedTask;
    }

    internal void Seal()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (sealedForRead) return;
        digest = hash.GetHashAndReset();
        sealedForRead = true;
    }

    internal Stream OpenRead()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (!sealedForRead) throw new InvalidOperationException("RAW_EXPORT_RECIPIENT_PACKAGE_SPOOL_NOT_SEALED");
        return new SegmentReadStream(segments, segmentLengths, length);
    }

    public override void Flush() { }
    public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing) DisposeCore();
        base.Dispose(disposing);
    }

    public override ValueTask DisposeAsync()
    {
        DisposeCore();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    private void EnsureWritableSegment()
    {
        if (segments.Count > 0 && segmentLengths[^1] < SegmentSize) return;
        segments.Add(ArrayPool<byte>.Shared.Rent(SegmentSize));
        segmentLengths.Add(0);
    }

    private void DisposeCore()
    {
        if (disposed) return;
        foreach (var segment in segments)
        {
            CryptographicOperations.ZeroMemory(segment);
            ArrayPool<byte>.Shared.Return(segment);
        }
        segments.Clear();
        segmentLengths.Clear();
        if (digest is not null) CryptographicOperations.ZeroMemory(digest);
        hash.Dispose();
        disposed = true;
    }

    private sealed class SegmentReadStream(
        IReadOnlyList<byte[]> segments,
        IReadOnlyList<int> lengths,
        long totalLength) : Stream
    {
        private int segmentIndex;
        private int segmentOffset;
        private long position;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => totalLength;
        public override long Position { get => position; set => throw new NotSupportedException(); }
        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count) =>
            Read(buffer.AsSpan(offset, count));

        public override int Read(Span<byte> buffer)
        {
            if (position >= totalLength || buffer.IsEmpty) return 0;
            var written = 0;
            while (written < buffer.Length && segmentIndex < segments.Count)
            {
                var remaining = lengths[segmentIndex] - segmentOffset;
                if (remaining == 0)
                {
                    segmentIndex++;
                    segmentOffset = 0;
                    continue;
                }
                var copy = Math.Min(remaining, buffer.Length - written);
                segments[segmentIndex].AsSpan(segmentOffset, copy).CopyTo(buffer[written..]);
                segmentOffset += copy;
                written += copy;
                position += copy;
            }
            return written;
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(Read(buffer.Span));
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
