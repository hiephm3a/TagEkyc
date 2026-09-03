using System.Buffers;
using System.Buffers.Binary;
using System.Security.Cryptography;
using TagEkyc.Contracts.RawExport;

namespace TagEkyc.Infrastructure.RawExport;

internal sealed class RecipientPackageCryptoService
{
    internal async Task<RecipientPackageEncryptionResult> EncryptAsync(
        C2AssemblyPreparationRequest request,
        RecipientPackageReserveResult reservation,
        Func<Stream, CancellationToken, Task> boundedAssemblyWriter,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(reservation);
        ArgumentNullException.ThrowIfNull(boundedAssemblyWriter);
        if (request.CompleteAssemblyLength is < 1 or > RecipientPackageOptions.MaximumCompleteAssemblyLength)
            throw new ArgumentOutOfRangeException(nameof(request), "RAW_EXPORT_RECIPIENT_PACKAGE_PLAINTEXT_LIMIT_EXCEEDED");
        if (reservation.PackageId is null
            || reservation.PackageEqualityFingerprint is not { Length: 32 }
            || reservation.ProviderOperationTokenDigest is not { Length: 32 }
            || reservation.RecipientKeyFingerprint is not { Length: 32 }
            || reservation.RecipientPublicKeySpki is not { Length: >= 384 and <= 1024 }
            || reservation.RecipientKeyId is null
            || reservation.RecipientKeyVersion is null)
            throw new InvalidOperationException("RAW_EXPORT_RECIPIENT_PACKAGE_RESERVATION_INVALID");

        var cek = new byte[32];
        var noncePrefix = new byte[8];
        var spool = new RecipientPackageEncryptedSpool();
        byte[]? wrappedCek = null;
        byte[]? envelope = null;
        byte[]? envelopeDigest = null;
        try
        {
            RandomNumberGenerator.Fill(cek);
            RandomNumberGenerator.Fill(noncePrefix);
            using var rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(reservation.RecipientPublicKeySpki, out var consumed);
            if (consumed != reservation.RecipientPublicKeySpki.Length || rsa.KeySize is < 3072 or > 4096)
                throw new CryptographicException("RAW_EXPORT_RECIPIENT_KEY_INVALID");
            var exponent = rsa.ExportParameters(false).Exponent;
            if (exponent is null || exponent.Length == 0 || (exponent[^1] & 1) == 0)
                throw new CryptographicException("RAW_EXPORT_RECIPIENT_KEY_INVALID");
            wrappedCek = rsa.Encrypt(cek, RSAEncryptionPadding.OaepSHA256);
            var header = new RecipientPackageHeader(
                request.AssemblyId,
                request.AssemblyDigest,
                request.AssemblyFingerprint,
                request.C2PreparationId,
                request.CompleteAssemblyLength,
                noncePrefix,
                reservation.PackageEqualityFingerprint,
                reservation.PackageId.Value,
                reservation.ProviderOperationTokenDigest,
                request.RecipientClientApplicationId,
                reservation.RecipientKeyFingerprint,
                reservation.RecipientKeyId,
                reservation.RecipientKeyVersion.Value,
                wrappedCek);
            envelope = RecipientPackageCodec.Envelope(header, out envelopeDigest);
            await spool.WriteAsync(envelope, cancellationToken).ConfigureAwait(false);

            using var aes = new AesGcm(cek, 16);
            await using (var encrypting = new FrameEncryptingStream(
                spool, aes, noncePrefix, envelopeDigest, request.CompleteAssemblyLength))
            {
                await boundedAssemblyWriter(encrypting, cancellationToken).ConfigureAwait(false);
                await encrypting.CompleteAsync(cancellationToken).ConfigureAwait(false);
            }

            spool.Seal();
            if (spool.EncryptedLength > RecipientPackageOptions.MaximumEncryptedPackageLength)
                throw new InvalidOperationException("RAW_EXPORT_RECIPIENT_PACKAGE_SIZE_LIMIT_EXCEEDED");
            return new(spool, envelopeDigest.ToArray(), spool.PackageCiphertextDigest, spool.EncryptedLength);
        }
        catch
        {
            await spool.DisposeAsync().ConfigureAwait(false);
            throw;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(cek);
            CryptographicOperations.ZeroMemory(noncePrefix);
            if (wrappedCek is not null) CryptographicOperations.ZeroMemory(wrappedCek);
            if (envelope is not null) CryptographicOperations.ZeroMemory(envelope);
            if (envelopeDigest is not null) CryptographicOperations.ZeroMemory(envelopeDigest);
        }
    }

    private sealed class FrameEncryptingStream : Stream
    {
        private readonly RecipientPackageEncryptedSpool destination;
        private readonly AesGcm aes;
        private readonly byte[] noncePrefix;
        private readonly byte[] envelopeDigest;
        private readonly long expectedLength;
        private readonly byte[] plaintext = ArrayPool<byte>.Shared.Rent(RecipientPackageOptions.FramePlaintextBytes);
        private int buffered;
        private int frameCount;
        private long total;
        private bool completed;
        private bool disposed;

        internal FrameEncryptingStream(
            RecipientPackageEncryptedSpool destination,
            AesGcm aes,
            byte[] noncePrefix,
            byte[] envelopeDigest,
            long expectedLength)
        {
            this.destination = destination;
            this.aes = aes;
            this.noncePrefix = noncePrefix.ToArray();
            this.envelopeDigest = envelopeDigest.ToArray();
            this.expectedLength = expectedLength;
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => !completed && !disposed;
        public override long Length => total;
        public override long Position { get => total; set => throw new NotSupportedException(); }
        public override void Flush() { }

        public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            EnsureWritable();
            while (!buffer.IsEmpty)
            {
                var copy = Math.Min(RecipientPackageOptions.FramePlaintextBytes - buffered, buffer.Length);
                buffer[..copy].CopyTo(plaintext.AsSpan(buffered, copy));
                buffered += copy;
                total = checked(total + copy);
                if (total > expectedLength) throw new InvalidOperationException("RAW_EXPORT_RECIPIENT_PACKAGE_PLAINTEXT_LENGTH_INVALID");
                buffer = buffer[copy..];
                if (buffered == RecipientPackageOptions.FramePlaintextBytes) EncryptFrame();
            }
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Write(buffer.Span);
            return ValueTask.CompletedTask;
        }

        internal ValueTask CompleteAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureWritable();
            if (buffered > 0) EncryptFrame();
            if (total != expectedLength || frameCount is < 1 or > RecipientPackageOptions.MaximumDataFrameCount)
                throw new InvalidOperationException("RAW_EXPORT_RECIPIENT_PACKAGE_PLAINTEXT_LENGTH_INVALID");
            Span<byte> completion = stackalloc byte[4 + 8 + 4 + 16];
            BinaryPrimitives.WriteUInt32BigEndian(completion, uint.MaxValue);
            BinaryPrimitives.WriteUInt64BigEndian(completion[4..], checked((ulong)total));
            BinaryPrimitives.WriteUInt32BigEndian(completion[12..], checked((uint)frameCount));
            Span<byte> nonce = stackalloc byte[12];
            noncePrefix.CopyTo(nonce);
            BinaryPrimitives.WriteUInt32BigEndian(nonce[8..], uint.MaxValue);
            var aad = RecipientPackageCodec.CompletionAad(envelopeDigest, frameCount, total);
            try
            {
                aes.Encrypt(nonce, ReadOnlySpan<byte>.Empty, Span<byte>.Empty, completion[16..], aad);
                destination.Write(completion);
                completed = true;
                return ValueTask.CompletedTask;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(aad);
                CryptographicOperations.ZeroMemory(nonce);
                CryptographicOperations.ZeroMemory(completion);
            }
        }

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

        private void EncryptFrame()
        {
            if (frameCount >= RecipientPackageOptions.MaximumDataFrameCount)
                throw new InvalidOperationException("RAW_EXPORT_RECIPIENT_PACKAGE_FRAME_LIMIT_EXCEEDED");
            var ciphertext = ArrayPool<byte>.Shared.Rent(buffered);
            Span<byte> tag = stackalloc byte[16];
            Span<byte> nonce = stackalloc byte[12];
            noncePrefix.CopyTo(nonce);
            BinaryPrimitives.WriteUInt32BigEndian(nonce[8..], checked((uint)frameCount));
            var aad = RecipientPackageCodec.FrameAad(envelopeDigest, frameCount, buffered);
            Span<byte> prefix = stackalloc byte[8];
            try
            {
                aes.Encrypt(nonce, plaintext.AsSpan(0, buffered), ciphertext.AsSpan(0, buffered), tag, aad);
                BinaryPrimitives.WriteUInt32BigEndian(prefix, checked((uint)frameCount));
                BinaryPrimitives.WriteUInt32BigEndian(prefix[4..], checked((uint)buffered));
                destination.Write(prefix);
                destination.Write(ciphertext.AsSpan(0, buffered));
                destination.Write(tag);
                frameCount++;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(plaintext.AsSpan(0, buffered));
                CryptographicOperations.ZeroMemory(ciphertext);
                ArrayPool<byte>.Shared.Return(ciphertext);
                CryptographicOperations.ZeroMemory(tag);
                CryptographicOperations.ZeroMemory(nonce);
                CryptographicOperations.ZeroMemory(aad);
                CryptographicOperations.ZeroMemory(prefix);
                buffered = 0;
            }
        }

        private void EnsureWritable()
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (completed) throw new InvalidOperationException("RAW_EXPORT_RECIPIENT_PACKAGE_STREAM_COMPLETED");
        }

        private void DisposeCore()
        {
            if (disposed) return;
            CryptographicOperations.ZeroMemory(plaintext);
            ArrayPool<byte>.Shared.Return(plaintext);
            CryptographicOperations.ZeroMemory(noncePrefix);
            CryptographicOperations.ZeroMemory(envelopeDigest);
            disposed = true;
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
